#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CombatLevelEditor : EditorWindow
{
    private CombatLevelData currentLevel;
    private Vector2 scrollPosition;
    private Vector2 waveScrollPosition;
    private bool showSpawnWaves = true;
    private int selectedWaveIndex = -1;

    [MenuItem("VR Fitness/Combat Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<CombatLevelEditor>("Combat Level Editor");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Combat Level Editor", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        DrawLevelSelection();
        
        if (currentLevel != null)
        {
            DrawBasicConfiguration();
            DrawSpawnConfiguration();
            DrawDodgeMechanics();
            DrawSpawnWaves();
            DrawActionButtons();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawLevelSelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Current Level:", GUILayout.Width(100));
        
        CombatLevelData newLevel = (CombatLevelData)EditorGUILayout.ObjectField(
            currentLevel, typeof(CombatLevelData), false);
            
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
        currentLevel.levelDuration = EditorGUILayout.FloatField("Duration (s)", currentLevel.levelDuration);
        currentLevel.difficultyRating = EditorGUILayout.IntSlider("Difficulty", currentLevel.difficultyRating, 1, 5);

        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawSpawnConfiguration()
    {
        EditorGUILayout.LabelField("Spawn Configuration", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        currentLevel.baseSpawnInterval = EditorGUILayout.Slider("Base Spawn Interval", currentLevel.baseSpawnInterval, 0.1f, 5f);
        currentLevel.minSpawnInterval = EditorGUILayout.Slider("Min Spawn Interval", currentLevel.minSpawnInterval, 0.1f, 2f);
        currentLevel.difficultyIncrement = EditorGUILayout.Slider("Difficulty Increment", currentLevel.difficultyIncrement, 0.01f, 0.2f);
        currentLevel.maxSimultaneousCubes = EditorGUILayout.IntSlider("Max Simultaneous Cubes", currentLevel.maxSimultaneousCubes, 1, 10);

        // Difficulty curve visualization
        DrawDifficultyCurve();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Cube Properties", EditorStyles.boldLabel);
        currentLevel.cubeSpeed = EditorGUILayout.Slider("Cube Speed", currentLevel.cubeSpeed, 0.5f, 10f);
        currentLevel.cubeSize = EditorGUILayout.Slider("Cube Size", currentLevel.cubeSize, 0.5f, 2f);
        currentLevel.spawnOffset = EditorGUILayout.Vector3Field("Spawn Offset", currentLevel.spawnOffset);
        currentLevel.destroyZ = EditorGUILayout.FloatField("Destroy Z Position", currentLevel.destroyZ);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawDifficultyCurve()
    {
        EditorGUILayout.LabelField("Difficulty Progression", EditorStyles.miniBoldLabel);
        
        float timeAtMinDifficulty = (currentLevel.baseSpawnInterval - currentLevel.minSpawnInterval) / currentLevel.difficultyIncrement;
        EditorGUILayout.LabelField($"Time to reach max difficulty: {timeAtMinDifficulty:F1}s", EditorStyles.helpBox);
        
        // Simple visual representation
        Rect rect = GUILayoutUtility.GetRect(200, 50);
        EditorGUI.DrawRect(rect, Color.gray);
        
        for (int i = 0; i < 10; i++)
        {
            float t = i / 9f;
            float interval = Mathf.Max(currentLevel.baseSpawnInterval - (t * timeAtMinDifficulty * currentLevel.difficultyIncrement), currentLevel.minSpawnInterval);
            float height = Mathf.InverseLerp(currentLevel.minSpawnInterval, currentLevel.baseSpawnInterval, interval);
            
            float x = rect.x + t * rect.width;
            float y = rect.y + (1f - height) * rect.height;
            EditorGUI.DrawRect(new Rect(x, y, 2, rect.height - (y - rect.y)), Color.red);
        }
    }

    void DrawDodgeMechanics()
    {
        EditorGUILayout.LabelField("Dodge Mechanics", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        currentLevel.dodgeZoneStart = EditorGUILayout.FloatField("Dodge Zone Start", currentLevel.dodgeZoneStart);
        currentLevel.dodgeZoneEnd = EditorGUILayout.FloatField("Dodge Zone End", currentLevel.dodgeZoneEnd);
        currentLevel.squatThreshold = EditorGUILayout.Slider("Squat Threshold", currentLevel.squatThreshold, 0.1f, 1f);
        currentLevel.dodgeDuration = EditorGUILayout.Slider("Dodge Duration", currentLevel.dodgeDuration, 0.1f, 2f);

        // Validate dodge zone
        if (currentLevel.dodgeZoneStart <= currentLevel.dodgeZoneEnd)
        {
            EditorGUILayout.HelpBox("Dodge Zone Start should be greater than Dodge Zone End", MessageType.Warning);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Scoring", EditorStyles.boldLabel);
        currentLevel.basePointsPerCube = EditorGUILayout.IntField("Base Points Per Cube", currentLevel.basePointsPerCube);
        currentLevel.bonusMultiplier = EditorGUILayout.IntField("Bonus Multiplier", currentLevel.bonusMultiplier);
        currentLevel.coinValue = EditorGUILayout.IntField("Coin Value", currentLevel.coinValue);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(currentLevel);
    }

    void DrawSpawnWaves()
    {
        EditorGUILayout.Space();
        showSpawnWaves = EditorGUILayout.Foldout(showSpawnWaves, "Spawn Waves");
        
        if (showSpawnWaves)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Waves: {currentLevel.spawnWaves.Count}");
            
            if (GUILayout.Button("Add Wave", GUILayout.Width(80)))
            {
                currentLevel.spawnWaves.Add(new CombatLevelData.CubeSpawnWave());
                EditorUtility.SetDirty(currentLevel);
            }
            
            EditorGUILayout.EndHorizontal();

            waveScrollPosition = EditorGUILayout.BeginScrollView(waveScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < currentLevel.spawnWaves.Count; i++)
            {
                DrawSpawnWave(i);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawSpawnWave(int index)
    {
        var wave = currentLevel.spawnWaves[index];
        bool isSelected = selectedWaveIndex == index;
        
        EditorGUILayout.BeginVertical(isSelected ? EditorStyles.helpBox : EditorStyles.textArea);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Wave {index + 1}", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Select", GUILayout.Width(60)))
            selectedWaveIndex = isSelected ? -1 : index;
            
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            currentLevel.spawnWaves.RemoveAt(index);
            if (selectedWaveIndex == index) selectedWaveIndex = -1;
            EditorUtility.SetDirty(currentLevel);
            return;
        }
        
        EditorGUILayout.EndHorizontal();

        if (isSelected)
        {
            EditorGUI.BeginChangeCheck();
            
            wave.startTime = EditorGUILayout.FloatField("Start Time", wave.startTime);
            wave.duration = EditorGUILayout.FloatField("Duration", wave.duration);
            wave.customSpawnRate = EditorGUILayout.FloatField("Custom Spawn Rate", wave.customSpawnRate);
            wave.isBossWave = EditorGUILayout.Toggle("Boss Wave", wave.isBossWave);
            
            // Spawn lanes (simplified)
            EditorGUILayout.LabelField("Spawn Lanes:");
            string lanesString = string.Join(",", wave.spawnLanes);
            string newLanesString = EditorGUILayout.TextField(lanesString);
            
            if (newLanesString != lanesString)
            {
                try
                {
                    var lanes = new List<int>();
                    foreach (string s in newLanesString.Split(','))
                    {
                        if (int.TryParse(s.Trim(), out int lane))
                            lanes.Add(lane);
                    }
                    wave.spawnLanes = lanes.ToArray();
                }
                catch
                {
                    // Invalid format, keep old values
                }
            }
            
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(currentLevel);
        }
        else
        {
            EditorGUILayout.LabelField($"Time: {wave.startTime}s - {wave.startTime + wave.duration}s");
            EditorGUILayout.LabelField($"Lanes: {string.Join(",", wave.spawnLanes)}");
        }
        
        EditorGUILayout.EndVertical();
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
        
        if (GUILayout.Button("Test Level"))
            TestLevel();
            
        if (GUILayout.Button("Export Timeline"))
            ExportTimeline();
            
        EditorGUILayout.EndHorizontal();
    }

    void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Combat Level",
            "NewCombatLevel",
            "asset",
            "Choose location for new combat level");
            
        if (!string.IsNullOrEmpty(path))
        {
            CombatLevelData newLevel = CreateInstance<CombatLevelData>();
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            currentLevel = newLevel;
        }
    }

    void TestLevel()
    {
        if (currentLevel == null) return;
        
        // Find or create combat stage manager
        var combatManager = FindObjectOfType<CombatSystem.Management.CombatStageManager>();
        if (combatManager == null)
        {
            EditorUtility.DisplayDialog("Test Level", "No Combat Stage Manager found in scene. Please add one to test the level.", "OK");
            return;
        }
        
        // Apply level settings (you would need to extend CombatStageManager to accept level data)
        EditorUtility.DisplayDialog("Test Level", "Level testing would start here. Implement level loading in CombatStageManager.", "OK");
    }

    void ExportTimeline()
    {
        if (currentLevel == null) return;
        
        string timeline = $"Combat Level Timeline: {currentLevel.levelName}\n";
        timeline += $"Duration: {currentLevel.levelDuration}s\n\n";
        
        foreach (var wave in currentLevel.spawnWaves)
        {
            timeline += $"Wave: {wave.startTime}s - {wave.startTime + wave.duration}s\n";
            timeline += $"  Lanes: {string.Join(",", wave.spawnLanes)}\n";
            timeline += $"  Rate: {wave.customSpawnRate}\n";
            timeline += $"  Boss: {wave.isBossWave}\n\n";
        }
        
        EditorUtility.DisplayDialog("Timeline Export", timeline, "OK");
    }
}
#endif