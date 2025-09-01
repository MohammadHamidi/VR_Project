using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.Tutorial;

namespace VRRehab.UI
{
    public class TutorialOverlay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image visualAidImage;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject highlightRingPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float highlightPulseSpeed = 2f;
        [SerializeField] private Color highlightColor = Color.yellow;

        [Header("Audio")]
        [SerializeField] private AudioSource tutorialAudioSource;
        [SerializeField] private AudioClip stepAdvanceSound;
        [SerializeField] private AudioClip completionSound;

        // Current tutorial state
        private TutorialManager tutorialManager;
        private GameObject currentHighlightObject;
        private Coroutine highlightCoroutine;
        private Coroutine fadeCoroutine;

        // Events
        public static event Action OnTutorialOverlayShown;
        public static event Action OnTutorialOverlayHidden;
        public static event Action<string> OnStepCompleted;

        void Awake()
        {
            InitializeTutorialOverlay();
            SubscribeToEvents();
        }

        private void InitializeTutorialOverlay()
        {
            if (tutorialCanvas != null)
                DontDestroyOnLoad(tutorialCanvas.gameObject);

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);

                // Setup initial panel state
                CanvasGroup canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0f;
            }

            // Find tutorial manager
            tutorialManager = FindObjectOfType<TutorialManager>();
        }

        private void SubscribeToEvents()
        {
            if (tutorialManager != null)
            {
                TutorialManager.OnTutorialStart += OnTutorialStarted;
                TutorialManager.OnTutorialStep += OnTutorialStepChanged;
                TutorialManager.OnTutorialComplete += OnTutorialCompleted;
            }
        }

        #region Tutorial Event Handlers

        private void OnTutorialStarted(string tutorialId)
        {
            ShowTutorialOverlay();
            UpdateUI();
        }

        private void OnTutorialStepChanged(string tutorialId, string stepId)
        {
            UpdateUI();
            PlayStepSound();
        }

        private void OnTutorialCompleted(string tutorialId)
        {
            StartCoroutine(HideTutorialOverlayCoroutine());
            PlayCompletionSound();
        }

        #endregion

        #region UI Management

        public void ShowTutorialOverlay()
        {
            if (tutorialPanel != null && !tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(true);
                StartCoroutine(FadeInPanel());

                OnTutorialOverlayShown?.Invoke();
            }
        }

        public void HideTutorialOverlay()
        {
            StartCoroutine(HideTutorialOverlayCoroutine());
        }

        private IEnumerator HideTutorialOverlayCoroutine()
        {
            yield return StartCoroutine(FadeOutPanel());
            ClearHighlights();

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            OnTutorialOverlayHidden?.Invoke();
        }

        private IEnumerator FadeInPanel()
        {
            CanvasGroup canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
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

        private IEnumerator FadeOutPanel()
        {
            CanvasGroup canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        private void UpdateUI()
        {
            if (tutorialManager == null) return;

            // Get current tutorial and step information
            // This would need to be exposed from TutorialManager
            UpdateTitle("Tutorial Step");
            UpdateDescription("Follow the highlighted objects to complete this step.");
            UpdateProgress(0.5f);
            UpdateButtons(true, true);
        }

        public void UpdateTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void UpdateDescription(string description)
        {
            if (descriptionText != null)
            {
                descriptionText.text = description;
            }
        }

        public void UpdateVisualAid(Sprite sprite)
        {
            if (visualAidImage != null)
            {
                visualAidImage.sprite = sprite;
                visualAidImage.gameObject.SetActive(sprite != null);
            }
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }

        public void UpdateButtons(bool showSkip, bool showNext)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(showSkip);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNext);
            }
        }

        #endregion

        #region Highlighting System

        public void HighlightObject(GameObject targetObject)
        {
            if (targetObject == null) return;

            ClearHighlights();

            // Add highlight material or effect
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create a highlight effect
                GameObject highlightObj = Instantiate(highlightRingPrefab, targetObject.transform.position, Quaternion.identity);
                highlightObj.transform.SetParent(targetObject.transform);

                // Scale to object size
                Vector3 objectSize = renderer.bounds.size;
                highlightObj.transform.localScale = objectSize * 1.2f;

                // Start pulsing animation
                currentHighlightObject = highlightObj;
                highlightCoroutine = StartCoroutine(PulseHighlight(highlightObj));
            }
        }

        public void HighlightPosition(Vector3 position, float radius = 1f)
        {
            if (highlightRingPrefab == null) return;

            ClearHighlights();

            GameObject highlightObj = Instantiate(highlightRingPrefab, position, Quaternion.identity);
            highlightObj.transform.localScale = Vector3.one * radius * 2f;

            // Set highlight color
            Renderer renderer = highlightObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = highlightColor;
            }

            currentHighlightObject = highlightObj;
            highlightCoroutine = StartCoroutine(PulseHighlight(highlightObj));
        }

        private IEnumerator PulseHighlight(GameObject highlightObj)
        {
            if (highlightObj == null) yield break;

            Renderer renderer = highlightObj.GetComponent<Renderer>();
            if (renderer == null) yield break;

            Color baseColor = highlightColor;
            float time = 0f;

            while (highlightObj != null && highlightObj.activeInHierarchy)
            {
                time += Time.deltaTime * highlightPulseSpeed;
                float intensity = (Mathf.Sin(time) + 1f) / 2f; // 0 to 1

                Color pulseColor = Color.Lerp(Color.clear, baseColor, intensity);
                renderer.material.SetColor("_EmissionColor", pulseColor);

                yield return null;
            }
        }

        public void ClearHighlights()
        {
            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }

            if (currentHighlightObject != null)
            {
                Destroy(currentHighlightObject);
                currentHighlightObject = null;
            }
        }

        #endregion

        #region Audio

        private void PlayStepSound()
        {
            if (tutorialAudioSource != null && stepAdvanceSound != null)
            {
                tutorialAudioSource.PlayOneShot(stepAdvanceSound);
            }
        }

        private void PlayCompletionSound()
        {
            if (tutorialAudioSource != null && completionSound != null)
            {
                tutorialAudioSource.PlayOneShot(completionSound);
            }
        }

        #endregion

        #region Button Handlers

        public void OnSkipButtonPressed()
        {
            if (tutorialManager != null)
            {
                tutorialManager.SkipTutorial();
            }
        }

        public void OnNextButtonPressed()
        {
            if (tutorialManager != null)
            {
                tutorialManager.AdvanceToNextStep();
            }
        }

        #endregion

        #region Public Methods

        public bool IsTutorialActive()
        {
            return tutorialPanel != null && tutorialPanel.activeSelf;
        }

        public void SetTutorialManager(TutorialManager manager)
        {
            tutorialManager = manager;
        }

        public void ForceHide()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            ClearHighlights();
        }

        #endregion

        void OnDestroy()
        {
            ClearHighlights();

            if (tutorialManager != null)
            {
                TutorialManager.OnTutorialStart -= OnTutorialStarted;
                TutorialManager.OnTutorialStep -= OnTutorialStepChanged;
                TutorialManager.OnTutorialComplete -= OnTutorialCompleted;
            }
        }
    }
}
