using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRRehab.UI;
using VRRehab.DataPersistence;

namespace VRRehab.SceneBuilders
{
    public class SettingsBuilder : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private DataPersistenceManager dataManager;

        [Header("UI Configuration")]
        [SerializeField] private Color primaryColor = new Color(0.3f, 0.5f, 0.7f);
        [SerializeField] private Color secondaryColor = new Color(0.4f, 0.6f, 0.8f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.15f, 0.2f);
        [SerializeField] private Color panelColor = new Color(0.15f, 0.2f, 0.25f);

        [Header("Audio Settings")]
        [SerializeField] private AudioClip[] testSounds;
        [SerializeField] private AudioSource settingsAudioSource;

        // Settings data
        private Dictionary<string, object> currentSettings;
        private bool hasUnsavedChanges = false;

        void Start()
        {
            StartCoroutine(BuildSettingsScene());
        }

        private IEnumerator BuildSettingsScene()
        {
            // Find required systems
            FindRequiredSystems();

            // Create main canvas and layout
            yield return CreateMainCanvas();

            // Create header section
            CreateHeaderSection();

            // Create settings panels
            yield return CreateSettingsPanels();

            // Load current settings
            LoadCurrentSettings();

            Debug.Log("Settings scene built successfully");
        }

        private void FindRequiredSystems()
        {
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();

            if (dataManager == null)
                dataManager = FindObjectOfType<DataPersistenceManager>();

            if (settingsAudioSource == null)
                settingsAudioSource = gameObject.AddComponent<AudioSource>();
        }

        private IEnumerator CreateMainCanvas()
        {
            // Create canvas if it doesn't exist
            GameObject canvasObj = GameObject.Find("MainCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("MainCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add background
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = backgroundColor;

                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
            }

            yield return null;
        }

        private void CreateHeaderSection()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Create header panel
            GameObject headerPanel = new GameObject("HeaderPanel");
            headerPanel.transform.SetParent(canvas.transform, false);

            Image headerImage = headerPanel.AddComponent<Image>();
            headerImage.color = primaryColor;

            RectTransform headerRect = headerPanel.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = Vector2.zero;

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerPanel.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Settings & Preferences";
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.3f, 0.1f);
            titleRect.anchorMax = new Vector2(0.7f, 0.9f);
            titleRect.sizeDelta = Vector2.zero;

            // Create navigation buttons
            CreateHeaderButton(headerPanel, "Back", "MainMenu", new Vector2(0.02f, 0.2f), new Vector2(0.12f, 0.8f));
            CreateHeaderButton(headerPanel, "Save", "", new Vector2(0.85f, 0.2f), new Vector2(0.95f, 0.8f));
        }

        private IEnumerator CreateSettingsPanels()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) yield break;

            // Create left panel for general settings
            CreateGeneralSettingsPanel(canvas);

            // Create middle panel for accessibility settings
            CreateAccessibilityPanel(canvas);

            // Create right panel for system settings
            CreateSystemSettingsPanel(canvas);

            // Create bottom panel for action buttons
            CreateActionPanel(canvas);

            yield return null;
        }

        private void CreateGeneralSettingsPanel(GameObject canvas)
        {
            GameObject panel = CreateSettingsPanel(canvas, "General Settings", new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.88f));

            string[] generalSettings = {
                "Enable Voice Guidance",
                "Enable Haptic Feedback",
                "Enable Progress Notifications",
                "Auto-save Progress",
                "Show Performance Tips"
            };

            float yPos = 0.85f;
            foreach (string setting in generalSettings)
            {
                CreateToggleSetting(panel, setting, new Vector2(0.05f, yPos), new Vector2(0.95f, yPos + 0.08f));
                yPos -= 0.1f;
            }
        }

        private void CreateAccessibilityPanel(GameObject canvas)
        {
            GameObject panel = CreateSettingsPanel(canvas, "Accessibility", new Vector2(0.34f, 0.08f), new Vector2(0.64f, 0.88f));

            // High contrast toggle
            CreateToggleSetting(panel, "High Contrast Mode", new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.93f));

            // Large print toggle
            CreateToggleSetting(panel, "Large Print Mode", new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.83f));

            // Text scale slider
            CreateSliderSetting(panel, "Text Scale", 0.5f, 2.0f, 1.0f, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.68f));

            // Voice over speed slider
            CreateSliderSetting(panel, "Voice Speed", 0.5f, 2.0f, 1.0f, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.58f));

            // Color theme selector
            CreateDropdownSetting(panel, "Color Theme", new[] { "Default", "High Contrast", "Blue", "Green", "Purple" },
                                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.43f));
        }

        private void CreateSystemSettingsPanel(GameObject canvas)
        {
            GameObject panel = CreateSettingsPanel(canvas, "System Settings", new Vector2(0.66f, 0.08f), new Vector2(0.98f, 0.88f));

            // Audio settings
            CreateSliderSetting(panel, "Master Volume", 0f, 1f, 0.8f, new Vector2(0.05f, 0.85f), new Vector2(0.8f, 0.93f));
            CreateButtonSetting(panel, "Test Sound", new Vector2(0.82f, 0.85f), new Vector2(0.95f, 0.93f));

            // Performance settings
            CreateSliderSetting(panel, "Target FPS", 30f, 90f, 60f, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.83f));
            CreateDropdownSetting(panel, "Quality Level", new[] { "Low", "Medium", "High", "Ultra" },
                                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.68f));

            // Data management
            CreateButtonSetting(panel, "Export Data", new Vector2(0.05f, 0.5f), new Vector2(0.45f, 0.58f));
            CreateButtonSetting(panel, "Import Data", new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.58f));
            CreateButtonSetting(panel, "Clear All Data", new Vector2(0.05f, 0.4f), new Vector2(0.45f, 0.48f), true);

            // System info
            CreateInfoDisplay(panel, "System Information", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.38f));
        }

        private void CreateActionPanel(GameObject canvas)
        {
            GameObject actionPanel = new GameObject("ActionPanel");
            actionPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = actionPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.15f, 0.2f);

            RectTransform panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.08f);
            panelRect.sizeDelta = Vector2.zero;

            // Create action buttons
            CreateActionButton(actionPanel, "Apply Changes", new Vector2(0.7f, 0.1f), new Vector2(0.85f, 0.9f));
            CreateActionButton(actionPanel, "Reset to Defaults", new Vector2(0.02f, 0.1f), new Vector2(0.17f, 0.9f));
            CreateActionButton(actionPanel, "Cancel", new Vector2(0.87f, 0.1f), new Vector2(0.98f, 0.9f));

            // Create status text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(actionPanel.transform, false);

            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "Settings loaded successfully";
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;

            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.2f, 0.1f);
            statusRect.anchorMax = new Vector2(0.68f, 0.9f);
            statusRect.sizeDelta = Vector2.zero;
        }

        private GameObject CreateSettingsPanel(GameObject canvas, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject($"{title.Replace(" ", "")}Panel");
            panel.transform.SetParent(canvas.transform, false);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = panelColor;

            // Panel title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 20;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.92f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.sizeDelta = Vector2.zero;

            return panel;
        }

        private void CreateToggleSetting(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject settingObj = new GameObject($"{label.Replace(" ", "")}Setting");
            settingObj.transform.SetParent(parent.transform, false);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.1f);
            labelRect.anchorMax = new Vector2(0.7f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(settingObj.transform, false);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = GetSettingValue<bool>(label, false);

            // Create toggle background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.35f, 0.4f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(40, 20);

            // Create checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = primaryColor;

            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.sizeDelta = Vector2.zero;

            toggle.graphic = checkImage;

            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.75f, 0.1f);
            toggleRect.anchorMax = new Vector2(0.95f, 0.9f);
            toggleRect.sizeDelta = Vector2.zero;

            // Add change listener
            toggle.onValueChanged.AddListener((value) => OnSettingChanged(label, value));

            RectTransform settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.anchorMin = anchorMin;
            settingRect.anchorMax = anchorMax;
            settingRect.sizeDelta = Vector2.zero;
        }

        private void CreateSliderSetting(GameObject parent, string label, float minValue, float maxValue, float defaultValue, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject settingObj = new GameObject($"{label.Replace(" ", "")}Setting");
            settingObj.transform.SetParent(parent.transform, false);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(0.3f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(settingObj.transform, false);

            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = defaultValue.ToString("F1");
            valueText.fontSize = 12;
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.color = primaryColor;

            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.7f, 0.6f);
            valueRect.anchorMax = new Vector2(0.95f, 0.9f);
            valueRect.sizeDelta = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(settingObj.transform, false);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = GetSettingValue<float>(label, defaultValue);
            slider.wholeNumbers = false;

            // Slider background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.35f, 0.4f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.4f);
            bgRect.anchorMax = new Vector2(1, 0.6f);
            bgRect.sizeDelta = Vector2.zero;

            // Slider fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = primaryColor;

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = Vector2.zero;

            // Slider handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = secondaryColor;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.32f, 0.4f);
            sliderRect.anchorMax = new Vector2(0.65f, 0.6f);
            sliderRect.sizeDelta = Vector2.zero;

            // Add change listeners
            slider.onValueChanged.AddListener((value) => {
                valueText.text = value.ToString("F1");
                OnSettingChanged(label, value);
            });

            RectTransform settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.anchorMin = anchorMin;
            settingRect.anchorMax = anchorMax;
            settingRect.sizeDelta = Vector2.zero;
        }

        private void CreateDropdownSetting(GameObject parent, string label, string[] options, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject settingObj = new GameObject($"{label.Replace(" ", "")}Setting");
            settingObj.transform.SetParent(parent.transform, false);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.1f);
            labelRect.anchorMax = new Vector2(0.3f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Dropdown
            GameObject dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(settingObj.transform, false);

            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }
            dropdown.value = GetSettingValue<int>(label, 0);

            // Dropdown template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.25f, 0.3f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(templateObj.transform, false);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            TextMeshProUGUI itemText = itemObj.AddComponent<TextMeshProUGUI>();
            itemText.text = "Option";
            itemText.fontSize = 14;
            itemText.color = Color.white;

            dropdown.template = templateObj.GetComponent<RectTransform>();

            // Dropdown label
            GameObject dropdownLabelObj = new GameObject("Label");
            dropdownLabelObj.transform.SetParent(dropdownObj.transform, false);

            TextMeshProUGUI dropdownLabel = dropdownLabelObj.AddComponent<TextMeshProUGUI>();
            dropdownLabel.text = options[0];
            dropdownLabel.fontSize = 14;
            dropdownLabel.color = Color.white;

            // Dropdown arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);

            Image arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.color = Color.white;

            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.9f, 0.1f);
            arrowRect.anchorMax = new Vector2(0.95f, 0.9f);
            arrowRect.sizeDelta = Vector2.zero;

            RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.35f, 0.1f);
            dropdownRect.anchorMax = new Vector2(0.95f, 0.9f);
            dropdownRect.sizeDelta = Vector2.zero;

            // Add change listener
            dropdown.onValueChanged.AddListener((value) => OnSettingChanged(label, value));

            RectTransform settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.anchorMin = anchorMin;
            settingRect.anchorMax = anchorMax;
            settingRect.sizeDelta = Vector2.zero;
        }

        private void CreateButtonSetting(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax, bool isDestructive = false)
        {
            GameObject buttonObj = new GameObject($"{label.Replace(" ", "")}Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = isDestructive ? new Color(0.8f, 0.3f, 0.3f) : secondaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = isDestructive ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = isDestructive ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 12;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OnButtonSettingClicked(label));
        }

        private void CreateInfoDisplay(GameObject parent, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject infoObj = new GameObject($"{title.Replace(" ", "")}Info");
            infoObj.transform.SetParent(parent.transform, false);

            // Background
            Image bgImage = infoObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.3f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(infoObj.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 14;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.8f);
            titleRect.anchorMax = new Vector2(0.95f, 0.9f);
            titleRect.sizeDelta = Vector2.zero;

            // Info text
            GameObject textObj = new GameObject("InfoText");
            textObj.transform.SetParent(infoObj.transform, false);

            TextMeshProUGUI infoText = textObj.AddComponent<TextMeshProUGUI>();
            infoText.text = GenerateSystemInfoText();
            infoText.fontSize = 10;
            infoText.color = new Color(0.8f, 0.8f, 0.8f);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.75f);
            textRect.sizeDelta = Vector2.zero;

            RectTransform infoRect = infoObj.GetComponent<RectTransform>();
            infoRect.anchorMin = anchorMin;
            infoRect.anchorMax = anchorMax;
            infoRect.sizeDelta = Vector2.zero;
        }

        private void CreateHeaderButton(GameObject parent, string label, string targetScene, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"Header{label}Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = secondaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 16;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OnHeaderButtonClicked(label, targetScene));
        }

        private void CreateActionButton(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"{label.Replace(" ", "")}Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = primaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 14;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OnActionButtonClicked(label));
        }

        // Helper methods
        private string GenerateSystemInfoText()
        {
            return $"Unity Version: {Application.unityVersion}\n" +
                   $"Platform: {Application.platform}\n" +
                   $"System Memory: {SystemInfo.systemMemorySize}MB\n" +
                   $"Graphics: {SystemInfo.graphicsDeviceName}\n" +
                   $"VR Supported: {UnityEngine.XR.XRSettings.isDeviceActive}\n" +
                   $"Screen: {Screen.currentResolution}";
        }

        private T GetSettingValue<T>(string settingName, T defaultValue)
        {
            string key = $"Setting_{settingName.Replace(" ", "")}";
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)PlayerPrefs.GetInt(key, (bool)(object)defaultValue ? 1 : 0);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)PlayerPrefs.GetFloat(key, (float)(object)defaultValue);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)PlayerPrefs.GetInt(key, (int)(object)defaultValue);
            }
            return defaultValue;
        }

        private void LoadCurrentSettings()
        {
            currentSettings = new Dictionary<string, object>();

            // Load all settings from PlayerPrefs
            string[] settingKeys = {
                "EnableVoiceGuidance", "EnableHapticFeedback", "EnableProgressNotifications",
                "AutoSaveProgress", "ShowPerformanceTips", "HighContrastMode", "LargePrintMode",
                "TextScale", "VoiceOverSpeed", "MasterVolume", "TargetFPS", "QualityLevel"
            };

            foreach (string key in settingKeys)
            {
                if (PlayerPrefs.HasKey($"Setting_{key}"))
                {
                    // Load based on type (simplified - you'd want to track types)
                    currentSettings[key] = PlayerPrefs.GetFloat($"Setting_{key}", 0f);
                }
            }
        }

        // Event handlers
        private void OnSettingChanged(string settingName, object value)
        {
            hasUnsavedChanges = true;
            string key = $"Setting_{settingName.Replace(" ", "")}";

            if (value is bool boolValue)
            {
                PlayerPrefs.SetInt(key, boolValue ? 1 : 0);
            }
            else if (value is float floatValue)
            {
                PlayerPrefs.SetFloat(key, floatValue);
            }
            else if (value is int intValue)
            {
                PlayerPrefs.SetInt(key, intValue);
            }

            // Apply setting immediately if possible
            ApplySetting(settingName, value);
        }

        private void OnButtonSettingClicked(string buttonName)
        {
            switch (buttonName)
            {
                case "Test Sound":
                    PlayTestSound();
                    break;
                case "Export Data":
                    ExportData();
                    break;
                case "Import Data":
                    ImportData();
                    break;
                case "Clear All Data":
                    ClearAllData();
                    break;
            }
        }

        private void OnHeaderButtonClicked(string label, string targetScene)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            if (label == "Save")
            {
                SaveSettings();
            }
            else if (!string.IsNullOrEmpty(targetScene))
            {
                var sceneTransitionManager = FindObjectOfType<VRRehab.SceneManagement.SceneTransitionManager>();
                if (sceneTransitionManager != null)
                {
                    sceneTransitionManager.LoadScene(targetScene);
                }
                else
                {
                    SceneManager.LoadScene(targetScene);
                }
            }
        }

        private void OnActionButtonClicked(string buttonName)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            switch (buttonName)
            {
                case "Apply Changes":
                    ApplyAllSettings();
                    break;
                case "Reset to Defaults":
                    ResetToDefaults();
                    break;
                case "Cancel":
                    CancelChanges();
                    break;
            }
        }

        // Setting application methods
        private void ApplySetting(string settingName, object value)
        {
            switch (settingName)
            {
                case "Enable Voice Guidance":
                    if (uiManager != null) uiManager.EnableVoiceFeedback = (bool)value;
                    break;
                case "Enable Haptic Feedback":
                    if (uiManager != null) uiManager.EnableHapticFeedback = (bool)value;
                    break;
                case "High Contrast Mode":
                    if (uiManager != null) uiManager.ToggleHighContrastMode();
                    break;
                case "Large Print Mode":
                    if (uiManager != null) uiManager.ToggleLargePrintMode();
                    break;
                case "Text Scale":
                    if (uiManager != null) uiManager.AdjustTextScale((float)value);
                    break;
                case "Voice Speed":
                    if (uiManager != null) uiManager.AdjustVoiceOverSpeed((float)value);
                    break;
                case "Master Volume":
                    AudioListener.volume = (float)value;
                    break;
            }
        }

        private void ApplyAllSettings()
        {
            PlayerPrefs.Save();
            hasUnsavedChanges = false;

            if (uiManager != null)
            {
                uiManager.ShowSuccess("Settings applied successfully!");
            }

            Debug.Log("All settings applied");
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
            hasUnsavedChanges = false;

            if (uiManager != null)
            {
                uiManager.ShowSuccess("Settings saved successfully!");
            }

            Debug.Log("Settings saved");
        }

        private void ResetToDefaults()
        {
            PlayerPrefs.DeleteAll();
            hasUnsavedChanges = true;

            // Reset UI elements to defaults
            ResetAllUIElements();

            if (uiManager != null)
            {
                uiManager.ShowWarning("Settings reset to defaults");
            }
        }

        private void CancelChanges()
        {
            if (hasUnsavedChanges)
            {
                LoadCurrentSettings();
                ResetAllUIElements();

                if (uiManager != null)
                {
                    uiManager.ShowNotification("Changes cancelled", VRRehab.UI.UIManager.NotificationData.NotificationType.Info);
                }
            }

            hasUnsavedChanges = false;
        }

        private void ResetAllUIElements()
        {
            // Reset all UI elements to their saved/default values
            // This would be implemented to reset each toggle, slider, etc.
            Debug.Log("UI elements reset to saved values");
        }

        // Utility methods
        private void PlayTestSound()
        {
            if (settingsAudioSource != null && testSounds.Length > 0)
            {
                settingsAudioSource.PlayOneShot(testSounds[0]);
            }
        }

        private void ExportData()
        {
            // Implement data export functionality
            if (uiManager != null)
            {
                uiManager.ShowSuccess("Data export feature coming soon!");
            }
        }

        private void ImportData()
        {
            // Implement data import functionality
            if (uiManager != null)
            {
                uiManager.ShowSuccess("Data import feature coming soon!");
            }
        }

        private void ClearAllData()
        {
            // Implement data clearing with confirmation
            if (uiManager != null)
            {
                uiManager.ShowError("Data clearing requires confirmation!");
            }
        }

        // Public methods
        public bool HasUnsavedChanges()
        {
            return hasUnsavedChanges;
        }

        public void ForceSave()
        {
            SaveSettings();
        }

        public Dictionary<string, object> GetCurrentSettings()
        {
            return currentSettings;
        }
    }
}
