using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class BalanceChecker : MonoBehaviour
{
    [Header("Balance Settings")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private float maxOffsetX = 0.25f;
    [SerializeField] private float maxOffsetZ = 0.3f;
    [SerializeField] private float failureDelay = 0.5f;

    [Header("Bridge Progress Settings")]
    [SerializeField] private Transform bridgeStart;
    [SerializeField] private Transform bridgeEnd;
    [SerializeField] private bool autoDetectBridgePoints = true;
    [SerializeField] private float progressUpdateInterval = 0.1f;
    [SerializeField] private float movementSpeedThreshold = 0.01f; // Minimum speed to consider "moving"

    [Header("Progress Milestones")]
    [SerializeField] private float[] progressMilestones = { 0.25f, 0.5f, 0.75f, 1.0f }; // 25%, 50%, 75%, 100%

    [Header("UI Reference")]
    [SerializeField] private BridgeUIController uiController;

    [Header("Balance Events")]
    public UnityEvent OnBalanceLost;
    public UnityEvent OnBalanceRecovered;
    public UnityEvent OnFailure;

    [Header("Progress Events")]
    public UnityEvent<float> OnProgressChanged; // Passes progress percentage (0-1)
    public UnityEvent<float> OnMilestoneReached; // Passes milestone percentage
    public UnityEvent OnBridgeStarted; // Player starts crossing
    public UnityEvent OnBridgeCompleted; // Player reaches the end
    public UnityEvent<float> OnMovementSpeedChanged; // Passes current movement speed

    // Balance Properties
    private bool isBalanced = true;
    private float imbalanceTimer = 0f;
    private Vector3 lastValidPosition;

    // Progress Properties
    public float CurrentOffsetX { get; private set; }
    public float CurrentOffsetZ { get; private set; }
    public float BalancePercentage => Mathf.Max(
        Mathf.Abs(CurrentOffsetX) / maxOffsetX,
        Mathf.Abs(CurrentOffsetZ) / maxOffsetZ
    );

    // Bridge Progress Properties
    public float BridgeProgress { get; private set; } = 0f; // 0 to 1
    public float BridgeLength { get; private set; }
    public float DistanceTraveled { get; private set; }
    public float RemainingDistance => BridgeLength - DistanceTraveled;
    public float MovementSpeed { get; private set; }
    public Vector3 MovementDirection { get; private set; }
    public bool IsMovingForward { get; private set; }
    public bool HasStartedCrossing { get; private set; }
    public bool HasCompletedBridge { get; private set; }

    // Internal tracking
    private Vector3 lastPlayerPosition;
    private Vector3 bridgeDirection;
    private bool[] milestonesReached;
    private float lastProgressUpdate;

    void Start()
    {
        InitializeCamera();
        InitializeBridgePoints();
        InitializeTracking();
        
        if (uiController == null)
            uiController = FindObjectOfType<BridgeUIController>();

        lastValidPosition = transform.position;
        lastPlayerPosition = GetPlayerPosition();
        
        // Initialize milestone tracking
        milestonesReached = new bool[progressMilestones.Length];
    }

    void Update()
    {
        if (xrCamera == null) return;

        CheckBalance();
        UpdateBridgeProgress();
        UpdateUI();
        HandleBalanceLogic();
    }

    private void InitializeCamera()
    {
        if (xrCamera == null)
        {
            // Try to find XR Camera first
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                xrCamera = xrOrigin.Camera.transform;
            }
            else
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                    xrCamera = mainCamera.transform;
                else
                    Debug.LogWarning("BalanceChecker: No XR Camera assigned and no camera found!");
            }
        }
    }

    private void InitializeBridgePoints()
    {
        if (autoDetectBridgePoints)
        {
            AutoDetectBridgePoints();
        }

        if (bridgeStart != null && bridgeEnd != null)
        {
            BridgeLength = Vector3.Distance(bridgeStart.position, bridgeEnd.position);
            bridgeDirection = (bridgeEnd.position - bridgeStart.position).normalized;
            
            Debug.Log($"Bridge initialized: Length = {BridgeLength:F2}m, Direction = {bridgeDirection}");
        }
        else
        {
            Debug.LogWarning("BalanceChecker: Bridge start/end points not set!");
        }
    }

    private void AutoDetectBridgePoints()
    {
        // Try to find bridge builder and get platform information
        var bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();
        if (bridgeBuilder != null)
        {
            var startPlatform = bridgeBuilder.GetStartPlatform();
            var endPlatform = bridgeBuilder.GetEndPlatform();
            
            if (startPlatform?.GameObject != null)
                bridgeStart = startPlatform.GameObject.transform;
            
            if (endPlatform?.GameObject != null)
                bridgeEnd = endPlatform.GameObject.transform;
        }

        // Fallback: Try to find objects by name
        if (bridgeStart == null)
        {
            var startObj = GameObject.Find("StartPlatform");
            if (startObj != null) bridgeStart = startObj.transform;
        }

        if (bridgeEnd == null)
        {
            var endObj = GameObject.Find("EndPlatform");
            if (endObj != null) bridgeEnd = endObj.transform;
        }
    }

    private void InitializeTracking()
    {
        BridgeProgress = 0f;
        DistanceTraveled = 0f;
        MovementSpeed = 0f;
        IsMovingForward = false;
        HasStartedCrossing = false;
        HasCompletedBridge = false;
        lastProgressUpdate = Time.time;
    }

    private Vector3 GetPlayerPosition()
    {
        return xrCamera != null ? xrCamera.position : transform.position;
    }

    private void CheckBalance()
    {
        // Calculate local offset from plank center
        Vector3 localPos = transform.InverseTransformPoint(GetPlayerPosition());
        CurrentOffsetX = localPos.x;
        CurrentOffsetZ = localPos.z;

        // Check if within balance thresholds
        bool wasBalanced = isBalanced;
        isBalanced = Mathf.Abs(CurrentOffsetX) <= maxOffsetX && Mathf.Abs(CurrentOffsetZ) <= maxOffsetZ;

        // Handle balance state changes
        if (wasBalanced && !isBalanced)
        {
            OnBalanceLost?.Invoke();
            imbalanceTimer = 0f;
        }
        else if (!wasBalanced && isBalanced)
        {
            OnBalanceRecovered?.Invoke();
            lastValidPosition = transform.position;
        }
    }

    private void UpdateBridgeProgress()
    {
        if (bridgeStart == null || bridgeEnd == null || BridgeLength <= 0) return;

        if (Time.time - lastProgressUpdate < progressUpdateInterval) return;

        Vector3 currentPlayerPos = GetPlayerPosition();
        
        // Calculate movement
        Vector3 movement = currentPlayerPos - lastPlayerPosition;
        float deltaTime = Time.time - lastProgressUpdate;
        MovementSpeed = movement.magnitude / deltaTime;
        MovementDirection = movement.normalized;
        
        // Calculate progress along bridge
        Vector3 startToPlayer = currentPlayerPos - bridgeStart.position;
        float projectedDistance = Vector3.Dot(startToPlayer, bridgeDirection);
        
        // Clamp to bridge bounds
        projectedDistance = Mathf.Clamp(projectedDistance, 0f, BridgeLength);
        DistanceTraveled = projectedDistance;
        
        float newProgress = BridgeLength > 0 ? projectedDistance / BridgeLength : 0f;
        newProgress = Mathf.Clamp01(newProgress);

        // Check if moving forward
        Vector3 forwardMovement = Vector3.Project(movement, bridgeDirection);
        IsMovingForward = Vector3.Dot(forwardMovement, bridgeDirection) > 0 && MovementSpeed > movementSpeedThreshold;

        // Track crossing start
        if (!HasStartedCrossing && newProgress > 0.05f) // 5% into bridge
        {
            HasStartedCrossing = true;
            OnBridgeStarted?.Invoke();
        }

        // Track bridge completion
        if (!HasCompletedBridge && newProgress >= 0.95f) // 95% of bridge
        {
            HasCompletedBridge = true;
            OnBridgeCompleted?.Invoke();
        }

        // Check milestones
        CheckProgressMilestones(newProgress);

        // Update progress if changed significantly
        if (Mathf.Abs(newProgress - BridgeProgress) > 0.01f) // 1% change threshold
        {
            BridgeProgress = newProgress;
            OnProgressChanged?.Invoke(BridgeProgress);
        }

        // Fire movement speed event
        OnMovementSpeedChanged?.Invoke(MovementSpeed);

        // Update tracking variables
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
                Debug.Log($"Bridge milestone reached: {progressMilestones[i] * 100:F0}%");
            }
        }
    }

    private void UpdateUI()
    {
        if (uiController != null)
        {
            // Update balance UI
            float maxOffset = Mathf.Max(maxOffsetX, maxOffsetZ);
            float currentOffset = Mathf.Max(Mathf.Abs(CurrentOffsetX), Mathf.Abs(CurrentOffsetZ));
            uiController.UpdateBalanceUI(currentOffset, maxOffset);
            
            // Update progress UI (if method exists)
            if (HasMethod(uiController, "UpdateProgressUI"))
            {
                uiController.GetType().GetMethod("UpdateProgressUI")?.Invoke(uiController, 
                    new object[] { BridgeProgress, DistanceTraveled, RemainingDistance, MovementSpeed });
            }
        }
    }

    private bool HasMethod(object obj, string methodName)
    {
        return obj.GetType().GetMethod(methodName) != null;
    }

    private void HandleBalanceLogic()
    {
        if (!isBalanced)
        {
            imbalanceTimer += Time.deltaTime;
            if (imbalanceTimer >= failureDelay)
            {
                TriggerFailure();
            }
        }
        else
        {
            imbalanceTimer = 0f;
        }
    }

    private void TriggerFailure()
    {
        OnFailure?.Invoke();
        
        // Default failure behavior - reload scene
        var sceneManager = FindObjectOfType<SceneTransitionManager>();
        if (sceneManager != null)
            sceneManager.RestartCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
    }

    // Public API for external access
    public void ResetProgress()
    {
        InitializeTracking();
        for (int i = 0; i < milestonesReached.Length; i++)
        {
            milestonesReached[i] = false;
        }
    }

    public float GetProgressPercentage()
    {
        return BridgeProgress * 100f;
    }

    public string GetProgressText()
    {
        return $"{BridgeProgress:P1} ({DistanceTraveled:F1}m / {BridgeLength:F1}m)";
    }

    public float GetEstimatedTimeToComplete()
    {
        if (MovementSpeed <= 0 || RemainingDistance <= 0) return -1f;
        return RemainingDistance / MovementSpeed;
    }

    public bool IsPlayerOnBridge()
    {
        return BridgeProgress > 0.01f && BridgeProgress < 0.99f;
    }

    // Manual bridge point assignment
    public void SetBridgePoints(Transform start, Transform end)
    {
        bridgeStart = start;
        bridgeEnd = end;
        InitializeBridgePoints();
        ResetProgress();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw balance area
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(maxOffsetX * 2, 0.1f, maxOffsetZ * 2));
        
        // Draw bridge progress visualization
        if (bridgeStart != null && bridgeEnd != null)
        {
            // Draw bridge line
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(bridgeStart.position, bridgeEnd.position);
            
            // Draw current position on bridge
            if (BridgeLength > 0)
            {
                Vector3 progressPos = bridgeStart.position + bridgeDirection * DistanceTraveled;
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(progressPos, 0.1f);
                
                // Draw movement direction
                if (MovementSpeed > movementSpeedThreshold)
                {
                    Gizmos.color = IsMovingForward ? Color.green : Color.red;
                    Gizmos.DrawRay(GetPlayerPosition(), MovementDirection * MovementSpeed);
                }
            }
        }
        
        // Draw milestones
        if (bridgeStart != null && bridgeEnd != null && BridgeLength > 0)
        {
            for (int i = 0; i < progressMilestones.Length; i++)
            {
                Vector3 milestonePos = bridgeStart.position + bridgeDirection * (BridgeLength * progressMilestones[i]);
                Gizmos.color = milestonesReached[i] ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(milestonePos, 0.2f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Always draw bridge line if points are set
        if (bridgeStart != null && bridgeEnd != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bridgeStart.position + Vector3.up * 0.1f, bridgeEnd.position + Vector3.up * 0.1f);
        }
    }
}