using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRRehab.SceneManagement;
using VRRehab.UI;
using VRRehab.DataPersistence;

namespace VRRehab.SceneBuilders
{
    public class MainMenuBuilder : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private SceneTransitionManager sceneTransitionManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private DataPersistenceManager dataManager;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject mainCanvasPrefab;
        [SerializeField] private GameObject vrCameraRigPrefab;
        [SerializeField] private GameObject uiRaycasterPrefab;

        [Header("Background")]
        [SerializeField] private Material backgroundMaterial;
        [SerializeField] private Texture2D backgroundTexture;

        void Start()
        {
            StartCoroutine(BuildMainMenuScene());
        }

        private IEnumerator BuildMainMenuScene()
        {
            // Create essential scene objects
            yield return CreateEssentialObjects();

            // Create UI Canvas
            yield return CreateMainCanvas();

            // Create main menu UI elements
            yield return CreateMainMenuUI();

            // Setup navigation
            SetupNavigation();

            // Initialize scene systems
            InitializeSystems();

            Debug.Log("Main Menu scene built successfully");
        }

        private IEnumerator CreateEssentialObjects()
        {
            // Create VR Camera Rig if not present
            if (GameObject.Find("XR Origin") == null && vrCameraRigPrefab != null)
            {
                Instantiate(vrCameraRigPrefab);
            }
            else if (GameObject.Find("Main Camera") == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
                cameraObj.AddComponent<AudioListener>();
            }

            // Create Event System for UI
            if (GameObject.Find("EventSystem") == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create directional light
            if (GameObject.Find("Directional Light") == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            yield return null;
        }

        private IEnumerator CreateMainCanvas()
        {
            GameObject canvasObj = null;

            if (mainCanvasPrefab != null)
            {
                canvasObj = Instantiate(mainCanvasPrefab);
            }
            else
            {
                canvasObj = new GameObject("MainCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.15f, 0.2f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            yield return null;
        }

        private IEnumerator CreateMainMenuUI()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) yield break;

            // Create title
            CreateTitle(canvas);

            // Create menu buttons
            CreateMenuButtons(canvas);

            // Create status panel
            CreateStatusPanel(canvas);

            yield return null;
        }

        private void CreateTitle(GameObject canvas)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvas.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "VR Rehabilitation Center";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(600, 80);
            titleRect.anchoredPosition = Vector2.zero;

            // Create subtitle
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(canvas.transform, false);

            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "Interactive Therapeutic Exercises";
            subtitleText.fontSize = 24;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.8f, 0.8f, 0.8f);

            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.75f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.8f);
            subtitleRect.sizeDelta = new Vector2(500, 40);
            subtitleRect.anchoredPosition = Vector2.zero;
        }

        private void CreateMenuButtons(GameObject canvas)
        {
            string[] buttonLabels = { "Start Exercises", "Patient Profile", "Analytics", "Settings", "Exit" };
            string[] buttonScenes = { "ThrowBall", "ProfileManagement", "Analytics", "Settings", "" };

            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.4f);
            containerRect.anchorMax = new Vector2(0.5f, 0.6f);
            containerRect.sizeDelta = new Vector2(400, 300);
            containerRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = buttonContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            ContentSizeFitter fitter = buttonContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < buttonLabels.Length; i++)
            {
                CreateMenuButton(buttonContainer, buttonLabels[i], buttonScenes[i]);
            }
        }

        private void CreateMenuButton(GameObject parent, string label, string targetScene)
        {
            GameObject buttonObj = new GameObject($"Button_{label.Replace(" ", "")}");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.4f, 0.5f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.5f, 0.6f);
            colors.pressedColor = new Color(0.2f, 0.3f, 0.4f);
            button.colors = colors;

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 24;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Setup button click handler
            if (!string.IsNullOrEmpty(targetScene))
            {
                button.onClick.AddListener(() => OnMenuButtonClicked(targetScene));
            }
            else if (label == "Exit")
            {
                button.onClick.AddListener(() => OnExitButtonClicked());
            }

            // Size the button
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(300, 60);
        }

        private void CreateStatusPanel(GameObject canvas)
        {
            GameObject statusPanel = new GameObject("StatusPanel");
            statusPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = statusPanel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

            RectTransform panelRect = statusPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.3f, 0.15f);
            panelRect.sizeDelta = Vector2.zero;

            // Create status text
            GameObject statusTextObj = new GameObject("StatusText");
            statusTextObj.transform.SetParent(statusPanel.transform, false);

            TextMeshProUGUI statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "Ready for rehabilitation session";
            statusText.fontSize = 16;
            statusText.color = Color.white;

            RectTransform statusRect = statusTextObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.1f);
            statusRect.anchorMax = new Vector2(0.9f, 0.9f);
            statusRect.sizeDelta = Vector2.zero;
        }

        private void SetupNavigation()
        {
            if (sceneTransitionManager == null)
            {
                sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
            }

            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (dataManager == null)
            {
                dataManager = FindObjectOfType<DataPersistenceManager>();
            }
        }

        private void InitializeSystems()
        {
            // Initialize any required systems
            if (uiManager != null)
            {
                uiManager.ShowSuccess("VR Rehabilitation System Ready");
            }
        }

        private void OnMenuButtonClicked(string targetScene)
        {
            if (sceneTransitionManager != null)
            {
                if (uiManager != null)
                {
                    uiManager.PlayButtonClick();
                }

                sceneTransitionManager.LoadScene(targetScene);
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }
        }

        private void OnExitButtonClicked()
        {
            if (uiManager != null)
            {
                uiManager.PlayButtonClick();
            }

            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // Public method to rebuild the menu (useful for dynamic updates)
        public void RebuildMenu()
        {
            StartCoroutine(BuildMainMenuScene());
        }
    }
}
