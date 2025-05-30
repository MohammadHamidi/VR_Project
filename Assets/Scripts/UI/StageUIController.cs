using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageUIController : MonoBehaviour
{
    [Header("Prefabs & UI Containers")]
    [Tooltip("Prefab with RingPanelItem on its root")]
    public RingPanelItem ringItemPrefab;
    [Tooltip("Parent transform under which to spawn ring icons")]
    public Transform ringContainer;

    [Header("Text & Timer")]
    [Tooltip("Header text to describe stage")]
    public TextMeshProUGUI stageHeaderText;
    [Tooltip("Slider to show remaining time")]
    public Slider timerSlider;
    [Tooltip("Gradient for fill color based on normalized time (0 = empty, 1 = full)")]
    public Gradient fillGradient;
    [Tooltip("If true, slider will fill from right→left (and value is inverted)")]
    public bool reverseFill = false;

    // Internal
    List<RingPanelItem> _spawnedItems = new List<RingPanelItem>();
    float _maxTime;

    /// <summary>
    /// Call once at stage start to wire up your rings and UI.
    /// </summary>
    public void Initialize(
        List<TargetRing> targets,
        string initialHeader,
        bool enableTimer,
        float totalTime
    )
    {
        // Clear old icons
        foreach (var item in _spawnedItems)
            if (item) Destroy(item.gameObject);
        _spawnedItems.Clear();

        // Header
        stageHeaderText.text = initialHeader;

        // Timer
        timerSlider.gameObject.SetActive(enableTimer);
        if (enableTimer)
        {
            _maxTime = totalTime;
            timerSlider.maxValue = totalTime;
            // set fill direction
            timerSlider.direction = reverseFill
                ? Slider.Direction.RightToLeft
                : Slider.Direction.LeftToRight;
            timerSlider.value = totalTime;
        }

        // Spawn ring icons
        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var item = Instantiate(ringItemPrefab, ringContainer);
            item.name = $"RingIcon_{i}";
            _spawnedItems.Add(item);

            // flip icon when hit
            target.OnHit += _ => item.SetHit(true);
        }
    }

    /// <summary>
    /// Call every frame from your StageManager to update the slider.
    /// </summary>
    public void UpdateTimer(float timeLeft)
    {
        // clamp
        float clamped = Mathf.Clamp(timeLeft, 0f, _maxTime);
        // apply reversed logic if needed
        timerSlider.value = reverseFill
            ? (_maxTime - clamped)
            : clamped;

        // color
        if (fillGradient != null && timerSlider.fillRect != null)
        {
            float t = _maxTime > 0
                ? Mathf.InverseLerp(0f, _maxTime, clamped)
                : 0f;
            var img = timerSlider.fillRect.GetComponent<Image>();
            if (img) img.color = fillGradient.Evaluate(t);
        }
    }

    /// <summary>
    /// Change the header text on the fly.
    /// </summary>
    public void UpdateHeader(string newText)
    {
        stageHeaderText.text = newText;
    }
}
