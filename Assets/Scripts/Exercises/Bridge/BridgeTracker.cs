using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simplified tracker for bridge balance and progress with spawn/teleport grace windows.
/// </summary>
public class BridgeTracker : MonoBehaviour
{
    [Header("Balance Settings")]
    [SerializeField] private float maxOffsetX = 0.5f;
    [SerializeField] private float maxOffsetZ = 0.6f;
    [SerializeField] private float failureDelay = 0.5f;

    [Tooltip("Don’t allow failures for a short time after scene start.")]
    [SerializeField] private float spawnGraceSeconds = 1.25f;

    [Tooltip("Don't allow failures for a short time after each teleport.")]
    [SerializeField] private float teleportGraceSeconds = 2.0f;

    [Tooltip("If true, balance failures are only checked after crossing has begun (~5% progress).")]
    [SerializeField] private bool failOnlyAfterStart = true;
    
    [Tooltip("Temporarily disable balance failures for debugging.")]
    [SerializeField] private bool disableBalanceFailures = false;

    [Header("Progress Settings")]
    [SerializeField] private float progressUpdateInterval = 0.1f;
    [SerializeField] private float movementSpeedThreshold = 0.01f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bridgeStart;
    [SerializeField] private Transform bridgeEnd;
    [SerializeField] private BalanceUI balanceUI;
    [SerializeField] private ProgressUI progressUI;

    [Header("Events")]
    public UnityEvent OnBalanceLost;
    public UnityEvent OnBalanceRecovered;
    public UnityEvent OnFailure;
    public UnityEvent<float> OnProgressChanged;
    public UnityEvent<float> OnMilestoneReached;
    public UnityEvent OnBridgeStarted;
    public UnityEvent OnBridgeCompleted;
    public UnityEvent<float> OnMovementSpeedChanged;

    // Balance state
    private bool isBalanced = true;
    private float imbalanceTimer = 0f;
    private float currentOffsetX;
    private float currentOffsetZ;

    // Progress state
    private float bridgeProgress = 0f;
    private float bridgeLength;
    private Vector3 bridgeDirection;
    private Vector3 bridgeRight;
    private Vector3 lastPlayerPosition;
    private float lastProgressUpdate;
    private float movementSpeed;
    private bool hasStartedCrossing;
    private bool hasCompletedBridge;

    // Milestone tracking
    private readonly float[] progressMilestones = { 0.25f, 0.5f, 0.75f, 1.0f };
    private readonly bool[] milestonesReached = new bool[4];

    // Grace window
    private float nextFailureAllowedTime = 0f;

    void Awake()
    {
        // Ensure UnityEvents are initialized to prevent null refs when listeners are added at runtime
        if (OnBalanceLost == null) OnBalanceLost = new UnityEvent();
        if (OnBalanceRecovered == null) OnBalanceRecovered = new UnityEvent();
        if (OnFailure == null) OnFailure = new UnityEvent();
        if (OnProgressChanged == null) OnProgressChanged = new UnityEvent<float>();
        if (OnMilestoneReached == null) OnMilestoneReached = new UnityEvent<float>();
        if (OnBridgeStarted == null) OnBridgeStarted = new UnityEvent();
        if (OnBridgeCompleted == null) OnBridgeCompleted = new UnityEvent();
        if (OnMovementSpeedChanged == null) OnMovementSpeedChanged = new UnityEvent<float>();
    }

    void Start()
    {
        InitializeReferences();
        InitializeBridgePoints();

        lastPlayerPosition = GetPlayerPosition();
        lastProgressUpdate = Time.time;

        // Initial grace at scene start
        nextFailureAllowedTime = Time.time + spawnGraceSeconds;
    }

    void Update()
    {
        CheckBalance();
        UpdateBridgeProgress();
        UpdateUI();
        HandleBalanceLogic();
    }

    private void InitializeReferences()
    {
        if (playerTransform == null)
        {
            // Try to find XR Camera, fallback to main camera
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                playerTransform = xrOrigin.Camera.transform;
            }
            else if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        if (balanceUI == null) balanceUI = FindObjectOfType<BalanceUI>();
        if (progressUI == null) progressUI = FindObjectOfType<ProgressUI>();
    }

    private void InitializeBridgePoints()
    {
        var bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();
        if (bridgeBuilder != null)
        {
            var startPlatform = bridgeBuilder.GetStartPlatform();
            var endPlatform = bridgeBuilder.GetEndPlatform();

            if (startPlatform != null) bridgeStart = startPlatform.transform;
            if (endPlatform != null) bridgeEnd = endPlatform.transform;
        }

        if (bridgeStart != null && bridgeEnd != null)
        {
            bridgeLength = Vector3.Distance(bridgeStart.position, bridgeEnd.position);
            bridgeDirection = (bridgeEnd.position - bridgeStart.position).normalized;
            // Define a stable lateral axis (right) perpendicular to the bridge direction
            // Use world up to construct a tangent basis
            bridgeRight = Vector3.Normalize(Vector3.Cross(Vector3.up, bridgeDirection));
        }
    }

    private Vector3 GetPlayerPosition() =>
        playerTransform != null ? playerTransform.position : transform.position;

    private void CheckBalance()
    {
        if (playerTransform == null) return;

        // Calculate lateral (sideways) offset relative to the bridge axis
        Vector3 playerPos = GetPlayerPosition();
        if (bridgeStart != null && bridgeEnd != null && bridgeLength > 0f)
        {
            Vector3 startToPlayer = playerPos - bridgeStart.position;
            // Lateral offset is the projection of startToPlayer onto bridgeRight
            currentOffsetX = Vector3.Dot(startToPlayer, bridgeRight);
            // Forward offset along the bridge axis (not used for balance failure anymore)
            currentOffsetZ = Vector3.Dot(startToPlayer, bridgeDirection);
        }
        else
        {
            // Fallback to local space if bridge points not yet initialized
            Vector3 localPos = transform.InverseTransformPoint(playerPos);
            currentOffsetX = localPos.x;
            currentOffsetZ = localPos.z;
        }

        bool wasBalanced = isBalanced;
        // Only lateral offset determines balance; ignore forward/back (Z)
        isBalanced = Mathf.Abs(currentOffsetX) <= maxOffsetX;

        // Debug balance state changes
        if (wasBalanced && !isBalanced)
        {
            Debug.LogWarning($"BridgeTracker: Balance lost! Player pos: {playerPos}, Lateral: {currentOffsetX:F3}, Forward: {currentOffsetZ:F3}, LimitX: {maxOffsetX}");
            OnBalanceLost?.Invoke();
            imbalanceTimer = 0f;
        }
        else if (!wasBalanced && isBalanced)
        {
            Debug.Log($"BridgeTracker: Balance recovered! Local offset: ({currentOffsetX:F3}, {currentOffsetZ:F3})");
            OnBalanceRecovered?.Invoke();
        }
    }

    private void UpdateBridgeProgress()
    {
        if (bridgeStart == null || bridgeEnd == null || bridgeLength <= 0) return;
        if (Time.time - lastProgressUpdate < progressUpdateInterval) return;

        Vector3 currentPlayerPos = GetPlayerPosition();

        // Movement speed
        Vector3 movement = currentPlayerPos - lastPlayerPosition;
        float deltaTime = Mathf.Max(0.0001f, Time.time - lastProgressUpdate);
        movementSpeed = movement.magnitude / deltaTime;

        // Progress along bridge
        Vector3 startToPlayer = currentPlayerPos - bridgeStart.position;
        float projectedDistance = Mathf.Clamp(Vector3.Dot(startToPlayer, bridgeDirection), 0f, bridgeLength);

        float newProgress = Mathf.Clamp01(projectedDistance / bridgeLength);

        // Start crossing once we’ve moved a bit onto the bridge
        if (!hasStartedCrossing && newProgress > 0.05f)
        {
            hasStartedCrossing = true;
            OnBridgeStarted?.Invoke();
        }

        // Completion
        if (!hasCompletedBridge && newProgress >= 0.95f)
        {
            hasCompletedBridge = true;
            OnBridgeCompleted?.Invoke();
        }

        CheckProgressMilestones(newProgress);

        if (Mathf.Abs(newProgress - bridgeProgress) > 0.01f)
        {
            bridgeProgress = newProgress;
            OnProgressChanged?.Invoke(bridgeProgress);
        }

        OnMovementSpeedChanged?.Invoke(movementSpeed);

        lastPlayerPosition = currentPlayerPos;
        lastProgressUpdate = Time.time;
    }

    private void CheckProgressMilestones(float currentProgress)
    {
        for (int i = 0; i < progressMilestones.Length; i++)
        {
            if (!milestonesReached[i] && currentProgress >= progressMilestones[i])
            {
                milestonesReached[i] = true;
                OnMilestoneReached?.Invoke(progressMilestones[i]);
            }
        }
    }

    private void UpdateUI()
    {
        if (balanceUI != null)
        {
            float maxOffset = Mathf.Max(maxOffsetX, maxOffsetZ);
            float currentOffset = Mathf.Max(Mathf.Abs(currentOffsetX), Mathf.Abs(currentOffsetZ));
            balanceUI.UpdateBalance(currentOffset, maxOffset);
        }

        if (progressUI != null && bridgeLength > 0f)
        {
            // Update progress panel
            progressUI.UpdateProgress(
                bridgeProgress,
                bridgeProgress * bridgeLength,
                (1f - bridgeProgress) * bridgeLength,
                movementSpeed
            );

            // Update balance bar with signed lateral offset
            progressUI.UpdateBalanceBar(currentOffsetX, maxOffsetX);
        }
    }

    private void HandleBalanceLogic()
    {
        // Skip balance failure logic if disabled for debugging
        if (disableBalanceFailures)
        {
            if (Time.time % 5f < 0.1f) // Debug every 5 seconds
                Debug.Log($"BridgeTracker: Balance failures disabled for debugging. Offset X: {currentOffsetX:F3}, Z: {currentOffsetZ:F3}");
            return;
        }
        
        // Block failure during grace windows, and (optionally) until crossing started
        if (Time.time < nextFailureAllowedTime) 
        {
            if (Time.time % 2f < 0.1f) // Debug every 2 seconds
                Debug.Log($"BridgeTracker: Grace period active. Time remaining: {nextFailureAllowedTime - Time.time:F1}s");
            return;
        }
        if (failOnlyAfterStart && !hasStartedCrossing) 
        {
            if (Time.time % 2f < 0.1f) // Debug every 2 seconds
                Debug.Log($"BridgeTracker: Waiting for crossing to start. Progress: {bridgeProgress:F2}");
            return;
        }

        if (!isBalanced)
        {
            imbalanceTimer += Time.deltaTime;
            if (imbalanceTimer >= failureDelay)
            {
                Debug.LogError($"BridgeTracker: Balance failure triggered! Offset X: {currentOffsetX:F3}, Z: {currentOffsetZ:F3}, Timer: {imbalanceTimer:F2}s");
                OnFailure?.Invoke();
                TriggerFailure();
            }
            else if (imbalanceTimer > 0.1f && Time.time % 1f < 0.1f) // Debug every second when imbalanced
            {
                Debug.LogWarning($"BridgeTracker: Imbalanced! Offset X: {currentOffsetX:F3}, Z: {currentOffsetZ:F3}, Timer: {imbalanceTimer:F2}s");
            }
        }
        else
        {
            if (imbalanceTimer > 0f)
            {
                Debug.Log($"BridgeTracker: Balance recovered. Offset X: {currentOffsetX:F3}, Z: {currentOffsetZ:F3}");
            }
            imbalanceTimer = 0f;
        }
    }

    private void TriggerFailure()
    {
        Debug.LogError("BridgeTracker: Triggering failure - scene will restart in 3 seconds");
        
        // Add a delay to prevent immediate restart and allow debugging
        StartCoroutine(DelayedSceneRestart());
    }
    
    private System.Collections.IEnumerator DelayedSceneRestart()
    {
        yield return new WaitForSeconds(3f);
        
        Debug.LogError("BridgeTracker: Restarting scene now");
        var sceneManager = FindObjectOfType<VRRehab.SceneManagement.SceneTransitionManager>();
        if (sceneManager != null)
            sceneManager.RestartCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
    }

    // ----- Public API -----
    public float GetProgressPercentage() => bridgeProgress * 100f;
    public float GetCurrentOffsetX() => currentOffsetX;
    public float GetCurrentOffsetZ() => currentOffsetZ;
    public bool IsBalanced() => isBalanced;
    public float GetMovementSpeed() => movementSpeed;

    public void ResetProgress()
    {
        bridgeProgress = 0f;
        hasStartedCrossing = false;
        hasCompletedBridge = false;
        for (int i = 0; i < milestonesReached.Length; i++) milestonesReached[i] = false;
    }

    public void SetBridgePoints(Transform start, Transform end)
    {
        bridgeStart = start;
        bridgeEnd = end;
        InitializeBridgePoints();
        ResetProgress();
    }

    /// <summary>
    /// Call this after teleporting the player to suppress false failures.
    /// </summary>
    public void NotifyTeleported(float extraGraceSeconds = 0f)
    {
        nextFailureAllowedTime = Time.time + teleportGraceSeconds + Mathf.Max(0f, extraGraceSeconds);
        imbalanceTimer = 0f;
        lastPlayerPosition = GetPlayerPosition(); // avoid speed spike
    }
}
