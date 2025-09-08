using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simplified UI component for balance display.
/// Handles balance slider, messages, and visual feedback.
/// </summary>
public class BalanceUI : MonoBehaviour
{
    [Header("Balance UI Components")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Slider balanceSlider;
    [SerializeField] private Image balanceFill;

    [Header("Visual Settings")]
    [SerializeField] private Gradient balanceGradient = new Gradient();
    [SerializeField] private string[] balanceMessages = {
        "Perfect Balance!",
        "Keep steady...",
        "Careful now!",
        "Too far - lean back!"
    };

    private float targetSliderValue = 0.5f;
    private Color targetBalanceColor = Color.green;

    void Start()
    {
        InitializeUI();
    }

    void Update()
    {
        // Smooth animation
        if (balanceSlider != null)
        {
            balanceSlider.value = Mathf.Lerp(balanceSlider.value, targetSliderValue, Time.deltaTime * 5f);
        }

        if (balanceFill != null)
        {
            balanceFill.color = Color.Lerp(balanceFill.color, targetBalanceColor, Time.deltaTime * 5f);
        }
    }

    private void InitializeUI()
    {
        if (balanceSlider != null)
        {
            balanceSlider.minValue = 0f;
            balanceSlider.maxValue = 1f;
            balanceSlider.value = 0.5f;
        }

        if (balanceFill == null && balanceSlider != null)
        {
            balanceFill = balanceSlider.fillRect?.GetComponent<Image>();
        }

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

    /// <summary>
    /// Updates the balance UI with current offset and max offset
    /// </summary>
    public void UpdateBalance(float currentOffset, float maxOffset)
    {
        // Calculate normalized balance value [0, 1]
        float normalizedBalance = Mathf.InverseLerp(0f, maxOffset, Mathf.Abs(currentOffset));
        targetSliderValue = normalizedBalance;

        // Update color
        targetBalanceColor = balanceGradient.Evaluate(normalizedBalance);

        // Update text message
        UpdateBalanceMessage(normalizedBalance);
    }

    private void UpdateBalanceMessage(float balanceValue)
    {
        if (promptText == null || balanceMessages.Length == 0) return;

        int messageIndex = Mathf.FloorToInt(balanceValue * (balanceMessages.Length - 1));
        messageIndex = Mathf.Clamp(messageIndex, 0, balanceMessages.Length - 1);
        promptText.text = balanceMessages[messageIndex];
    }

    /// <summary>
    /// Shows a temporary warning message
    /// </summary>
    public void ShowWarning(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.color = Color.red;
            Invoke(nameof(ResetMessageColor), 1f);
        }
    }

    private void ResetMessageColor()
    {
        if (promptText != null)
        {
            promptText.color = Color.white;
        }
    }

    /// <summary>
    /// Sets the visibility of the balance UI
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
