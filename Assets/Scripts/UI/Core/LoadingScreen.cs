using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.SceneManagement;

namespace VRRehab.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject spinner;
        [SerializeField] private TextMeshProUGUI tipText;

        [Header("Visual Settings")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.95f);
        [SerializeField] private Color progressColor = new Color(0.2f, 0.6f, 0.8f);
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private float spinnerSpeed = 360f; // degrees per second

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float textChangeInterval = 2f;

        [Header("Loading Messages")]
        [SerializeField] private string[] loadingMessages = {
            "Loading rehabilitation exercises...",
            "Preparing your personalized therapy...",
            "Calibrating motion tracking...",
            "Setting up virtual environment...",
            "Loading patient profile...",
            "Initializing therapy session..."
        };

        [Header("Tips")]
        [SerializeField] private string[] loadingTips = {
            "Take deep breaths and relax during your session",
            "Focus on proper form for maximum benefit",
            "Stay hydrated throughout your therapy",
            "Consistency is key to recovery progress",
            "Listen to your body and communicate with your therapist"
        };

        // Current state
        private bool isLoading = false;
        private float currentProgress = 0f;
        private Coroutine spinnerCoroutine;
        private Coroutine textCycleCoroutine;
        private CanvasGroup canvasGroup;

        // Events
        public static event Action OnLoadingStarted;
        public static event Action OnLoadingComplete;

        void Awake()
        {
            InitializeLoadingScreen();
        }

        private void InitializeLoadingScreen()
        {
            if (loadingCanvas != null)
                DontDestroyOnLoad(loadingCanvas.gameObject);

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
                canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0f;
            }

            // Setup visual elements
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
                if (backgroundSprite != null)
                {
                    backgroundImage.sprite = backgroundSprite;
                }
            }

            if (progressFill != null)
            {
                progressFill.color = progressColor;
            }

            // Subscribe to scene transition events
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        #region Public Methods

        public void ShowLoading(string message = null, float initialProgress = 0f)
        {
            if (isLoading) return;

            isLoading = true;
            currentProgress = initialProgress;

            if (loadingPanel != null && !loadingPanel.activeSelf)
            {
                loadingPanel.SetActive(true);
                StartCoroutine(FadeIn());
            }

            UpdateLoadingText(message ?? GetRandomLoadingMessage());
            UpdateProgress(currentProgress);
            StartSpinner();
            StartTextCycling();

            OnLoadingStarted?.Invoke();
        }

        public void HideLoading()
        {
            if (!isLoading) return;

            isLoading = false;
            StopSpinner();
            StopTextCycling();

            StartCoroutine(FadeOut());

            OnLoadingComplete?.Invoke();
        }

        public void UpdateProgress(float progress)
        {
            currentProgress = Mathf.Clamp01(progress);

            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"{GetRandomLoadingMessage()} ({(currentProgress * 100):F0}%)";
            }
        }

        public void SetLoadingMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        public void SetTip(string tip)
        {
            if (tipText != null)
            {
                tipText.text = $"💡 Tip: {tip}";
            }
        }

        public bool IsLoading()
        {
            return isLoading;
        }

        public float GetCurrentProgress()
        {
            return currentProgress;
        }

        #endregion

        #region Animation Methods

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            loadingPanel.SetActive(false);
        }

        private void StartSpinner()
        {
            if (spinner != null && spinnerCoroutine == null)
            {
                spinnerCoroutine = StartCoroutine(SpinAnimation());
            }
        }

        private void StopSpinner()
        {
            if (spinnerCoroutine != null)
            {
                StopCoroutine(spinnerCoroutine);
                spinnerCoroutine = null;
            }
        }

        private IEnumerator SpinAnimation()
        {
            while (spinner != null && spinner.activeInHierarchy)
            {
                spinner.transform.Rotate(Vector3.forward, spinnerSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private void StartTextCycling()
        {
            if (textCycleCoroutine == null)
            {
                textCycleCoroutine = StartCoroutine(CycleLoadingText());
            }
        }

        private void StopTextCycling()
        {
            if (textCycleCoroutine != null)
            {
                StopCoroutine(textCycleCoroutine);
                textCycleCoroutine = null;
            }
        }

        private IEnumerator CycleLoadingText()
        {
            while (isLoading)
            {
                yield return new WaitForSeconds(textChangeInterval);

                if (loadingText != null && !string.IsNullOrEmpty(loadingText.text))
                {
                    SetLoadingMessage(GetRandomLoadingMessage());
                }

                if (tipText != null)
                {
                    SetTip(GetRandomTip());
                }
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateLoadingText(string text)
        {
            if (loadingText != null)
            {
                loadingText.text = text;
            }
        }

        private string GetRandomLoadingMessage()
        {
            if (loadingMessages.Length == 0) return "Loading...";

            int randomIndex = UnityEngine.Random.Range(0, loadingMessages.Length);
            return loadingMessages[randomIndex];
        }

        private string GetRandomTip()
        {
            if (loadingTips.Length == 0) return "Stay focused on your recovery goals";

            int randomIndex = UnityEngine.Random.Range(0, loadingTips.Length);
            return loadingTips[randomIndex];
        }

        #endregion

        #region Event Handlers

        private void OnSceneLoadStart(string sceneName)
        {
            ShowLoading($"Loading {sceneName}...");
        }

        private void OnSceneLoadComplete(string sceneName)
        {
            // Add a small delay to show completion
            StartCoroutine(DelayedHide());
        }

        private IEnumerator DelayedHide()
        {
            UpdateProgress(1f);
            SetLoadingMessage("Complete!");
            yield return new WaitForSeconds(0.5f);
            HideLoading();
        }

        #endregion

        #region Scene Management Integration

        public void LoadSceneWithLoading(string sceneName, string loadingMessage = null)
        {
            ShowLoading(loadingMessage ?? $"Loading {sceneName}...");

            var sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
            if (sceneTransitionManager != null)
            {
                sceneTransitionManager.LoadScene(sceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        public void SimulateLoading(float duration, string message = null)
        {
            StartCoroutine(SimulateLoadingProcess(duration, message));
        }

        private IEnumerator SimulateLoadingProcess(float duration, string message)
        {
            ShowLoading(message ?? "Processing...");

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                UpdateProgress(progress);
                yield return null;
            }

            UpdateProgress(1f);
            yield return new WaitForSeconds(0.5f);
            HideLoading();
        }

        #endregion

        #region Advanced Features

        public void ShowProgressWithSteps(string[] steps, float stepDuration = 1f)
        {
            StartCoroutine(ShowProgressSteps(steps, stepDuration));
        }

        private IEnumerator ShowProgressSteps(string[] steps, float stepDuration)
        {
            ShowLoading();

            float progressPerStep = 1f / steps.Length;

            for (int i = 0; i < steps.Length; i++)
            {
                SetLoadingMessage(steps[i]);
                UpdateProgress(progressPerStep * (i + 1));

                yield return new WaitForSeconds(stepDuration);
            }

            yield return new WaitForSeconds(0.5f);
            HideLoading();
        }

        public void ShowLoadingWithCallback(Action onComplete, float duration = 2f)
        {
            StartCoroutine(LoadingWithCallback(onComplete, duration));
        }

        private IEnumerator LoadingWithCallback(Action onComplete, float duration)
        {
            ShowLoading();
            yield return new WaitForSeconds(duration);
            HideLoading();

            onComplete?.Invoke();
        }

        #endregion

        void OnDestroy()
        {
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadStart -= OnSceneLoadStart;
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;

            StopSpinner();
            StopTextCycling();
        }
    }
}
