#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class BridgeLevelEditor : EditorWindow
{
    private BridgeLevelData currentLevel;
    private MultiPlankBridgeBuilder previewBuilder;
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private bool showPreview = true;

    [MenuItem("VR Fitness/Bridge Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<BridgeLevelEditor>("Bridge Level Editor");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Bridge Level Editor", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        // Level Selection
        DrawLevelSelection();
        
        if (currentLevel != null)
        {
            DrawLevelConfiguration();
            DrawPreviewControls();
            DrawActionButtons();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawLevelSelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Current Level:", GUILayout.Width(100));
        
        BridgeLevelData newLevel = (BridgeLevelData)EditorGUILayout.ObjectField(
            currentLevel, typeof(BridgeLevelData), false);
            
        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;
            if (currentLevel != null)
                LoadLevelIntoPreview();
        }

        if (GUILayout.Button("New Level", GUILayout.Width(80)))
            CreateNewLevel();
            
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    void DrawLevelConfiguration()
    {
        EditorGUI.BeginChangeCheck();

        // Basic Settings
        EditorGUILayout.LabelField("Basic Configuration", EditorStyles.boldLabel);
        currentLevel.levelName = EditorGUILayout.TextField("Level Name", currentLevel.levelName);
        currentLevel.description = EditorGUILayout.TextField("Description", currentLevel.description);
        currentLevel.estimatedTime = EditorGUILayout.FloatField("Estimated Time (s)", currentLevel.estimatedTime);
        currentLevel.difficultyRating = EditorGUILayout.IntSlider("Difficulty", currentLevel.difficultyRating, 1, 5);

        EditorGUILayout.Space();

        // Bridge Structure
        EditorGUILayout.LabelField("Bridge Structure", EditorStyles.boldLabel);
        currentLevel.numberOfPlanks = EditorGUILayout.IntSlider("Number of Planks", currentLevel.numberOfPlanks, 1, 20);
        currentLevel.totalBridgeLength = EditorGUILayout.Slider("Bridge Length", currentLevel.totalBridgeLength, 1f, 50f);
        currentLevel.plankWidth = EditorGUILayout.Slider("Plank Width", currentLevel.plankWidth, 0.1f, 2f);
        
        // Real-time calculations
        float plankLength = (currentLevel.totalBridgeLength - (currentLevel.numberOfPlanks - 1) * currentLevel.plankGap) / currentLevel.numberOfPlanks;
        EditorGUILayout.LabelField($"Calculated Plank Length: {plankLength:F2}m", EditorStyles.helpBox);

        EditorGUILayout.Space();

        // Platform Settings
        currentLevel.createPlatforms = EditorGUILayout.Toggle("Create Platforms", currentLevel.createPlatforms);
        if (currentLevel.createPlatforms)
        {
            EditorGUI.indentLevel++;
            currentLevel.platformLength = EditorGUILayout.FloatField("Platform Length", currentLevel.platformLength);
            currentLevel.platformWidth = EditorGUILayout.FloatField("Platform Width", currentLevel.platformWidth);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Advanced Settings
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
            currentLevel.plankMass = EditorGUILayout.FloatField("Plank Mass", currentLevel.plankMass);
            currentLevel.jointSpring = EditorGUILayout.FloatField("Joint Spring", currentLevel.jointSpring);
            currentLevel.jointDamper = EditorGUILayout.FloatField("Joint Damper", currentLevel.jointDamper);
            
            EditorGUILayout.LabelField("Balance", EditorStyles.boldLabel);
            currentLevel.maxBalanceOffset = EditorGUILayout.FloatField("Max Balance Offset", currentLevel.maxBalanceOffset);
            currentLevel.failureDelay = EditorGUILayout.FloatField("Failure Delay", currentLevel.failureDelay);
            
            EditorGUILayout.LabelField("Ropes", EditorStyles.boldLabel);
            currentLevel.useRopes = EditorGUILayout.Toggle("Use Rope Supports", currentLevel.useRopes);
            if (currentLevel.useRopes)
            {
                currentLevel.ropeHeight = EditorGUILayout.FloatField("Rope Height", currentLevel.ropeHeight);
                currentLevel.ropeSag = EditorGUILayout.FloatField("Rope Sag", currentLevel.ropeSag);
            }
            
            EditorGUI.indentLevel--;
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentLevel);
            if (showPreview && previewBuilder != null)
                UpdatePreview();
        }
    }

    void DrawPreviewControls()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        
        if (GUILayout.Button("Update Preview"))
            UpdatePreview();
            
        if (GUILayout.Button("Test in Scene"))
            TestInScene();
            
        EditorGUILayout.EndHorizontal();

        if (showPreview && previewBuilder == null)
        {
            EditorGUILayout.HelpBox("No preview available. Create or select a bridge in the scene.", MessageType.Info);
        }
    }

    void DrawActionButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save Level"))
        {
            EditorUtility.SetDirty(currentLevel);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Level Saved!"));
        }
        
        if (GUILayout.Button("Build in Scene"))
            BuildInScene();
            
        if (GUILayout.Button("Validate Level"))
            ValidateLevel();
            
        EditorGUILayout.EndHorizontal();
    }

    void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Bridge Level",
            "NewBridgeLevel",
            "asset",
            "Choose location for new bridge level");
            
        if (!string.IsNullOrEmpty(path))
        {
            BridgeLevelData newLevel = CreateInstance<BridgeLevelData>();
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            currentLevel = newLevel;
            LoadLevelIntoPreview();
        }
    }

    void LoadLevelIntoPreview()
    {
        previewBuilder = FindObjectOfType<MultiPlankBridgeBuilder>();
        if (previewBuilder != null && currentLevel != null)
        {
            currentLevel.ApplyToBridgeBuilder(previewBuilder);
            UpdatePreview();
        }
    }

    void UpdatePreview()
    {
        if (previewBuilder != null && currentLevel != null)
        {
            currentLevel.ApplyToBridgeBuilder(previewBuilder);
            previewBuilder.BuildBridge();
        }
    }

    void TestInScene()
    {
        if (previewBuilder != null)
        {
            UpdatePreview();
            
            // Position camera to view the bridge
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.pivot = previewBuilder.transform.position;
                sceneView.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                sceneView.size = currentLevel.totalBridgeLength * 1.5f;
                sceneView.Repaint();
            }
        }
    }

    void BuildInScene()
    {
        var builder = FindObjectOfType<MultiPlankBridgeBuilder>();
        if (builder == null)
        {
            // Create new bridge builder
            GameObject bridgeObj = new GameObject("Bridge");
            builder = bridgeObj.AddComponent<MultiPlankBridgeBuilder>();
            previewBuilder = builder;
        }
        
        if (currentLevel != null)
        {
            currentLevel.ApplyToBridgeBuilder(builder);
            builder.BuildBridge();
            
            Selection.activeGameObject = builder.gameObject;
            SceneView.FrameLastActiveSceneView();
        }
    }

    void ValidateLevel()
    {
        if (currentLevel == null) return;

        bool isValid = true;
        string validationMessage = "Level Validation:\n";

        // Check basic parameters
        if (currentLevel.numberOfPlanks <= 0)
        {
            validationMessage += "• Number of planks must be greater than 0\n";
            isValid = false;
        }

        if (currentLevel.totalBridgeLength <= 0)
        {
            validationMessage += "• Bridge length must be greater than 0\n";
            isValid = false;
        }

        // Check plank dimensions
        float plankLength = (currentLevel.totalBridgeLength - (currentLevel.numberOfPlanks - 1) * currentLevel.plankGap) / currentLevel.numberOfPlanks;
        if (plankLength <= 0)
        {
            validationMessage += "• Invalid plank configuration - planks would have negative length\n";
            isValid = false;
        }

        // Check physics values
        if (currentLevel.plankMass <= 0)
        {
            validationMessage += "• Plank mass must be greater than 0\n";
            isValid = false;
        }

        if (isValid)
        {
            validationMessage += "✓ Level configuration is valid!";
            EditorUtility.DisplayDialog("Level Validation", validationMessage, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Level Validation Failed", validationMessage, "OK");
        }
    }
}
#endif