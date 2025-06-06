#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class TargetLevelEditor : EditorWindow
{
    private TargetLevelData currentLevel;
    private Vector2 scrollPosition;
    private Vector2 targetScrollPosition;
    private int selectedTargetIndex = -1;
    private bool showScenePreview = true;

    // ── NEW FIELD: number of targets to generate ──
    private int generateCount = 5;

    [MenuItem("VR Fitness/Target Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<TargetLevelEditor>("Target Level Editor");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Target Level Editor", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        DrawLevelSelection();

        if (currentLevel != null)
        {
            DrawBasicConfiguration();
            DrawBallConfiguration();
            DrawTargetConfiguration();
            DrawTargetList();
            DrawActionButtons();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawLevelSelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Current Level:", GUILayout.Width(100));

        TargetLevelData newLevel = (TargetLevelData)EditorGUILayout.ObjectField(
            currentLevel, typeof(TargetLevelData), false);

        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;
        }

        if (GUILayout.Button("New Level", GUILayout.Width(80)))
            CreateNewLevel();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    void DrawBasicConfiguration()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Basic Configuration", EditorStyles.boldLabel);
        currentLevel.levelName = EditorGUILayout.TextField("Level Name", currentLevel.levelName);
        currentLevel.description = EditorGUILayout.TextField("Description", currentLevel.description);
        currentLevel.timeLimit = EditorGUILayout.FloatField("Time Limit (s)", currentLevel.timeLimit);
        currentLevel.difficultyRating = EditorGUILayout.IntSlider("Difficulty", currentLevel.difficultyRating, 1, 5);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Objectives", EditorStyles.boldLabel);
        int maxHits = Mathf.Max(1, currentLevel.targetRings.Count);
        currentLevel.requiredHits = EditorGUILayout.IntSlider("Required Hits", currentLevel.requiredHits, 1, maxHits);
        currentLevel.requireAllTargets = EditorGUILayout.Toggle("Require All Targets", currentLevel.requireAllTargets);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawBallConfiguration()
    {
        EditorGUILayout.LabelField("Ball Configuration", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        currentLevel.ballRespawnDelay = EditorGUILayout.FloatField("Respawn Delay", currentLevel.ballRespawnDelay);
        currentLevel.ballSpawnPosition = EditorGUILayout.Vector3Field("Spawn Position", currentLevel.ballSpawnPosition);
        currentLevel.maxActiveBalls = EditorGUILayout.IntSlider("Max Active Balls", currentLevel.maxActiveBalls, 1, 10);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawTargetConfiguration()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scoring", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        currentLevel.pointsPerHit = EditorGUILayout.IntField("Points Per Hit", currentLevel.pointsPerHit);
        currentLevel.timeBonus = EditorGUILayout.IntField("Time Bonus", currentLevel.timeBonus);
        currentLevel.accuracyBonus = EditorGUILayout.IntField("Accuracy Bonus", currentLevel.accuracyBonus);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawTargetList()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Rings", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Target", GUILayout.Width(80)))
        {
            currentLevel.targetRings.Add(new TargetLevelData.TargetRingSetup());
            EditorUtility.SetDirty(currentLevel);
        }

        EditorGUILayout.EndHorizontal();

        targetScrollPosition = EditorGUILayout.BeginScrollView(targetScrollPosition, GUILayout.Height(300));

        for (int i = 0; i < currentLevel.targetRings.Count; i++)
        {
            DrawTargetRing(i);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawTargetRing(int index)
    {
        var target = currentLevel.targetRings[index];
        bool isSelected = selectedTargetIndex == index;

        EditorGUILayout.BeginVertical(isSelected ? EditorStyles.helpBox : EditorStyles.textArea);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Target {index + 1}", EditorStyles.boldLabel);

        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            selectedTargetIndex = isSelected ? -1 : index;
            if (selectedTargetIndex == index)
            {
                // Focus scene view on this target
                SceneView.lastActiveSceneView?.LookAt(target.position);
            }
        }

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            currentLevel.targetRings.RemoveAt(index);
            if (selectedTargetIndex == index) selectedTargetIndex = -1;
            EditorUtility.SetDirty(currentLevel);
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (isSelected)
        {
            EditorGUI.BeginChangeCheck();

            target.position = EditorGUILayout.Vector3Field("Position", target.position);
            target.rotation = EditorGUILayout.Vector3Field("Rotation", target.rotation);
            target.scale = EditorGUILayout.Slider("Scale", target.scale, 0.1f, 3f);
            target.ringColor = EditorGUILayout.ColorField("Color", target.ringColor);
            target.pointValue = EditorGUILayout.IntField("Point Value", target.pointValue);

            EditorGUILayout.Space();
            target.isMoving = EditorGUILayout.Toggle("Moving Target", target.isMoving);

            if (target.isMoving)
            {
                EditorGUI.indentLevel++;
                target.movementRange = EditorGUILayout.Vector3Field("Movement Range", target.movementRange);
                target.movementSpeed = EditorGUILayout.Slider("Movement Speed", target.movementSpeed, 0.1f, 10f);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(currentLevel);
        }
        else
        {
            EditorGUILayout.LabelField($"Position: {target.position}");
            EditorGUILayout.LabelField($"Points: {target.pointValue} | Moving: {target.isMoving}");
        }

        EditorGUILayout.EndVertical();
    }

    void DrawActionButtons()
    {
        EditorGUILayout.Space();

        // ── NEW UI: Let user set how many targets to generate ──
        EditorGUILayout.BeginHorizontal();
        generateCount = EditorGUILayout.IntField("Number of targets to generate:", generateCount);
        if (generateCount < 0) generateCount = 0;
        EditorGUILayout.EndHorizontal();
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

        if (GUILayout.Button("Generate Positions"))
            GenerateTargetPositions(generateCount);

        EditorGUILayout.EndHorizontal();
    }

    void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Target Level",
            "NewTargetLevel",
            "asset",
            "Choose location for new target level");

        if (!string.IsNullOrEmpty(path))
        {
            TargetLevelData newLevel = CreateInstance<TargetLevelData>();
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            currentLevel = newLevel;
        }
    }

    void BuildInScene()
    {
        if (currentLevel == null) return;

        // Find or create parent object
        GameObject levelParent = GameObject.Find("TargetLevel");
        if (levelParent == null)
        {
            levelParent = new GameObject("TargetLevel");
        }

        // Clear existing targets
        while (levelParent.transform.childCount > 0)
        {
            DestroyImmediate(levelParent.transform.GetChild(0).gameObject);
        }

        // Create targets
        for (int i = 0; i < currentLevel.targetRings.Count; i++)
        {
            var targetData = currentLevel.targetRings[i];

            // Create target ring (you'll need a prefab)
            GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetObj.name = $"Target_{i:00}";
            targetObj.transform.SetParent(levelParent.transform);
            targetObj.transform.position = targetData.position;
            targetObj.transform.rotation = Quaternion.Euler(targetData.rotation);
            targetObj.transform.localScale = Vector3.one * targetData.scale;

            // Add TargetRing component
            var targetRing = targetObj.GetComponent<TargetRing>();
            if (targetRing == null)
                targetRing = targetObj.AddComponent<TargetRing>();

            // Configure collider
            var collider = targetObj.GetComponent<Collider>();
            collider.isTrigger = true;

            // Set color
            var renderer = targetObj.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = targetData.ringColor;
            renderer.material = material;
        }

        Selection.activeGameObject = levelParent;
        SceneView.FrameLastActiveSceneView();
    }

    // ── UPDATED: accepts an integer count ──
    void GenerateTargetPositions(int targetCount)
    {
        if (currentLevel == null) return;
        if (targetCount <= 0) return;

        currentLevel.targetRings.Clear();

        for (int i = 0; i < targetCount; i++)
        {
            var target = new TargetLevelData.TargetRingSetup();

            // Generate random positions in a reasonable range
            target.position = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(1f, 3f),
                Random.Range(2f, 8f)
            );

            target.rotation = Vector3.zero;
            target.scale = Random.Range(0.8f, 1.2f);
            target.ringColor = Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 0.9f);
            target.pointValue = (i + 1) * 100;

            currentLevel.targetRings.Add(target);
        }

        EditorUtility.SetDirty(currentLevel);
    }
}
#endif
