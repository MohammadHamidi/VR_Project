using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BridgeUIController : MonoBehaviour
{
    [Header("UI Components")]
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

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationSpeed = 5f;

    private float targetSliderValue = 0.5f;
    private Color targetColor = Color.green;

    void Start()
    {
        InitializeUI();
    }

    void Update()
    {
        if (useAnimation)
            AnimateUI();
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
            balanceFill = balanceSlider.fillRect?.GetComponent<Image>();

        // Setup default gradient if none assigned
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

    public void UpdateBalanceUI(float currentOffset, float maxOffset)
    {
        // Calculate normalized balance value [0, 1]
        float normalizedBalance = Mathf.InverseLerp(0f, maxOffset, Mathf.Abs(currentOffset));
        targetSliderValue = normalizedBalance;

        // Update color
        targetColor = balanceGradient.Evaluate(normalizedBalance);

        // Update text message
        UpdateBalanceMessage(normalizedBalance);

        // If not using animation, update immediately
        if (!useAnimation)
        {
            if (balanceSlider != null)
                balanceSlider.value = targetSliderValue;
            if (balanceFill != null)
                balanceFill.color = targetColor;
        }
    }

    private void UpdateBalanceMessage(float balanceValue)
    {
        if (promptText == null || balanceMessages.Length == 0) return;

        int messageIndex = Mathf.FloorToInt(balanceValue * (balanceMessages.Length - 1));
        messageIndex = Mathf.Clamp(messageIndex, 0, balanceMessages.Length - 1);
        promptText.text = balanceMessages[messageIndex];
    }

    private void AnimateUI()
    {
        if (balanceSlider != null)
        {
            balanceSlider.value = Mathf.Lerp(balanceSlider.value, targetSliderValue, 
                animationSpeed * Time.deltaTime);
        }

        if (balanceFill != null)
        {
            balanceFill.color = Color.Lerp(balanceFill.color, targetColor, 
                animationSpeed * Time.deltaTime);
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

    private System.Collections.IEnumerator PulseTextCoroutine()
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
}