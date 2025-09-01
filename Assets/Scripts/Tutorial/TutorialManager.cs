using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.SceneManagement;

namespace VRRehab.Tutorial
{
    [System.Serializable]
    public class TutorialStep
    {
        public string stepId;
        public string title;
        public string description;
        public string voiceOverAudio;
        public Sprite visualAid;
        public GameObject highlightObject;
        public Vector3 highlightPosition;
        public float highlightRadius = 1f;
        public TutorialAction requiredAction;
        public float stepDuration = 0f; // 0 = wait for action, >0 = auto-advance
        public bool canSkip = true;
        public string nextStepId;
    }

    [System.Serializable]
    public class TutorialAction
    {
        public enum ActionType
        {
            None,
            GrabObject,
            ThrowObject,
            MoveToPosition,
            Squat,
            WaitForTime,
            ButtonPress,
            VoiceCommand
        }

        public ActionType actionType;
        public GameObject targetObject;
        public Transform targetPosition;
        public string buttonName;
        public float requiredDuration = 0f;
        public float tolerance = 0.5f;
    }

    [System.Serializable]
    public class TutorialSequence
    {
        public string tutorialId;
        public string tutorialName;
        public VRRehab.SceneManagement.SceneTransitionManager.ExerciseType exerciseType;
        public TutorialStep[] steps;
        public bool isCompleted = false;
        public bool isRequired = true;
        public int currentStepIndex = 0;
    }

    public class TutorialManager : MonoBehaviour
    {
        [Header("Tutorial Configuration")]
        [SerializeField] private TutorialSequence[] tutorialSequences;
        [SerializeField] private bool enableVoiceGuidance = true;
        [SerializeField] private bool enableVisualCues = true;
        [SerializeField] private bool enableHapticFeedback = true;

        [Header("UI References")]
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image visualAidImage;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject tutorialPanel;

        [Header("Highlighting")]
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private float highlightPulseSpeed = 2f;
        [SerializeField] private GameObject highlightRingPrefab;

        [Header("Audio")]
        [SerializeField] private AudioSource tutorialAudioSource;
        [SerializeField] private AudioClip[] voiceOverClips;
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private AudioClip stepAdvanceSound;

        [Header("Timing")]
        [SerializeField] private float autoAdvanceDelay = 2f;
        [SerializeField] private float stepTransitionDuration = 0.5f;

        // Events
        public static event Action<string> OnTutorialStart;
        public static event Action<string, string> OnTutorialStep;
        public static event Action<string> OnTutorialComplete;
        public static event Action OnTutorialSkip;

        // State
        private TutorialSequence currentTutorial;
        private TutorialStep currentStep;
        private Dictionary<string, AudioClip> voiceOverDictionary;
        private GameObject currentHighlightObject;
        private Coroutine highlightCoroutine;
        private Coroutine autoAdvanceCoroutine;
        private bool isTutorialActive = false;
        private bool waitingForAction = false;

        void Awake()
        {
            InitializeVoiceOverDictionary();
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeVoiceOverDictionary()
        {
            voiceOverDictionary = new Dictionary<string, AudioClip>();
            foreach (AudioClip clip in voiceOverClips)
            {
                voiceOverDictionary[clip.name] = clip;
            }
        }

        private void InitializeUI()
        {
            if (tutorialCanvas != null)
                DontDestroyOnLoad(tutorialCanvas.gameObject);

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);

            if (nextButton != null)
                nextButton.onClick.AddListener(AdvanceToNextStep);
        }

        private void SubscribeToEvents()
        {
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadComplete += OnSceneLoaded;
        }

        #region Tutorial Control

        public void StartTutorial(string tutorialId)
        {
            TutorialSequence tutorial = GetTutorialById(tutorialId);
            if (tutorial == null)
            {
                Debug.LogError($"Tutorial not found: {tutorialId}");
                return;
            }

            if (tutorial.isCompleted && !tutorial.isRequired)
            {
                Debug.Log($"Tutorial {tutorialId} already completed, skipping.");
                return;
            }

            currentTutorial = tutorial;
            currentTutorial.currentStepIndex = 0;

            isTutorialActive = true;
            OnTutorialStart?.Invoke(tutorialId);

            ShowTutorialPanel();
            StartStep(0);
        }

        public void StartTutorialForExercise(VRRehab.SceneManagement.SceneTransitionManager.ExerciseType exerciseType)
        {
            TutorialSequence tutorial = GetTutorialByExerciseType(exerciseType);
            if (tutorial != null)
            {
                StartTutorial(tutorial.tutorialId);
            }
            else
            {
                Debug.LogWarning($"No tutorial found for exercise type: {exerciseType}");
            }
        }

        public void SkipTutorial()
        {
            if (currentStep != null && !currentStep.canSkip)
            {
                Debug.Log("Cannot skip required tutorial step");
                return;
            }

            CompleteTutorial(true);
            OnTutorialSkip?.Invoke();
        }

        public void CompleteTutorial(bool skipped = false)
        {
            if (currentTutorial == null) return;

            if (!skipped)
            {
                currentTutorial.isCompleted = true;
                PlayCompletionSound();
            }

            isTutorialActive = false;
            HideTutorialPanel();
            ClearHighlights();

            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            OnTutorialComplete?.Invoke(currentTutorial.tutorialId);

            currentTutorial = null;
            currentStep = null;
        }

        #endregion

        #region Step Management

        private void StartStep(int stepIndex)
        {
            if (currentTutorial == null || stepIndex >= currentTutorial.steps.Length)
            {
                CompleteTutorial();
                return;
            }

            currentStep = currentTutorial.steps[stepIndex];
            currentTutorial.currentStepIndex = stepIndex;

            UpdateUI();
            PlayVoiceOver();
            SetupHighlighting();
            SetupActionDetection();

            UpdateProgressBar();

            OnTutorialStep?.Invoke(currentTutorial.tutorialId, currentStep.stepId);

            // Handle auto-advance
            if (currentStep.stepDuration > 0)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceStep(currentStep.stepDuration));
            }
        }

        public void AdvanceToNextStep()
        {
            if (currentTutorial == null || currentStep == null) return;

            PlayStepAdvanceSound();

            ClearHighlights();

            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            int nextStepIndex = currentTutorial.currentStepIndex + 1;

            if (!string.IsNullOrEmpty(currentStep.nextStepId))
            {
                // Find step by ID
                nextStepIndex = GetStepIndexById(currentStep.nextStepId);
            }

            StartStep(nextStepIndex);
        }

        private int GetStepIndexById(string stepId)
        {
            for (int i = 0; i < currentTutorial.steps.Length; i++)
            {
                if (currentTutorial.steps[i].stepId == stepId)
                {
                    return i;
                }
            }
            return currentTutorial.currentStepIndex + 1;
        }

        private IEnumerator AutoAdvanceStep(float delay)
        {
            yield return new WaitForSeconds(delay + autoAdvanceDelay);
            AdvanceToNextStep();
        }

        #endregion

        #region UI Management

        private void ShowTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                StartCoroutine(FadeInPanel());
            }
        }

        private void HideTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                StartCoroutine(FadeOutPanel());
            }
        }

        private IEnumerator FadeInPanel()
        {
            CanvasGroup canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();

            float elapsedTime = 0f;
            while (elapsedTime < stepTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / stepTransitionDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutPanel()
        {
            CanvasGroup canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();

            float elapsedTime = 0f;
            while (elapsedTime < stepTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / stepTransitionDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            tutorialPanel.SetActive(false);
        }

        private void UpdateUI()
        {
            if (currentStep == null) return;

            if (titleText != null)
                titleText.text = currentStep.title;

            if (descriptionText != null)
                descriptionText.text = currentStep.description;

            if (visualAidImage != null)
            {
                visualAidImage.sprite = currentStep.visualAid;
                visualAidImage.gameObject.SetActive(currentStep.visualAid != null);
            }

            if (skipButton != null)
                skipButton.gameObject.SetActive(currentStep.canSkip);

            if (nextButton != null)
                nextButton.gameObject.SetActive(currentStep.stepDuration <= 0);
        }

        private void UpdateProgressBar()
        {
            if (progressBar != null && currentTutorial != null)
            {
                float progress = (float)(currentTutorial.currentStepIndex + 1) / currentTutorial.steps.Length;
                progressBar.value = progress;
            }
        }

        #endregion

        #region Highlighting System

        private void SetupHighlighting()
        {
            ClearHighlights();

            if (!enableVisualCues || currentStep == null) return;

            if (currentStep.highlightObject != null)
            {
                HighlightObject(currentStep.highlightObject);
            }
            else if (currentStep.highlightPosition != Vector3.zero)
            {
                HighlightPosition(currentStep.highlightPosition, currentStep.highlightRadius);
            }
        }

        private void HighlightObject(GameObject targetObject)
        {
            if (targetObject == null) return;

            // Add highlight material
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                Material[] newMaterials = new Material[materials.Length + 1];
                materials.CopyTo(newMaterials, 0);
                newMaterials[newMaterials.Length - 1] = highlightMaterial;
                renderer.materials = newMaterials;

                currentHighlightObject = targetObject;
                highlightCoroutine = StartCoroutine(PulseHighlight(renderer));
            }
        }

        private void HighlightPosition(Vector3 position, float radius)
        {
            if (highlightRingPrefab != null)
            {
                GameObject highlightRing = Instantiate(highlightRingPrefab, position, Quaternion.identity);
                highlightRing.transform.localScale = Vector3.one * radius * 2f;

                Renderer renderer = highlightRing.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = highlightColor;
                    highlightCoroutine = StartCoroutine(PulseHighlight(renderer));
                }

                currentHighlightObject = highlightRing;
            }
        }

        private IEnumerator PulseHighlight(Renderer renderer)
        {
            if (renderer == null) yield break;

            Color baseColor = highlightColor;
            float time = 0f;

            while (currentHighlightObject != null && currentHighlightObject.activeInHierarchy)
            {
                time += Time.deltaTime * highlightPulseSpeed;
                float intensity = (Mathf.Sin(time) + 1f) / 2f; // 0 to 1

                Color pulseColor = Color.Lerp(Color.clear, baseColor, intensity);
                renderer.material.SetColor("_EmissionColor", pulseColor);

                yield return null;
            }
        }

        private void ClearHighlights()
        {
            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }

            if (currentHighlightObject != null)
            {
                if (currentHighlightObject != currentStep?.highlightObject)
                {
                    Destroy(currentHighlightObject);
                }
                else
                {
                    // Remove highlight material from original object
                    Renderer renderer = currentHighlightObject.GetComponent<Renderer>();
                    if (renderer != null && renderer.materials.Length > 1)
                    {
                        Material[] materials = renderer.materials;
                        Material[] newMaterials = new Material[materials.Length - 1];
                        for (int i = 0; i < newMaterials.Length; i++)
                        {
                            newMaterials[i] = materials[i];
                        }
                        renderer.materials = newMaterials;
                    }
                }

                currentHighlightObject = null;
            }
        }

        #endregion

        #region Action Detection

        private void SetupActionDetection()
        {
            if (currentStep == null || currentStep.requiredAction.actionType == TutorialAction.ActionType.None)
                return;

            waitingForAction = true;

            switch (currentStep.requiredAction.actionType)
            {
                case TutorialAction.ActionType.GrabObject:
                    StartCoroutine(WaitForGrabAction());
                    break;
                case TutorialAction.ActionType.ThrowObject:
                    StartCoroutine(WaitForThrowAction());
                    break;
                case TutorialAction.ActionType.MoveToPosition:
                    StartCoroutine(WaitForMovementAction());
                    break;
                case TutorialAction.ActionType.Squat:
                    StartCoroutine(WaitForSquatAction());
                    break;
                case TutorialAction.ActionType.WaitForTime:
                    StartCoroutine(WaitForTimeAction());
                    break;
                case TutorialAction.ActionType.ButtonPress:
                    StartCoroutine(WaitForButtonAction());
                    break;
            }
        }

        private IEnumerator WaitForGrabAction()
        {
            GameObject targetObject = currentStep.requiredAction.targetObject;
            if (targetObject == null) yield break;

            // Wait for grab interaction (you'll need to integrate with XR Interaction Toolkit)
            while (waitingForAction)
            {
                // Check if object is grabbed
                // This would need integration with your XR grab system
                yield return null;
            }
        }

        private IEnumerator WaitForThrowAction()
        {
            GameObject targetObject = currentStep.requiredAction.targetObject;
            if (targetObject == null) yield break;

            // Wait for throw action
            while (waitingForAction)
            {
                // Check if object is thrown (velocity threshold)
                Rigidbody rb = targetObject.GetComponent<Rigidbody>();
                if (rb != null && rb.velocity.magnitude > 2f)
                {
                    waitingForAction = false;
                    yield return new WaitForSeconds(0.5f);
                    AdvanceToNextStep();
                }
                yield return null;
            }
        }

        private IEnumerator WaitForMovementAction()
        {
            Transform targetPosition = currentStep.requiredAction.targetPosition;
            if (targetPosition == null) yield break;

            float tolerance = currentStep.requiredAction.tolerance;

            while (waitingForAction)
            {
                // Check if player/camera is within range of target position
                if (Vector3.Distance(Camera.main.transform.position, targetPosition.position) < tolerance)
                {
                    waitingForAction = false;
                    yield return new WaitForSeconds(1f);
                    AdvanceToNextStep();
                }
                yield return null;
            }
        }

        private IEnumerator WaitForSquatAction()
        {
            while (waitingForAction)
            {
                // Check for squat detection (integrate with your SquatDodge system)
                // This would need access to the squat detection logic
                yield return null;
            }
        }

        private IEnumerator WaitForTimeAction()
        {
            float duration = currentStep.requiredAction.requiredDuration;
            yield return new WaitForSeconds(duration);
            AdvanceToNextStep();
        }

        private IEnumerator WaitForButtonAction()
        {
            string buttonName = currentStep.requiredAction.buttonName;

            while (waitingForAction)
            {
                if (Input.GetButtonDown(buttonName))
                {
                    waitingForAction = false;
                    AdvanceToNextStep();
                }
                yield return null;
            }
        }

        #endregion

        #region Audio

        private void PlayVoiceOver()
        {
            if (!enableVoiceGuidance || currentStep == null ||
                string.IsNullOrEmpty(currentStep.voiceOverAudio)) return;

            if (voiceOverDictionary.TryGetValue(currentStep.voiceOverAudio, out AudioClip clip))
            {
                tutorialAudioSource.PlayOneShot(clip);
            }
        }

        private void PlayCompletionSound()
        {
            if (tutorialAudioSource != null && completionSound != null)
            {
                tutorialAudioSource.PlayOneShot(completionSound);
            }
        }

        private void PlayStepAdvanceSound()
        {
            if (tutorialAudioSource != null && stepAdvanceSound != null)
            {
                tutorialAudioSource.PlayOneShot(stepAdvanceSound);
            }
        }

        #endregion

        #region Utility Methods

        private TutorialSequence GetTutorialById(string tutorialId)
        {
            foreach (TutorialSequence tutorial in tutorialSequences)
            {
                if (tutorial.tutorialId == tutorialId)
                {
                    return tutorial;
                }
            }
            return null;
        }

        private TutorialSequence GetTutorialByExerciseType(VRRehab.SceneManagement.SceneTransitionManager.ExerciseType exerciseType)
        {
            foreach (TutorialSequence tutorial in tutorialSequences)
            {
                if (tutorial.exerciseType == exerciseType)
                {
                    return tutorial;
                }
            }
            return null;
        }

        public bool IsTutorialActive()
        {
            return isTutorialActive;
        }

        public bool IsTutorialCompleted(string tutorialId)
        {
            TutorialSequence tutorial = GetTutorialById(tutorialId);
            return tutorial != null && tutorial.isCompleted;
        }

        public void ResetTutorial(string tutorialId)
        {
            TutorialSequence tutorial = GetTutorialById(tutorialId);
            if (tutorial != null)
            {
                tutorial.isCompleted = false;
                tutorial.currentStepIndex = 0;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSceneLoaded(string sceneName)
        {
            // Auto-start tutorial for the loaded exercise if needed
            VRRehab.SceneManagement.SceneTransitionManager.SceneInfo sceneInfo = FindObjectOfType<VRRehab.SceneManagement.SceneTransitionManager>()?.GetCurrentSceneInfo();
            if (sceneInfo != null)
            {
                TutorialSequence tutorial = GetTutorialByExerciseType(sceneInfo.exerciseType);
                if (tutorial != null && !tutorial.isCompleted && tutorial.isRequired)
                {
                    StartTutorial(tutorial.tutorialId);
                }
            }
        }

        #endregion

        void OnDestroy()
        {
            VRRehab.SceneManagement.SceneTransitionManager.OnSceneLoadComplete -= OnSceneLoaded;
            ClearHighlights();
        }
    }
}
