using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace VRRehab.SceneManagement
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [System.Serializable]
        public class SceneInfo
        {
            public string sceneName;
            public string displayName;
            public string description;
            public Sprite thumbnail;
            public ExerciseType exerciseType;
            public bool requiresCalibration = false;
            public float estimatedDuration = 300f; // seconds
        }

        public enum ExerciseType
        {
            Throwing,
            BridgeBuilding,
            SquatDodge,
            Menu,
            Tutorial,
            ProfileManagement
        }

        [Header("Scene Configuration")]
        [SerializeField] private SceneInfo[] availableScenes;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string loadingScene = "Loading";

        [Header("Transition Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float loadingDelay = 1.0f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("UI References")]
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private Image fadePanel;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private GameObject loadingPanel;

        [Header("Audio")]
        [SerializeField] private AudioSource transitionAudioSource;
        [SerializeField] private AudioClip transitionSound;
        [SerializeField] private AudioClip sceneLoadSound;

        // Events
        public static event Action<string> OnSceneLoadStart;
        public static event Action<string> OnSceneLoadComplete;
        public static event Action OnTransitionStart;
        public static event Action OnTransitionComplete;

        // State
        private bool isTransitioning = false;
        private AsyncOperation currentLoadingOperation;
        private string currentSceneName;
        private string targetSceneName;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (transitionCanvas != null)
                DontDestroyOnLoad(transitionCanvas.gameObject);

            InitializeTransitionUI();
        }

        private void InitializeTransitionUI()
        {
            if (fadePanel != null)
            {
                Color panelColor = fadePanel.color;
                panelColor.a = 0f;
                fadePanel.color = panelColor;
            }

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        #region Scene Transitions

        public void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("Already transitioning to a scene. Ignoring request.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        public void LoadSceneByType(ExerciseType exerciseType)
        {
            SceneInfo sceneInfo = GetSceneInfoByType(exerciseType);
            if (sceneInfo != null)
            {
                LoadScene(sceneInfo.sceneName);
            }
            else
            {
                Debug.LogError($"No scene found for exercise type: {exerciseType}");
            }
        }

        public void LoadMainMenu()
        {
            LoadScene(mainMenuScene);
        }

        public void ReloadCurrentScene()
        {
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                LoadScene(currentSceneName);
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            if (isTransitioning) yield break;

            isTransitioning = true;
            targetSceneName = sceneName;

            OnSceneLoadStart?.Invoke(sceneName);
            OnTransitionStart?.Invoke();

            // Play transition sound
            PlayTransitionSound();

            // Fade out current scene
            yield return StartCoroutine(FadeOut());

            // Show loading screen
            ShowLoadingScreen();

            // Start loading the new scene
            currentLoadingOperation = SceneManager.LoadSceneAsync(sceneName);
            currentLoadingOperation.allowSceneActivation = false;

            // Update loading progress
            while (!currentLoadingOperation.isDone)
            {
                float progress = Mathf.Clamp01(currentLoadingOperation.progress / 0.9f);
                UpdateLoadingProgress(progress);

                // Wait until loading is complete
                if (currentLoadingOperation.progress >= 0.9f)
                {
                    // Add a small delay for better UX
                    yield return new WaitForSeconds(loadingDelay);
                    break;
                }

                yield return null;
            }

            // Activate the new scene
            currentLoadingOperation.allowSceneActivation = true;

            // Wait for scene to fully load
            while (!currentLoadingOperation.isDone)
            {
                yield return null;
            }

            // Update current scene name
            currentSceneName = sceneName;

            // Hide loading screen
            HideLoadingScreen();

            // Play scene load sound
            PlaySceneLoadSound();

            // Fade in new scene
            yield return StartCoroutine(FadeIn());

            OnSceneLoadComplete?.Invoke(sceneName);
            OnTransitionComplete?.Invoke();

            isTransitioning = false;
        }

        #endregion

        #region Visual Transitions

        private IEnumerator FadeOut()
        {
            if (fadePanel == null) yield break;

            Color startColor = fadePanel.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

            float elapsedTime = 0f;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = fadeCurve.Evaluate(elapsedTime / fadeOutDuration);
                fadePanel.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            fadePanel.color = targetColor;
        }

        private IEnumerator FadeIn()
        {
            if (fadePanel == null) yield break;

            Color startColor = fadePanel.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float elapsedTime = 0f;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = fadeCurve.Evaluate(elapsedTime / fadeInDuration);
                fadePanel.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            fadePanel.color = targetColor;
        }

        #endregion

        #region Loading Screen

        private void ShowLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            UpdateLoadingText("Loading...");
            UpdateLoadingProgress(0f);
        }

        private void HideLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void UpdateLoadingText(string text)
        {
            if (loadingText != null)
            {
                loadingText.text = text;
            }
        }

        private void UpdateLoadingProgress(float progress)
        {
            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }
        }

        #endregion

        #region Audio

        private void PlayTransitionSound()
        {
            if (transitionAudioSource != null && transitionSound != null)
            {
                transitionAudioSource.PlayOneShot(transitionSound);
            }
        }

        private void PlaySceneLoadSound()
        {
            if (transitionAudioSource != null && sceneLoadSound != null)
            {
                transitionAudioSource.PlayOneShot(sceneLoadSound);
            }
        }

        #endregion

        #region Scene Information

        public SceneInfo GetCurrentSceneInfo()
        {
            return GetSceneInfoByName(currentSceneName);
        }

        public SceneInfo GetSceneInfoByName(string sceneName)
        {
            foreach (SceneInfo sceneInfo in availableScenes)
            {
                if (sceneInfo.sceneName == sceneName)
                {
                    return sceneInfo;
                }
            }
            return null;
        }

        public SceneInfo GetSceneInfoByType(ExerciseType exerciseType)
        {
            foreach (SceneInfo sceneInfo in availableScenes)
            {
                if (sceneInfo.exerciseType == exerciseType)
                {
                    return sceneInfo;
                }
            }
            return null;
        }

        public SceneInfo[] GetScenesByType(ExerciseType exerciseType)
        {
            System.Collections.Generic.List<SceneInfo> matchingScenes = new System.Collections.Generic.List<SceneInfo>();

            foreach (SceneInfo sceneInfo in availableScenes)
            {
                if (sceneInfo.exerciseType == exerciseType)
                {
                    matchingScenes.Add(sceneInfo);
                }
            }

            return matchingScenes.ToArray();
        }

        public SceneInfo[] GetAllAvailableScenes()
        {
            return availableScenes;
        }

        #endregion

        #region Utility Methods

        public bool IsTransitioning()
        {
            return isTransitioning;
        }

        public string GetCurrentSceneName()
        {
            return currentSceneName;
        }

        public ExerciseType GetCurrentExerciseType()
        {
            SceneInfo currentScene = GetCurrentSceneInfo();
            return currentScene != null ? currentScene.exerciseType : ExerciseType.Menu;
        }

        public void QuitApplication()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        #region Scene State Management

        public void SaveSceneState()
        {
            // Save current scene state (position, settings, etc.)
            PlayerPrefs.SetString("LastScene", currentSceneName);
            PlayerPrefs.SetString("LastSceneLoadTime", DateTime.Now.ToString());
            PlayerPrefs.Save();
        }

        public void LoadLastScene()
        {
            string lastScene = PlayerPrefs.GetString("LastScene", mainMenuScene);
            if (!string.IsNullOrEmpty(lastScene) && lastScene != currentSceneName)
            {
                LoadScene(lastScene);
            }
        }

        public void SetLoadingMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        #endregion
    }
}
