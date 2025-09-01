using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.SceneManagement;

namespace VRRehab.UI
{
    public class UIManager : MonoBehaviour
    {
        [System.Serializable]
        public class UITheme
        {
            public string themeName;
            public Color primaryColor;
            public Color secondaryColor;
            public Color accentColor;
            public Color successColor = Color.green;
            public Color warningColor = Color.yellow;
            public Color errorColor = Color.red;
            public Color backgroundColor;
            public Color textColor;
            public Font textFont;
            public float animationSpeed = 1f;
        }

        [System.Serializable]
        public class NotificationSettings
        {
            public float displayDuration = 3f;
            public float fadeInDuration = 0.3f;
            public float fadeOutDuration = 0.3f;
            public Vector2 notificationOffset = new Vector2(0, 100);
            public int maxNotifications = 5;
        }

        [Header("UI Configuration")]
        [SerializeField] private UITheme[] availableThemes;
        [SerializeField] private NotificationSettings notificationSettings;
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private bool enableHapticFeedback = true;
        [SerializeField] private bool enableVoiceFeedback = true;

        // Public properties for external access
        public bool EnableHapticFeedback
        {
            get { return enableHapticFeedback; }
            set { enableHapticFeedback = value; }
        }

        public bool EnableVoiceFeedback
        {
            get { return enableVoiceFeedback; }
            set { enableVoiceFeedback = value; }
        }

        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Feedback Systems")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip notificationSound;

        [Header("Accessibility")]
        [SerializeField] private float textScale = 1f;
        [SerializeField] private bool highContrastMode = false;
        [SerializeField] private bool largePrintMode = false;
        [SerializeField] private float voiceOverSpeed = 1f;

        // Current state
        private UITheme currentTheme;
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<GameObject> activeNotifications = new List<GameObject>();
        private Coroutine notificationCoroutine;
        private Dictionary<string, GameObject> uiPanels = new Dictionary<string, GameObject>();

        // Events
        public static event Action<UITheme> OnThemeChanged;
        public static event Action<string> OnNotificationShown;
        public static event Action OnUIReset;

        void Awake()
        {
            InitializeUI();
            SubscribeToEvents();
            SetDefaultTheme();
        }

        private void InitializeUI()
        {
            if (mainCanvas != null)
                DontDestroyOnLoad(mainCanvas.gameObject);

            // Initialize notification system
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }

            // Initialize loading indicator
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }

            // Cache UI panels for quick access
            CacheUIPanels();
        }

        private void CacheUIPanels()
        {
            // Find and cache common UI panels
            try
            {
                GameObject[] panels = GameObject.FindGameObjectsWithTag("UIPanel");
                foreach (GameObject panel in panels)
                {
                    uiPanels[panel.name] = panel;
                }
            }
            catch (UnityException e)
            {
                Debug.LogWarning($"UIPanel tag not found: {e.Message}. Using fallback UI panel detection.");
                // Fallback: find all GameObjects that might be UI panels
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Panel") || obj.name.Contains("UI") || obj.GetComponent<Canvas>() != null)
                    {
                        uiPanels[obj.name] = obj;
                    }
                }
            }
        }

        private void SubscribeToEvents()
        {
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        #region Theme Management

        public void SetTheme(string themeName)
        {
            UITheme theme = GetThemeByName(themeName);
            if (theme != null)
            {
                currentTheme = theme;
                ApplyTheme(theme);
                OnThemeChanged?.Invoke(theme);
                SaveThemePreference(themeName);
            }
        }

        public void SetTheme(UITheme theme)
        {
            if (theme != null)
            {
                currentTheme = theme;
                ApplyTheme(theme);
                OnThemeChanged?.Invoke(theme);
            }
        }

        private void SetDefaultTheme()
        {
            string savedTheme = PlayerPrefs.GetString("PreferredTheme", "Default");
            UITheme theme = GetThemeByName(savedTheme) ?? availableThemes[0];
            SetTheme(theme);
        }

        private UITheme GetThemeByName(string themeName)
        {
            foreach (UITheme theme in availableThemes)
            {
                if (theme.themeName == themeName)
                {
                    return theme;
                }
            }
            return null;
        }

        private void ApplyTheme(UITheme theme)
        {
            // Apply theme to all UI elements
            Graphic[] graphics = FindObjectsOfType<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                ApplyThemeToGraphic(graphic, theme);
            }

            // Apply to TextMeshPro components
            TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in textComponents)
            {
                ApplyThemeToText(text, theme);
            }

            // Apply animations speed
            Animator[] animators = FindObjectsOfType<Animator>();
            foreach (Animator animator in animators)
            {
                animator.speed = theme.animationSpeed;
            }
        }

        private void ApplyThemeToGraphic(Graphic graphic, UITheme theme)
        {
            Image image = graphic as Image;
            if (image != null)
            {
                // Apply colors based on component type
                if (graphic.name.Contains("Background"))
                {
                    image.color = theme.backgroundColor;
                }
                else if (graphic.name.Contains("Button"))
                {
                    image.color = theme.primaryColor;
                }
                else if (graphic.name.Contains("Accent"))
                {
                    image.color = theme.accentColor;
                }
            }
        }

        private void ApplyThemeToText(TextMeshProUGUI text, UITheme theme)
        {
            text.color = theme.textColor;
            if (theme.textFont != null)
            {
                text.font = TMP_FontAsset.CreateFontAsset(theme.textFont);
            }

            // Apply accessibility scaling
            float scale = textScale;
            if (largePrintMode) scale *= 1.5f;
            text.fontSize *= scale;
        }

        private void SaveThemePreference(string themeName)
        {
            PlayerPrefs.SetString("PreferredTheme", themeName);
            PlayerPrefs.Save();
        }

        #endregion

        #region Notification System

        [System.Serializable]
        public class NotificationData
        {
            public string message;
            public NotificationType type;
            public float duration;
            public Sprite icon;
            public Action onClick;

            public enum NotificationType
            {
                Info,
                Success,
                Warning,
                Error,
                Achievement
            }
        }

        public void ShowNotification(string message, NotificationData.NotificationType type = NotificationData.NotificationType.Info, float duration = 0f, Sprite icon = null, Action onClick = null)
        {
            // Ensure notification settings exist
            if (notificationSettings == null)
            {
                notificationSettings = new NotificationSettings();
                Debug.Log("Created default notification settings");
            }

            NotificationData notification = new NotificationData
            {
                message = message,
                type = type,
                duration = duration > 0 ? duration : notificationSettings.displayDuration,
                icon = icon,
                onClick = onClick
            };

            notificationQueue.Enqueue(notification);

            if (notificationCoroutine == null)
            {
                notificationCoroutine = StartCoroutine(ProcessNotifications());
            }

            OnNotificationShown?.Invoke(message);
        }

        public void ShowSuccess(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationData.NotificationType.Success, duration);
            PlayFeedbackSound(successSound);
        }

        public void ShowError(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationData.NotificationType.Error, duration);
            PlayFeedbackSound(errorSound);
        }

        public void ShowWarning(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationData.NotificationType.Warning, duration);
            PlayFeedbackSound(warningSound);
        }

        public void ShowAchievement(string achievementName, string description)
        {
            string message = $"🏆 Achievement Unlocked!\n{achievementName}\n{description}";
            ShowNotification(message, NotificationData.NotificationType.Achievement, 5f);
            PlayFeedbackSound(successSound);
        }

        private IEnumerator ProcessNotifications()
        {
            while (notificationQueue.Count > 0)
            {
                NotificationData notification = notificationQueue.Dequeue();

                // Limit concurrent notifications
                if (notificationSettings != null && activeNotifications.Count >= notificationSettings.maxNotifications)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                yield return StartCoroutine(DisplayNotification(notification));
                yield return new WaitForSeconds(0.2f); // Brief pause between notifications
            }

            notificationCoroutine = null;
        }

        private IEnumerator DisplayNotification(NotificationData notification)
        {
            // Create notification instance
            GameObject notificationInstance = Instantiate(notificationPanel, mainCanvas.transform);
            activeNotifications.Add(notificationInstance);

            // Setup notification content
            TextMeshProUGUI text = notificationInstance.GetComponentInChildren<TextMeshProUGUI>();
            Image background = notificationInstance.GetComponent<Image>();
            Button button = notificationInstance.GetComponent<Button>();

            if (text != null)
            {
                text.text = notification.message;
                text.color = GetNotificationColor(notification.type);
            }

            if (background != null)
            {
                background.color = GetNotificationBackgroundColor(notification.type);
            }

            if (button != null && notification.onClick != null)
            {
                button.onClick.AddListener(() => notification.onClick());
            }

            // Position notification
            RectTransform rectTransform = notificationInstance.GetComponent<RectTransform>();
            Vector2 basePosition = notificationSettings != null
                ? new Vector2(0, -notificationSettings.notificationOffset.y * activeNotifications.Count)
                : new Vector2(0, -100f * activeNotifications.Count);
            rectTransform.anchoredPosition = basePosition + new Vector2(0, 1000); // Start off-screen

            // Animate in
            yield return StartCoroutine(AnimateNotificationIn(notificationInstance, basePosition));

            // Wait for duration
            yield return new WaitForSeconds(notification.duration);

            // Animate out
            yield return StartCoroutine(AnimateNotificationOut(notificationInstance));

            // Cleanup
            activeNotifications.Remove(notificationInstance);
            Destroy(notificationInstance);
        }

        private IEnumerator AnimateNotificationIn(GameObject notification, Vector2 targetPosition)
        {
            RectTransform rectTransform = notification.GetComponent<RectTransform>();
            Vector2 startPosition = rectTransform.anchoredPosition;
            CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = notification.AddComponent<CanvasGroup>();

            float elapsedTime = 0f;

            float fadeInDuration = notificationSettings != null ? notificationSettings.fadeInDuration : 0.3f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeInDuration;

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                canvasGroup.alpha = t;

                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
            canvasGroup.alpha = 1f;
        }

        private IEnumerator AnimateNotificationOut(GameObject notification)
        {
            RectTransform rectTransform = notification.GetComponent<RectTransform>();
            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 endPosition = startPosition + new Vector2(0, -100);
            CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();

            float elapsedTime = 0f;

            float fadeOutDuration = notificationSettings != null ? notificationSettings.fadeOutDuration : 0.3f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeOutDuration;

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
                canvasGroup.alpha = 1f - t;

                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        private Color GetNotificationColor(NotificationData.NotificationType type)
        {
            if (currentTheme == null) return Color.white;

            switch (type)
            {
                case NotificationData.NotificationType.Success:
                    return currentTheme.successColor;
                case NotificationData.NotificationType.Warning:
                    return currentTheme.warningColor;
                case NotificationData.NotificationType.Error:
                    return currentTheme.errorColor;
                case NotificationData.NotificationType.Achievement:
                    return currentTheme.accentColor;
                default:
                    return currentTheme.textColor;
            }
        }

        private Color GetNotificationBackgroundColor(NotificationData.NotificationType type)
        {
            Color color = GetNotificationColor(type);
            return new Color(color.r, color.g, color.b, 0.9f);
        }

        #endregion

        #region Loading and Progress

        public void ShowLoading(string message = "Loading...")
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
                UpdateLoadingText(message);
            }
        }

        public void HideLoading()
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }

        public void UpdateLoadingText(string message)
        {
            TextMeshProUGUI text = loadingIndicator?.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }
        }

        public void UpdateProgress(float progress, string statusText = "")
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }

            if (this.statusText != null && !string.IsNullOrEmpty(statusText))
            {
                this.statusText.text = statusText;
            }
        }

        public void ShowProgress(string message, float progress)
        {
            UpdateProgress(progress, message);

            if (progressBar != null && !progressBar.gameObject.activeSelf)
            {
                progressBar.gameObject.SetActive(true);
            }
        }

        public void HideProgress()
        {
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Feedback Systems

        public void PlayButtonClick()
        {
            PlayFeedbackSound(buttonClickSound);
            if (enableHapticFeedback)
            {
                TriggerHapticFeedback();
            }
        }

        public void PlaySuccessFeedback()
        {
            ShowSuccess("Great job!");
            PlayFeedbackSound(successSound);
            if (enableHapticFeedback)
            {
                TriggerHapticFeedback(0.5f, 0.1f);
            }
        }

        public void PlayErrorFeedback(string message = "Try again")
        {
            ShowError(message);
            PlayFeedbackSound(errorSound);
            if (enableHapticFeedback)
            {
                TriggerHapticFeedback(0.8f, 0.2f);
            }
        }

        private void PlayFeedbackSound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        private void TriggerHapticFeedback(float amplitude = 0.3f, float duration = 0.1f)
        {
            // This would integrate with XR haptic feedback systems
            // For now, we'll use a simple implementation

            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif

            // VR Haptic feedback would go here
            // OVRHaptics, SteamVR haptic feedback, etc.
        }

        #endregion

        #region Accessibility

        public void ToggleHighContrastMode()
        {
            highContrastMode = !highContrastMode;
            UITheme contrastTheme = CreateHighContrastTheme();
            SetTheme(contrastTheme);
            SaveAccessibilitySetting("HighContrast", highContrastMode);
        }

        public void ToggleLargePrintMode()
        {
            largePrintMode = !largePrintMode;
            textScale = largePrintMode ? 1.5f : 1f;
            RefreshTextScaling();
            SaveAccessibilitySetting("LargePrint", largePrintMode);
        }

        public void AdjustTextScale(float scale)
        {
            textScale = Mathf.Clamp(scale, 0.5f, 2.0f);
            RefreshTextScaling();
            SaveAccessibilitySetting("TextScale", textScale);
        }

        public void AdjustVoiceOverSpeed(float speed)
        {
            voiceOverSpeed = Mathf.Clamp(speed, 0.5f, 2.0f);
            SaveAccessibilitySetting("VoiceOverSpeed", voiceOverSpeed);
        }

        private void RefreshTextScaling()
        {
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in texts)
            {
                text.fontSize = text.fontSize * textScale / GetCurrentTextScale(text);
            }
        }

        private float GetCurrentTextScale(TextMeshProUGUI text)
        {
            // This is a simplified implementation
            // In a real system, you'd track the original font size
            return 1f;
        }

        private UITheme CreateHighContrastTheme()
        {
            return new UITheme
            {
                themeName = "HighContrast",
                primaryColor = Color.white,
                secondaryColor = Color.black,
                accentColor = Color.yellow,
                backgroundColor = Color.black,
                textColor = Color.white,
                successColor = Color.green,
                warningColor = Color.yellow,
                errorColor = Color.red
            };
        }

        private void SaveAccessibilitySetting(string setting, bool value)
        {
            PlayerPrefs.SetInt($"Accessibility_{setting}", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SaveAccessibilitySetting(string setting, float value)
        {
            PlayerPrefs.SetFloat($"Accessibility_{setting}", value);
            PlayerPrefs.Save();
        }

        #endregion

        #region Panel Management

        public void ShowPanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                StartCoroutine(FadeInPanel(uiPanels[panelName]));
            }
        }

        public void HidePanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                StartCoroutine(FadeOutPanel(uiPanels[panelName]));
            }
        }

        public void TogglePanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                GameObject panel = uiPanels[panelName];
                if (panel.activeSelf)
                {
                    HidePanel(panelName);
                }
                else
                {
                    ShowPanel(panelName);
                }
            }
        }

        private IEnumerator FadeInPanel(GameObject panel)
        {
            panel.SetActive(true);
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();

            float elapsedTime = 0f;
            float duration = 0.3f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = elapsedTime / duration;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutPanel(GameObject panel)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();

            float elapsedTime = 0f;
            float duration = 0.3f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsedTime / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }

        #endregion

        #region Event Handlers

        private void OnSceneLoadStart(string sceneName)
        {
            ShowLoading($"Loading {sceneName}...");
        }

        private void OnSceneLoadComplete(string sceneName)
        {
            HideLoading();
            ShowSuccess($"Loaded {sceneName}");
        }

        #endregion

        #region Utility Methods

        public void ResetUI()
        {
            // Clear all notifications
            foreach (GameObject notification in activeNotifications)
            {
                Destroy(notification);
            }
            activeNotifications.Clear();
            notificationQueue.Clear();

            // Hide all panels
            foreach (GameObject panel in uiPanels.Values)
            {
                panel.SetActive(false);
            }

            // Reset progress
            HideProgress();

            OnUIReset?.Invoke();
        }

        public UITheme GetCurrentTheme()
        {
            return currentTheme;
        }

        public UITheme[] GetAvailableThemes()
        {
            return availableThemes;
        }

        public Dictionary<string, object> GetUISettings()
        {
            return new Dictionary<string, object>
            {
                ["CurrentTheme"] = currentTheme?.themeName ?? "None",
                ["AnimationsEnabled"] = enableAnimations,
                ["HapticFeedbackEnabled"] = enableHapticFeedback,
                ["VoiceFeedbackEnabled"] = enableVoiceFeedback,
                ["HighContrastMode"] = highContrastMode,
                ["LargePrintMode"] = largePrintMode,
                ["TextScale"] = textScale,
                ["VoiceOverSpeed"] = voiceOverSpeed,
                ["ActiveNotifications"] = activeNotifications.Count,
                ["NotificationQueueSize"] = notificationQueue.Count
            };
        }

        #endregion
    }
}
