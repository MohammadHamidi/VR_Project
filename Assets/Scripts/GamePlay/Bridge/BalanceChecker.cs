using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Transform))]
public class BalanceChecker : MonoBehaviour
{
    [Header("Balance Settings")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private float maxOffsetX = 0.25f;
    [SerializeField] private float maxOffsetZ = 0.3f;
    [SerializeField] private float failureDelay = 0.5f;

    [Header("UI Reference")]
    [SerializeField] private BridgeUIController uiController;

    [Header("Events")]
    public UnityEvent OnBalanceLost;
    public UnityEvent OnBalanceRecovered;
    public UnityEvent OnFailure;

    private bool isBalanced = true;
    private float imbalanceTimer = 0f;
    private Vector3 lastValidPosition;

    public float CurrentOffsetX { get; private set; }
    public float CurrentOffsetZ { get; private set; }
    public float BalancePercentage => Mathf.Max(
        Mathf.Abs(CurrentOffsetX) / maxOffsetX,
        Mathf.Abs(CurrentOffsetZ) / maxOffsetZ
    );

    void Start()
    {
        if (xrCamera == null)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
                xrCamera = mainCamera.transform;
            else
                Debug.LogWarning("BalanceChecker: No XR Camera assigned and no main camera found!");
        }

        if (uiController == null)
            uiController = FindObjectOfType<BridgeUIController>();

        lastValidPosition = transform.position;
    }

    void Update()
    {
        if (xrCamera == null) return;

        CheckBalance();
        UpdateUI();
        HandleBalanceLogic();
    }

    private void CheckBalance()
    {
        // Calculate local offset from plank center
        Vector3 localPos = transform.InverseTransformPoint(xrCamera.position);
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

    private void UpdateUI()
    {
        if (uiController != null)
        {
            float maxOffset = Mathf.Max(maxOffsetX, maxOffsetZ);
            float currentOffset = Mathf.Max(Mathf.Abs(CurrentOffsetX), Mathf.Abs(CurrentOffsetZ));
            uiController.UpdateBalanceUI(currentOffset, maxOffset);
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(maxOffsetX * 2, 0.1f, maxOffsetZ * 2));
    }
}