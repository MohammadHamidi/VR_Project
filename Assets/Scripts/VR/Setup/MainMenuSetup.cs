using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.UI;
using VRRehab.DataPersistence;

namespace VRRehab.SceneSetup
{
    public class MainMenuSetup : MonoBehaviour
    {
        [Header("UI Prefabs")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;

        [Header("Menu UI Elements")]
        [SerializeField] private GameObject patientSelectionPanel;
        [SerializeField] private GameObject exerciseSelectionPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button newPatientButton;
        [SerializeField] private Button loadPatientButton;
        [SerializeField] private Button throwingExerciseButton;
        [SerializeField] private Button bridgeExerciseButton;
        [SerializeField] private Button squatExerciseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button backButton;

        [Header("Input Fields")]
        [SerializeField] private TMP_InputField patientNameInput;
        [SerializeField] private TMP_Text welcomeText;

        private DataPersistenceManager dataManager;
        private UIManager uiManager;

        private void Awake()
        {
            StartCoroutine(SetupMainMenu());
        }

        private IEnumerator SetupMainMenu()
        {
            yield return new WaitForEndOfFrame();

            // Setup VR Environment
            SetupVREnvironment();

            // Setup Core Systems
            SetupCoreSystems();

            // Setup UI
            SetupUI();

            // Initialize Menu
            InitializeMenu();

            Debug.Log("Main menu setup completed!");
        }

        private void SetupVREnvironment()
        {
            if (GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                if (xrTemplatePrefab != null)
                {
                    Instantiate(xrTemplatePrefab);
                }
            }
        }

        private void SetupCoreSystems()
        {
            // Find or create data manager
            dataManager = FindObjectOfType<DataPersistenceManager>();
            if (dataManager == null)
            {
                GameObject managerObj = new GameObject("DataPersistenceManager");
                dataManager = managerObj.AddComponent<DataPersistenceManager>();
            }

            // Find or create UI manager
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                GameObject uiObj = new GameObject("UIManager");
                uiManager = uiObj.AddComponent<UIManager>();
            }
        }

        private void SetupUI()
        {
            // Setup Canvas
            if (GameObject.FindObjectOfType<Canvas>() == null)
            {
                if (canvasPrefab != null)
                {
                    GameObject canvas = Instantiate(canvasPrefab);
                    SetupMenuUI(canvas);
                }
            }
        }

        private void SetupMenuUI(GameObject canvas)
        {
            // Create main menu panel
            GameObject mainPanel = CreateMainPanel(canvas.transform);

            // Create patient selection panel
            CreatePatientSelectionPanel(mainPanel.transform);

            // Create exercise selection panel
            CreateExerciseSelectionPanel(mainPanel.transform);

            // Create settings panel
            CreateSettingsPanel(mainPanel.transform);
        }

        private GameObject CreateMainPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);

            // Add RectTransform
            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(600, 400);
            rectTransform.anchoredPosition = Vector2.zero;

            // Add Image component for background
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            return panel;
        }

        private void CreatePatientSelectionPanel(Transform parent)
        {
            GameObject panel = new GameObject("PatientSelectionPanel");
            panel.transform.SetParent(parent, false);

            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Title
            CreateText(panel.transform, "Select Patient", new Vector2(0, 150), new Vector2(400, 50), 24, TextAlignmentOptions.Center);

            // Welcome text
            welcomeText = CreateText(panel.transform, "Welcome to VR Rehab!", new Vector2(0, 100), new Vector2(400, 30), 16, TextAlignmentOptions.Center).GetComponent<TMP_Text>();

            // New Patient Button
            newPatientButton = CreateButton(panel.transform, "New Patient", new Vector2(0, 20), new Vector2(200, 50));
            newPatientButton.onClick.AddListener(ShowNewPatientDialog);

            // Load Patient Button
            loadPatientButton = CreateButton(panel.transform, "Load Patient", new Vector2(0, -40), new Vector2(200, 50));
            loadPatientButton.onClick.AddListener(ShowLoadPatientDialog);

            // Settings Button
            settingsButton = CreateButton(panel.transform, "Settings", new Vector2(0, -100), new Vector2(200, 50));
            settingsButton.onClick.AddListener(ShowSettings);

            patientSelectionPanel = panel;
        }

        private void CreateExerciseSelectionPanel(Transform parent)
        {
            GameObject panel = new GameObject("ExerciseSelectionPanel");
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Title
            CreateText(panel.transform, "Select Exercise", new Vector2(0, 150), new Vector2(400, 50), 24, TextAlignmentOptions.Center);

            // Exercise buttons
            throwingExerciseButton = CreateButton(panel.transform, "Ball Throwing", new Vector2(0, 50), new Vector2(250, 50));
            throwingExerciseButton.onClick.AddListener(() => LoadExerciseScene("ThrowBall"));

            bridgeExerciseButton = CreateButton(panel.transform, "Bridge Building", new Vector2(0, -10), new Vector2(250, 50));
            bridgeExerciseButton.onClick.AddListener(() => LoadExerciseScene("Bridge"));

            squatExerciseButton = CreateButton(panel.transform, "Squat Dodge", new Vector2(0, -70), new Vector2(250, 50));
            squatExerciseButton.onClick.AddListener(() => LoadExerciseScene("Squat"));

            // Back button
            backButton = CreateButton(panel.transform, "Back", new Vector2(0, -130), new Vector2(150, 40));
            backButton.onClick.AddListener(() => ShowPatientSelection());

            exerciseSelectionPanel = panel;
        }

        private void CreateSettingsPanel(Transform parent)
        {
            GameObject panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Title
            CreateText(panel.transform, "Settings", new Vector2(0, 150), new Vector2(400, 50), 24, TextAlignmentOptions.Center);

            // Back button
            backButton = CreateButton(panel.transform, "Back", new Vector2(0, -130), new Vector2(150, 40));
            backButton.onClick.AddListener(() => ShowPatientSelection());

            settingsPanel = panel;
        }

        private TMP_Text CreateText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;

            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.alignment = alignment;
            textMesh.color = Color.white;

            return textMesh;
        }

        private Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent, false);

            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;

            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var button = buttonObj.AddComponent<Button>();

            // Create button text
            var textObj = CreateText(buttonObj.transform, text, Vector2.zero, size, 16, TextAlignmentOptions.Center);

            return button;
        }

        private void InitializeMenu()
        {
            ShowPatientSelection();
            UpdateWelcomeText();
        }

        private void ShowPatientSelection()
        {
            if (patientSelectionPanel != null) patientSelectionPanel.SetActive(true);
            if (exerciseSelectionPanel != null) exerciseSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void ShowExerciseSelection()
        {
            if (patientSelectionPanel != null) patientSelectionPanel.SetActive(false);
            if (exerciseSelectionPanel != null) exerciseSelectionPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void ShowSettings()
        {
            if (patientSelectionPanel != null) patientSelectionPanel.SetActive(false);
            if (exerciseSelectionPanel != null) exerciseSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void ShowNewPatientDialog()
        {
            // Show exercise selection for new patient
            ShowExerciseSelection();
        }

        private void ShowLoadPatientDialog()
        {
            // For now, just show exercise selection
            // In a full implementation, you'd show a patient list
            ShowExerciseSelection();
        }

        private void LoadExerciseScene(string sceneName)
        {
            if (uiManager != null)
            {
                uiManager.ShowLoading($"Loading {sceneName}...");
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        private void UpdateWelcomeText()
        {
            if (welcomeText != null)
            {
                // Get current patient from data manager
                if (dataManager != null)
                {
                    var currentPatient = dataManager.GetCurrentProfile();
                    if (currentPatient != null)
                    {
                        welcomeText.text = $"Welcome back, {currentPatient.firstName}!";
                    }
                }
            }
        }

        // Public methods
        public void SetPatientName(string name)
        {
            if (dataManager != null && !string.IsNullOrEmpty(name))
            {
                dataManager.CreateNewProfile(name, "", System.DateTime.Now);
                UpdateWelcomeText();
            }
        }
    }
}
