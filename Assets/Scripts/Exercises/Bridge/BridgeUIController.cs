using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BridgeUIController : MonoBehaviour
{
    [Header("Balance UI Components")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Slider balanceSlider;
    [SerializeField] private Image balanceFill;

    [Header("Progress UI Components")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI timeEstimateText;
    [SerializeField] private Image directionIndicator;
    [SerializeField] private Transform milestoneContainer;
    [SerializeField] private GameObject milestonePrefab;

    [Header("Bridge Minimap")]
    [SerializeField] private Image bridgeMap;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Transform[] milestoneMarkers;

    [Header("Visual Settings - Balance")]
    [SerializeField] private Gradient balanceGradient = new Gradient();
    [SerializeField] private string[] balanceMessages = {
        "Perfect Balance!",
        "Keep steady...",
        "Careful now!",
        "Too far - lean back!"
    };

    [Header("Visual Settings - Progress")]
    [SerializeField] private Gradient progressGradient = new Gradient();
    [SerializeField] private Color forwardColor = Color.green;
    [SerializeField] private Color backwardColor = Color.red;
    [SerializeField] private Color stationaryColor = Color.gray;
    [SerializeField] private Sprite forwardArrow;
    [SerializeField] private Sprite backwardArrow;

    [Header("Progress Messages")]
    [SerializeField] private string[] progressMessages = {
        "Starting your journey...",
        "Quarter way there!",
        "Halfway across!",
        "Three quarters done!",
        "Almost there!",
        "Bridge completed!"
    };

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private bool usePulseAnimation = true;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("UI Panels")]
    [SerializeField] private GameObject balancePanel;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private GameObject minimapPanel;

    // Balance Animation Targets
    private float targetSliderValue = 0.5f;
    private Color targetBalanceColor = Color.green;

    // Progress Animation Targets
    private float targetProgressValue = 0f;
    private Color targetProgressColor = Color.blue;

    // Progress Data
    private float currentProgress = 0f;
    private float currentSpeed = 0f;
    private bool isMovingForward = false;
    private float bridgeLength = 0f;
    private bool[] milestonesReached;

    void Start()
    {
        InitializeUI();
        InitializeMilestones();
    }

    void Update()
    {
        if (useAnimation)
            AnimateUI();
        
        if (usePulseAnimation)
            HandlePulseAnimations();
    }

    private void InitializeUI()
    {
        InitializeBalanceUI();
        InitializeProgressUI();
        InitializeMinimap();
    }

    private void InitializeBalanceUI()
    {
        if (balanceSlider != null)
        {
            balanceSlider.minValue = 0f;
            balanceSlider.maxValue = 1f;
            balanceSlider.value = 0.5f;
        }

        if (balanceFill == null && balanceSlider != null)
            balanceFill = balanceSlider.fillRect?.GetComponent<Image>();

        // Setup default balance gradient
        if (balanceGradient.colorKeys.Length == 0)
        {
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.green, 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.red, 1f)
            };
            balanceGradient.SetKeys(colorKeys, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) });
        }
    }

    private void InitializeProgressUI()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        if (progressFill == null && progressSlider != null)
            progressFill = progressSlider.fillRect?.GetComponent<Image>();

        // Setup default progress gradient
        if (progressGradient.colorKeys.Length == 0)
        {
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.green, 1f)
            };
            progressGradient.SetKeys(colorKeys, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) });
        }

        // Initialize text displays
        UpdateProgressText(0f);
        UpdateDistanceText(0f, 0f, 0f);
        UpdateSpeedText(0f);
        UpdateTimeEstimateText(-1f);
    }

    private void InitializeMinimap()
    {
        if (bridgeMap != null)
        {
            bridgeMap.fillAmount = 0f;
        }

        if (playerMarker != null)
        {
            playerMarker.anchoredPosition = Vector2.zero;
        }
    }

    private void InitializeMilestones()
    {
        // Initialize milestone tracking (default 4 milestones)
        milestonesReached = new bool[4];
        
        // Create milestone UI markers if container and prefab exist
        if (milestoneContainer != null && milestonePrefab != null)
        {
            CreateMilestoneMarkers();
        }
    }

    private void CreateMilestoneMarkers()
    {
        // Clear existing markers
        foreach (Transform child in milestoneContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // Create new milestone markers
        milestoneMarkers = new Transform[milestonesReached.Length];
        for (int i = 0; i < milestonesReached.Length; i++)
        {
            GameObject marker = Instantiate(milestonePrefab, milestoneContainer);
            marker.name = $"Milestone_{i + 1}";
            milestoneMarkers[i] = marker.transform;
            
            // Position markers evenly across the container
            if (marker.GetComponent<RectTransform>() != null)
            {
                RectTransform rect = marker.GetComponent<RectTransform>();
                float normalizedPosition = (i + 1) / (float)(milestonesReached.Length + 1);
                rect.anchorMin = new Vector2(normalizedPosition, 0.5f);
                rect.anchorMax = new Vector2(normalizedPosition, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }
        }
    }

    // ===== BALANCE UI METHODS =====
    public void UpdateBalanceUI(float currentOffset, float maxOffset)
    {
        // Calculate normalized balance value [0, 1]
        float normalizedBalance = Mathf.InverseLerp(0f, maxOffset, Mathf.Abs(currentOffset));
        targetSliderValue = normalizedBalance;

        // Update color
        targetBalanceColor = balanceGradient.Evaluate(normalizedBalance);

        // Update text message
        UpdateBalanceMessage(normalizedBalance);

        // If not using animation, update immediately
        if (!useAnimation)
        {
            if (balanceSlider != null)
                balanceSlider.value = targetSliderValue;
            if (balanceFill != null)
                balanceFill.color = targetBalanceColor;
        }
    }

    private void UpdateBalanceMessage(float balanceValue)
    {
        if (promptText == null || balanceMessages.Length == 0) return;

        int messageIndex = Mathf.FloorToInt(balanceValue * (balanceMessages.Length - 1));
        messageIndex = Mathf.Clamp(messageIndex, 0, balanceMessages.Length - 1);
        promptText.text = balanceMessages[messageIndex];
    }

    // ===== PROGRESS UI METHODS =====
    public void UpdateProgressUI(float progress, float distanceTraveled, float remainingDistance, float movementSpeed)
    {
        currentProgress = progress;
        currentSpeed = movementSpeed;
        bridgeLength = distanceTraveled + remainingDistance;

        targetProgressValue = progress;
        targetProgressColor = progressGradient.Evaluate(progress);

        if (!useAnimation)
        {
            ApplyProgressValues();
        }

        UpdateProgressText(progress);
        UpdateDistanceText(distanceTraveled, remainingDistance, bridgeLength);
        UpdateSpeedText(movementSpeed);
        UpdateDirectionIndicator();
        UpdateMinimap(progress);
    }

    private void ApplyProgressValues()
    {
        if (progressSlider != null)
            progressSlider.value = targetProgressValue;
        if (progressFill != null)
            progressFill.color = targetProgressColor;
    }

    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {progress:P1}";
        }
    }

    private void UpdateDistanceText(float traveled, float remaining, float total)
    {
        if (distanceText != null)
        {
            distanceText.text = $"{traveled:F1}m / {total:F1}m\n{remaining:F1}m remaining";
        }
    }

    private void UpdateSpeedText(float speed)
    {
        if (speedText != null)
        {
            speedText.text = $"Speed: {speed:F2} m/s";
        }
    }

    public void UpdateTimeEstimateText(float timeRemaining)
    {
        if (timeEstimateText != null)
        {
            if (timeRemaining > 0)
            {
                timeEstimateText.text = $"ETA: {timeRemaining:F1}s";
            }
            else
            {
                timeEstimateText.text = "ETA: --";
            }
        }
    }

    public void UpdateMovementDirection(bool movingForward)
    {
        isMovingForward = movingForward;
        UpdateDirectionIndicator();
    }

    private void UpdateDirectionIndicator()
    {
        if (directionIndicator == null) return;

        if (currentSpeed < 0.01f) // Stationary
        {
            directionIndicator.color = stationaryColor;
            directionIndicator.sprite = null;
        }
        else if (isMovingForward)
        {
            directionIndicator.color = forwardColor;
            directionIndicator.sprite = forwardArrow;
        }
        else
        {
            directionIndicator.color = backwardColor;
            directionIndicator.sprite = backwardArrow;
        }
    }

    private void UpdateMinimap(float progress)
    {
        if (bridgeMap != null)
        {
            bridgeMap.fillAmount = progress;
        }

        if (playerMarker != null && playerMarker is RectTransform playerRect)
        {
            // Move player marker along the minimap
            float mapWidth = bridgeMap != null ? bridgeMap.rectTransform.rect.width : 200f;
            playerRect.anchoredPosition = new Vector2(progress * mapWidth - mapWidth * 0.5f, 0);
        }
    }

    // ===== MILESTONE METHODS =====
    public void OnMilestoneReached(float milestoneValue)
    {
        int milestoneIndex = -1;
        
        // Determine which milestone was reached
        float[] standardMilestones = { 0.25f, 0.5f, 0.75f, 1.0f };
        for (int i = 0; i < standardMilestones.Length; i++)
        {
            if (Mathf.Approximately(milestoneValue, standardMilestones[i]))
            {
                milestoneIndex = i;
                break;
            }
        }

        if (milestoneIndex >= 0 && milestoneIndex < milestonesReached.Length)
        {
            milestonesReached[milestoneIndex] = true;
            UpdateMilestoneVisuals(milestoneIndex);
            ShowMilestoneMessage(milestoneIndex);
        }
    }

    private void UpdateMilestoneVisuals(int milestoneIndex)
    {
        if (milestoneMarkers != null && milestoneIndex < milestoneMarkers.Length)
        {
            var marker = milestoneMarkers[milestoneIndex];
            if (marker != null)
            {
                var image = marker.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.green; // Mark as completed
                }
                
                // Add pulse animation
                StartCoroutine(PulseMilestoneMarker(marker));
            }
        }
    }

    private void ShowMilestoneMessage(int milestoneIndex)
    {
        if (milestoneIndex < progressMessages.Length)
        {
            StartCoroutine(ShowTemporaryMessage(progressMessages[milestoneIndex], 2f));
        }
    }

    // ===== ANIMATION METHODS =====
    private void AnimateUI()
    {
        // Animate balance UI
        if (balanceSlider != null)
        {
            balanceSlider.value = Mathf.Lerp(balanceSlider.value, targetSliderValue, 
                animationSpeed * Time.deltaTime);
        }

        if (balanceFill != null)
        {
            balanceFill.color = Color.Lerp(balanceFill.color, targetBalanceColor, 
                animationSpeed * Time.deltaTime);
        }

        // Animate progress UI
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Lerp(progressSlider.value, targetProgressValue,
                animationSpeed * Time.deltaTime);
        }

        if (progressFill != null)
        {
            progressFill.color = Color.Lerp(progressFill.color, targetProgressColor,
                animationSpeed * Time.deltaTime);
        }
    }

    private void HandlePulseAnimations()
    {
        // Pulse direction indicator when moving
        if (directionIndicator != null && currentSpeed > 0.01f)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.1f + 1f;
            directionIndicator.transform.localScale = Vector3.one * pulse;
        }
    }

    // ===== UTILITY METHODS =====
    public void SetPanelVisibility(string panelName, bool visible)
    {
        switch (panelName.ToLower())
        {
            case "balance":
                if (balancePanel != null) balancePanel.SetActive(visible);
                break;
            case "progress":
                if (progressPanel != null) progressPanel.SetActive(visible);
                break;
            case "minimap":
                if (minimapPanel != null) minimapPanel.SetActive(visible);
                break;
            default:
                gameObject.SetActive(visible);
                break;
        }
    }

    public void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void PulseWarning()
    {
        if (promptText != null)
            StartCoroutine(PulseTextCoroutine());
    }

    public void ShowCompletionMessage()
    {
        StartCoroutine(ShowTemporaryMessage("Bridge Crossed Successfully!", 3f));
    }

    public void ResetProgressUI()
    {
        currentProgress = 0f;
        targetProgressValue = 0f;
        
        if (progressSlider != null) progressSlider.value = 0f;
        if (bridgeMap != null) bridgeMap.fillAmount = 0f;
        
        // Reset milestones
        for (int i = 0; i < milestonesReached.Length; i++)
        {
            milestonesReached[i] = false;
            if (milestoneMarkers != null && i < milestoneMarkers.Length)
            {
                var image = milestoneMarkers[i].GetComponent<Image>();
                if (image != null) image.color = Color.gray;
            }
        }
        
        UpdateProgressText(0f);
        UpdateDistanceText(0f, 0f, 0f);
        UpdateSpeedText(0f);
        UpdateTimeEstimateText(-1f);
    }

    // ===== COROUTINES =====
    private IEnumerator PulseTextCoroutine()
    {
        var originalColor = promptText.color;
        var warningColor = Color.red;
        
        for (int i = 0; i < 3; i++)
        {
            promptText.color = warningColor;
            yield return new WaitForSeconds(0.1f);
            promptText.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        if (promptText != null)
        {
            string originalText = promptText.text;
            promptText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            promptText.text = originalText;
        }
    }

    private IEnumerator PulseMilestoneMarker(Transform marker)
    {
        Vector3 originalScale = marker.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        
        // Scale up
        float timer = 0f;
        while (timer < 0.3f)
        {
            marker.localScale = Vector3.Lerp(originalScale, targetScale, timer / 0.3f);
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Scale down
        timer = 0f;
        while (timer < 0.3f)
        {
            marker.localScale = Vector3.Lerp(targetScale, originalScale, timer / 0.3f);
            timer += Time.deltaTime;
            yield return null;
        }
        
        marker.localScale = originalScale;
    }
}