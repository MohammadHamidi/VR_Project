using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simplified UI component for progress display.
/// Handles progress tracking, distance, speed, and milestone display.
/// </summary>
public class ProgressUI : MonoBehaviour
{
    [Header("Progress UI Components")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Image directionIndicator;

    [Header("Visual Settings")]
    [SerializeField] private Gradient progressGradient = new Gradient();
    [SerializeField] private Color forwardColor = Color.green;
    [SerializeField] private Color backwardColor = Color.red;
    [SerializeField] private Color stationaryColor = Color.gray;

    private float targetProgressValue = 0f;
    private Color targetProgressColor = Color.blue;
    
    // Balance bar target value (0..1 where 0.5 is centered)
    private bool hasBalanceTarget = false;
    private float targetBalanceValue = 0.5f;
    private Color balanceCenterColor = Color.green;
    private Color balanceEdgeColor = Color.red;

    void Start()
    {
        InitializeUI();
    }

    void Update()
    {
        // Smooth animation
        if (progressSlider != null)
        {
            float target = hasBalanceTarget ? targetBalanceValue : targetProgressValue;
            progressSlider.value = Mathf.Lerp(progressSlider.value, target, Time.deltaTime * 5f);
        }

        if (progressFill != null)
        {
            progressFill.color = Color.Lerp(progressFill.color, targetProgressColor, Time.deltaTime * 5f);
        }
    }

    private void InitializeUI()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        if (progressFill == null && progressSlider != null)
        {
            progressFill = progressSlider.fillRect?.GetComponent<Image>();
        }

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
        UpdateDistanceText(0f, 0f);
        UpdateSpeedText(0f);
    }

    /// <summary>
    /// Updates the progress UI with current progress data
    /// </summary>
    public void UpdateProgress(float progress, float distanceTraveled, float remainingDistance, float movementSpeed)
    {
        hasBalanceTarget = false; // show progress when called
        targetProgressValue = progress;
        targetProgressColor = progressGradient.Evaluate(progress);

        UpdateProgressText(progress);
        UpdateDistanceText(distanceTraveled, remainingDistance);
        UpdateSpeedText(movementSpeed);
    }

    /// <summary>
    /// Updates the balance bar using signed lateral offset. Center (0) = balanced.
    /// offsetX is player's local X offset; maxOffsetX is the threshold for failure.
    /// </summary>
    public void UpdateBalanceBar(float offsetX, float maxOffsetX)
    {
        if (progressSlider == null) return;

        // Normalize to [-1, 1] then map to [0, 1] with center at 0.5
        float normalized = 0f;
        if (maxOffsetX > 0.0001f)
            normalized = Mathf.Clamp(offsetX / maxOffsetX, -1f, 1f);

        float mapped = 0.5f + 0.5f * normalized; // left=0, center=0.5, right=1
        targetBalanceValue = mapped;
        hasBalanceTarget = true;

        // Color based on distance from center
        float distanceFromCenter = Mathf.Abs(normalized);
        targetProgressColor = Color.Lerp(balanceCenterColor, balanceEdgeColor, distanceFromCenter);
    }

    /// <summary>
    /// Updates movement direction indicator
    /// </summary>
    public void UpdateMovementDirection(float speed, bool isMovingForward)
    {
        if (directionIndicator == null) return;

        if (speed < 0.01f) // Stationary
        {
            directionIndicator.color = stationaryColor;
        }
        else if (isMovingForward)
        {
            directionIndicator.color = forwardColor;
        }
        else
        {
            directionIndicator.color = backwardColor;
        }
    }

    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {progress:P1}";
        }
    }

    private void UpdateDistanceText(float traveled, float remaining)
    {
        if (distanceText != null)
        {
            distanceText.text = $"{traveled:F1}m traveled\n{remaining:F1}m remaining";
        }
    }

    private void UpdateSpeedText(float speed)
    {
        if (speedText != null)
        {
            speedText.text = $"Speed: {speed:F2} m/s";
        }
    }

    /// <summary>
    /// Shows a completion message
    /// </summary>
    public void ShowCompletionMessage()
    {
        StartCoroutine(ShowTemporaryMessage("Bridge Crossed Successfully!", 3f));
    }

    /// <summary>
    /// Shows a milestone message
    /// </summary>
    public void ShowMilestoneMessage(string message)
    {
        StartCoroutine(ShowTemporaryMessage(message, 2f));
    }

    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        if (progressText != null)
        {
            string originalText = progressText.text;
            progressText.text = message;

            yield return new WaitForSeconds(duration);

            progressText.text = originalText;
        }
    }

    /// <summary>
    /// Resets the progress UI to initial state
    /// </summary>
    public void ResetProgress()
    {
        targetProgressValue = 0f;

        if (progressSlider != null) progressSlider.value = 0f;

        UpdateProgressText(0f);
        UpdateDistanceText(0f, 0f);
        UpdateSpeedText(0f);
    }

    /// <summary>
    /// Sets the visibility of the progress UI
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
