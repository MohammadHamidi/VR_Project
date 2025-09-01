using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRRehab.DataPersistence;
using VRRehab.UI;

namespace VRRehab.SceneBuilders
{
    public class ProfileManagementBuilder : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private DataPersistenceManager dataManager;
        [SerializeField] private UIManager uiManager;

        [Header("UI Configuration")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.4f, 0.6f);
        [SerializeField] private Color secondaryColor = new Color(0.3f, 0.5f, 0.7f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.15f, 0.2f);

        // Current state
        private PatientProfile currentProfile;
        private List<PatientProfile> allProfiles;
        private bool isEditing = false;

        void Start()
        {
            StartCoroutine(BuildProfileManagementScene());
        }

        private IEnumerator BuildProfileManagementScene()
        {
            // Find required systems
            FindRequiredSystems();

            // Create main canvas and layout
            yield return CreateMainCanvas();

            // Create header section
            CreateHeaderSection();

            // Create profile list panel
            CreateProfileListPanel();

            // Create profile editor panel
            CreateProfileEditorPanel();

            // Create navigation buttons
            CreateNavigationButtons();

            // Load existing profiles
            LoadProfiles();

            Debug.Log("Profile Management scene built successfully");
        }

        private void FindRequiredSystems()
        {
            if (dataManager == null)
                dataManager = FindObjectOfType<DataPersistenceManager>();

            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
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
            titleText.text = "Patient Profile Management";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.3f, 0.1f);
            titleRect.anchorMax = new Vector2(0.7f, 0.9f);
            titleRect.sizeDelta = Vector2.zero;

            // Create back button
            CreateHeaderButton(headerPanel, "Back", "MainMenu", new Vector2(0.02f, 0.5f), new Vector2(0.15f, 0.8f));
        }

        private void CreateProfileListPanel()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Create left panel for profile list
            GameObject listPanel = new GameObject("ProfileListPanel");
            listPanel.transform.SetParent(canvas.transform, false);

            Image listImage = listPanel.AddComponent<Image>();
            listImage.color = new Color(0.15f, 0.2f, 0.25f);

            RectTransform listRect = listPanel.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.02f, 0.1f);
            listRect.anchorMax = new Vector2(0.35f, 0.88f);
            listRect.sizeDelta = Vector2.zero;

            // Create list title
            GameObject listTitleObj = new GameObject("ListTitle");
            listTitleObj.transform.SetParent(listPanel.transform, false);

            TextMeshProUGUI listTitle = listTitleObj.AddComponent<TextMeshProUGUI>();
            listTitle.text = "Patient Profiles";
            listTitle.fontSize = 20;
            listTitle.alignment = TextAlignmentOptions.Center;
            listTitle.color = Color.white;

            RectTransform listTitleRect = listTitleObj.GetComponent<RectTransform>();
            listTitleRect.anchorMin = new Vector2(0.1f, 0.9f);
            listTitleRect.anchorMax = new Vector2(0.9f, 0.98f);
            listTitleRect.sizeDelta = Vector2.zero;

            // Create scrollable list area
            CreateScrollableProfileList(listPanel);

            // Create action buttons
            CreateListActionButtons(listPanel);
        }

        private void CreateScrollableProfileList(GameObject parent)
        {
            // Create scroll view
            GameObject scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(parent.transform, false);

            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            Image scrollImage = scrollViewObj.AddComponent<Image>();
            scrollImage.color = new Color(0.1f, 0.15f, 0.2f);

            RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.05f, 0.15f);
            scrollRectTransform.anchorMax = new Vector2(0.95f, 0.75f);
            scrollRectTransform.sizeDelta = Vector2.zero;

            // Create viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = Color.clear;

            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            scrollRect.viewport = viewportRect;

            // Create content area
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 1000);
            contentRect.pivot = new Vector2(0.5f, 1);

            VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
        }

        private void CreateListActionButtons(GameObject parent)
        {
            string[] buttonLabels = { "New Profile", "Load Profile", "Delete Profile" };
            float buttonHeight = 0.08f;

            for (int i = 0; i < buttonLabels.Length; i++)
            {
                GameObject buttonObj = new GameObject($"Button_{buttonLabels[i].Replace(" ", "")}");
                buttonObj.transform.SetParent(parent.transform, false);

                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = secondaryColor;

                Button button = buttonObj.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
                colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
                button.colors = colors;

                // Create button text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);

                TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = buttonLabels[i];
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                // Position button
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                float buttonTop = 0.08f + (i * (buttonHeight + 0.02f));
                buttonRect.anchorMin = new Vector2(0.1f, 1f - buttonTop - buttonHeight);
                buttonRect.anchorMax = new Vector2(0.9f, 1f - buttonTop);
                buttonRect.sizeDelta = Vector2.zero;

                // Add click handlers
                string buttonName = buttonLabels[i];
                button.onClick.AddListener(() => OnListButtonClicked(buttonName));
            }
        }

        private void CreateProfileEditorPanel()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Create right panel for profile editor
            GameObject editorPanel = new GameObject("ProfileEditorPanel");
            editorPanel.transform.SetParent(canvas.transform, false);

            Image editorImage = editorPanel.AddComponent<Image>();
            editorImage.color = new Color(0.15f, 0.2f, 0.25f);

            RectTransform editorRect = editorPanel.GetComponent<RectTransform>();
            editorRect.anchorMin = new Vector2(0.37f, 0.1f);
            editorRect.anchorMax = new Vector2(0.98f, 0.88f);
            editorRect.sizeDelta = Vector2.zero;

            // Create editor sections
            CreateBasicInfoSection(editorPanel);
            CreateMedicalInfoSection(editorPanel);
            CreatePreferencesSection(editorPanel);
            CreateActionButtons(editorPanel);
        }

        private void CreateBasicInfoSection(GameObject parent)
        {
            GameObject sectionObj = CreateFormSection(parent, "Basic Information", new Vector2(0.02f, 0.8f), new Vector2(0.98f, 0.98f));

            string[] fieldLabels = { "First Name", "Last Name", "Date of Birth", "Gender" };
            for (int i = 0; i < fieldLabels.Length; i++)
            {
                CreateFormField(sectionObj, fieldLabels[i], new Vector2(0.05f, 0.8f - (i * 0.15f)), new Vector2(0.95f, 0.9f - (i * 0.15f)));
            }
        }

        private void CreateMedicalInfoSection(GameObject parent)
        {
            GameObject sectionObj = CreateFormSection(parent, "Medical Information", new Vector2(0.02f, 0.45f), new Vector2(0.98f, 0.78f));

            string[] fieldLabels = { "Medical Record #", "Primary Physician", "Diagnosis", "Current Condition" };
            for (int i = 0; i < fieldLabels.Length; i++)
            {
                CreateFormField(sectionObj, fieldLabels[i], new Vector2(0.05f, 0.8f - (i * 0.15f)), new Vector2(0.95f, 0.9f - (i * 0.15f)));
            }
        }

        private void CreatePreferencesSection(GameObject parent)
        {
            GameObject sectionObj = CreateFormSection(parent, "Preferences", new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.43f));

            // Create toggle options
            string[] toggleLabels = { "Voice Guidance", "Haptic Feedback", "Progress Notifications" };
            for (int i = 0; i < toggleLabels.Length; i++)
            {
                CreateToggleField(sectionObj, toggleLabels[i], new Vector2(0.05f, 0.8f - (i * 0.2f)), new Vector2(0.95f, 0.9f - (i * 0.2f)));
            }
        }

        private GameObject CreateFormSection(GameObject parent, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject sectionObj = new GameObject($"{title.Replace(" ", "")}Section");
            sectionObj.transform.SetParent(parent.transform, false);

            Image sectionImage = sectionObj.AddComponent<Image>();
            sectionImage.color = new Color(0.2f, 0.25f, 0.3f);

            // Create section title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(sectionObj.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 18;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.9f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;

            RectTransform sectionRect = sectionObj.GetComponent<RectTransform>();
            sectionRect.anchorMin = anchorMin;
            sectionRect.anchorMax = anchorMax;
            sectionRect.sizeDelta = Vector2.zero;

            return sectionObj;
        }

        private void CreateFormField(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject fieldObj = new GameObject($"{label.Replace(" ", "")}Field");
            fieldObj.transform.SetParent(parent.transform, false);

            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(fieldObj.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label + ":";
            labelText.fontSize = 14;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.1f);
            labelRect.anchorMax = new Vector2(0.3f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Create input field
            GameObject inputObj = new GameObject("Input");
            inputObj.transform.SetParent(fieldObj.transform, false);

            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.3f, 0.35f, 0.4f);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textComponent = inputObj.AddComponent<TextMeshProUGUI>();
            inputField.textComponent.fontSize = 14;
            inputField.textComponent.color = Color.white;
            inputField.textComponent.alignment = TextAlignmentOptions.Left;

            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.32f, 0.1f);
            inputRect.anchorMax = new Vector2(1, 0.9f);
            inputRect.sizeDelta = Vector2.zero;

            RectTransform fieldRect = fieldObj.GetComponent<RectTransform>();
            fieldRect.anchorMin = anchorMin;
            fieldRect.anchorMax = anchorMax;
            fieldRect.sizeDelta = Vector2.zero;
        }

        private void CreateToggleField(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject fieldObj = new GameObject($"{label.Replace(" ", "")}Toggle");
            fieldObj.transform.SetParent(parent.transform, false);

            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(fieldObj.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.1f);
            labelRect.anchorMax = new Vector2(0.7f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Create toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(fieldObj.transform, false);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;

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

            RectTransform fieldRect = fieldObj.GetComponent<RectTransform>();
            fieldRect.anchorMin = anchorMin;
            fieldRect.anchorMax = anchorMax;
            fieldRect.sizeDelta = Vector2.zero;
        }

        private void CreateActionButtons(GameObject parent)
        {
            string[] buttonLabels = { "Save Profile", "Cancel", "Clear Form" };
            float buttonWidth = 0.3f;
            float buttonSpacing = 0.02f;

            for (int i = 0; i < buttonLabels.Length; i++)
            {
                GameObject buttonObj = new GameObject($"Button_{buttonLabels[i].Replace(" ", "")}");
                buttonObj.transform.SetParent(parent.transform, false);

                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = i == 0 ? primaryColor : secondaryColor;

                Button button = buttonObj.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
                colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
                button.colors = colors;

                // Create button text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);

                TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = buttonLabels[i];
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                // Position button
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                float buttonLeft = 0.02f + (i * (buttonWidth + buttonSpacing));
                buttonRect.anchorMin = new Vector2(buttonLeft, 0.02f);
                buttonRect.anchorMax = new Vector2(buttonLeft + buttonWidth, 0.08f);
                buttonRect.sizeDelta = Vector2.zero;

                // Add click handlers
                string buttonName = buttonLabels[i];
                button.onClick.AddListener(() => OnEditorButtonClicked(buttonName));
            }
        }

        private void CreateNavigationButtons()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Create bottom navigation panel
            GameObject navPanel = new GameObject("NavigationPanel");
            navPanel.transform.SetParent(canvas.transform, false);

            Image navImage = navPanel.AddComponent<Image>();
            navImage.color = new Color(0.1f, 0.15f, 0.2f);

            RectTransform navRect = navPanel.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 0);
            navRect.anchorMax = new Vector2(1, 0.08f);
            navRect.sizeDelta = Vector2.zero;

            // Create navigation buttons
            CreateNavButton(navPanel, "Main Menu", "MainMenu", new Vector2(0.02f, 0.1f), new Vector2(0.15f, 0.9f));
            CreateNavButton(navPanel, "Analytics", "Analytics", new Vector2(0.17f, 0.1f), new Vector2(0.3f, 0.9f));
            CreateNavButton(navPanel, "Settings", "Settings", new Vector2(0.32f, 0.1f), new Vector2(0.45f, 0.9f));
        }

        private void CreateNavButton(GameObject parent, string label, string targetScene, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"NavButton_{label.Replace(" ", "")}");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = secondaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Create button text
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

            button.onClick.AddListener(() => OnNavButtonClicked(targetScene));
        }

        private void CreateHeaderButton(GameObject parent, string label, string targetScene, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"HeaderButton_{label}");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = secondaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Create button text
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

            button.onClick.AddListener(() => OnNavButtonClicked(targetScene));
        }

        // Event handlers
        private void OnListButtonClicked(string buttonName)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            switch (buttonName)
            {
                case "New Profile":
                    CreateNewProfile();
                    break;
                case "Load Profile":
                    LoadSelectedProfile();
                    break;
                case "Delete Profile":
                    DeleteSelectedProfile();
                    break;
            }
        }

        private void OnEditorButtonClicked(string buttonName)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            switch (buttonName)
            {
                case "Save Profile":
                    SaveProfile();
                    break;
                case "Cancel":
                    CancelEdit();
                    break;
                case "Clear Form":
                    ClearForm();
                    break;
            }
        }

        private void OnNavButtonClicked(string targetScene)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            // Use scene transition manager if available
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

        // Profile management methods
        private void LoadProfiles()
        {
            if (dataManager != null)
            {
                allProfiles = dataManager.GetAllProfiles();
                PopulateProfileList();
            }
        }

        private void PopulateProfileList()
        {
            // Implementation would populate the scrollable list with profile entries
            Debug.Log($"Loaded {allProfiles.Count} profiles");
        }

        private void CreateNewProfile()
        {
            currentProfile = new PatientProfile();
            isEditing = true;
            ClearForm();
            if (uiManager != null)
            {
                uiManager.ShowSuccess("New profile created. Fill in the details and save.");
            }
        }

        private void LoadSelectedProfile()
        {
            // Implementation would load the selected profile from the list
            if (uiManager != null)
            {
                uiManager.ShowSuccess("Profile loaded successfully.");
            }
        }

        private void DeleteSelectedProfile()
        {
            // Implementation would delete the selected profile
            if (uiManager != null)
            {
                uiManager.ShowWarning("Profile deleted.");
            }
        }

        private void SaveProfile()
        {
            // Implementation would save the current profile data
            if (dataManager != null && currentProfile != null)
            {
                dataManager.SaveProfile(currentProfile);
                if (uiManager != null)
                {
                    uiManager.ShowSuccess("Profile saved successfully!");
                }
            }
        }

        private void CancelEdit()
        {
            isEditing = false;
            currentProfile = null;
            ClearForm();
        }

        private void ClearForm()
        {
            // Implementation would clear all form fields
            if (uiManager != null)
            {
                uiManager.ShowNotification("Form cleared.", VRRehab.UI.UIManager.NotificationData.NotificationType.Info);
            }
        }
    }
}
