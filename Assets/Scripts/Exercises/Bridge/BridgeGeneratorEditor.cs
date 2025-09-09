using UnityEngine;
using UnityEditor;
using VRRehab.Bridge;

#if UNITY_EDITOR


namespace VRRehab.Bridge.Editor
{
    /// <summary>
    /// Custom editor for BridgeConfigGenerator
    /// </summary>
    [CustomEditor(typeof(BridgeConfigGenerator))]
    public class BridgeGeneratorEditor : UnityEditor.Editor
    {
        private BridgeConfigGenerator generator;
        private BridgeConfigGenerator.DifficultyLevel selectedDifficulty = BridgeConfigGenerator.DifficultyLevel.Easy;
        private bool showGeneratedConfig = false;
        private BridgeConfig generatedConfig;

        void OnEnable()
        {
            generator = (BridgeConfigGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generator Tools", EditorStyles.boldLabel);

            // Difficulty selection
            selectedDifficulty = (BridgeConfigGenerator.DifficultyLevel)EditorGUILayout.EnumPopup("Difficulty", selectedDifficulty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Config"))
            {
                generatedConfig = generator.GenerateConfig(selectedDifficulty);
                showGeneratedConfig = true;
            }

            if (GUILayout.Button("Generate Random"))
            {
                generatedConfig = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Random);
                showGeneratedConfig = true;
                selectedDifficulty = BridgeConfigGenerator.DifficultyLevel.Random;
            }
            EditorGUILayout.EndHorizontal();

            // Show generated config
            if (showGeneratedConfig && generatedConfig != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Generated Configuration", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Plank Count", generatedConfig.plankCount);
                EditorGUILayout.FloatField("Bridge Length", generatedConfig.bridgeLength);
                EditorGUILayout.FloatField("Plank Width", generatedConfig.plankWidth);
                EditorGUILayout.FloatField("Plank Gap", generatedConfig.plankGap);
                EditorGUILayout.FloatField("Joint Spring", generatedConfig.jointSpring);
                EditorGUILayout.FloatField("Joint Damper", generatedConfig.jointDamper);
                EditorGUILayout.Toggle("Enable Platforms", generatedConfig.enablePlatforms);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Save as Asset"))
                {
                    SaveConfigAsAsset(generatedConfig);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Generation", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Progressive Set (5 levels)"))
            {
                GenerateProgressiveSet();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Tests", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Tutorial"))
            {
                generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Tutorial);
            }

            if (GUILayout.Button("Test Easy"))
            {
                generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Easy);
            }

            if (GUILayout.Button("Test Medium"))
            {
                generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);
            }

            if (GUILayout.Button("Test Hard"))
            {
                generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Hard);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Test Extreme"))
            {
                generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Extreme);
            }
        }

        private void SaveConfigAsAsset(BridgeConfig config)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Bridge Config",
                $"{selectedDifficulty}BridgeConfig",
                "asset",
                "Save bridge configuration as asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Bridge configuration saved as:\n{path}", "OK");
            }
        }

        private void GenerateProgressiveSet()
        {
            var configs = generator.GenerateProgressiveConfigs(5);

            // Create a folder for the set
            string folderPath = $"Assets/BridgeConfigs/ProgressiveSet_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] folders = folderPath.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath += "/" + folders[i];
                }
            }

            // Save each config
            for (int i = 0; i < configs.Count; i++)
            {
                string assetPath = $"{folderPath}/Level{i + 1}_{configs[i].plankCount}planks.asset";
                AssetDatabase.CreateAsset(configs[i], assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Progressive bridge config set saved to:\n{folderPath}", "OK");
        }
    }

    /// <summary>
    /// Editor window for advanced bridge configuration management
    /// </summary>
    public class BridgeConfigManagerWindow : EditorWindow
    {
        private BridgeConfigGenerator generator;
        private Vector2 scrollPos;
        private BridgeConfigGenerator.DifficultyLevel previewDifficulty = BridgeConfigGenerator.DifficultyLevel.Medium;
        private BridgeConfig previewConfig;

        [MenuItem("VR Rehab/Bridge Config Manager")]
        static void Init()
        {
            BridgeConfigManagerWindow window = (BridgeConfigManagerWindow)GetWindow(typeof(BridgeConfigManagerWindow));
            window.titleContent = new GUIContent("Bridge Config Manager");
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Bridge Configuration Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Generator selection
            generator = (BridgeConfigGenerator)EditorGUILayout.ObjectField("Generator", generator, typeof(BridgeConfigGenerator), true);

            if (generator == null)
            {
                EditorGUILayout.HelpBox("Please assign a BridgeConfigGenerator", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Preview section
            EditorGUILayout.LabelField("Configuration Preview", EditorStyles.boldLabel);
            previewDifficulty = (BridgeConfigGenerator.DifficultyLevel)EditorGUILayout.EnumPopup("Difficulty", previewDifficulty);

            if (GUILayout.Button("Generate Preview"))
            {
                previewConfig = generator.GenerateConfig(previewDifficulty);
            }

            if (previewConfig != null)
            {
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.LabelField("Preview Results:", EditorStyles.boldLabel);
                EditorGUILayout.IntField("Plank Count", previewConfig.plankCount);
                EditorGUILayout.FloatField("Bridge Length", previewConfig.bridgeLength);
                EditorGUILayout.FloatField("Plank Width", previewConfig.plankWidth);
                EditorGUILayout.FloatField("Plank Gap", previewConfig.plankGap);
                EditorGUILayout.FloatField("Joint Spring", previewConfig.jointSpring);
                EditorGUILayout.FloatField("Joint Damper", previewConfig.jointDamper);
                EditorGUILayout.FloatField("Plank Mass", previewConfig.plankMass);
                EditorGUILayout.Toggle("Platforms Enabled", previewConfig.enablePlatforms);

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All Difficulties"))
            {
                GenerateAllDifficulties();
            }

            if (GUILayout.Button("Generate Tutorial Set"))
            {
                GenerateTutorialSet();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Adaptive Configs"))
            {
                GenerateAdaptiveConfigs();
            }

            EditorGUILayout.EndScrollView();
        }

        private void GenerateAllDifficulties()
        {
            string basePath = $"Assets/GeneratedBridgeConfigs/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(basePath);

            foreach (BridgeConfigGenerator.DifficultyLevel difficulty in System.Enum.GetValues(typeof(BridgeConfigGenerator.DifficultyLevel)))
            {
                // Process all available difficulty levels

                BridgeConfig config = generator.GenerateConfig(difficulty);
                string path = $"{basePath}/{difficulty}.asset";
                AssetDatabase.CreateAsset(config, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"All difficulty configs saved to:\n{basePath}", "OK");
        }

        private void GenerateTutorialSet()
        {
            string path = $"Assets/TutorialBridgeConfigs/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(path);

            for (int i = 0; i < 3; i++)
            {
                BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Tutorial);
                string assetPath = $"{path}/Tutorial_Level{i + 1}.asset";
                AssetDatabase.CreateAsset(config, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Tutorial set saved to:\n{path}", "OK");
        }

        private void GenerateAdaptiveConfigs()
        {
            string path = $"Assets/AdaptiveBridgeConfigs/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(path);

            // Generate configs for different performance levels using existing difficulty levels
            float[] performanceLevels = { 0.3f, 0.5f, 0.7f, 0.9f };
            VRRehab.Bridge.BridgeConfigGenerator.DifficultyLevel[] difficulties = {
                VRRehab.Bridge.BridgeConfigGenerator.DifficultyLevel.Easy,
                VRRehab.Bridge.BridgeConfigGenerator.DifficultyLevel.Medium,
                VRRehab.Bridge.BridgeConfigGenerator.DifficultyLevel.Hard,
                VRRehab.Bridge.BridgeConfigGenerator.DifficultyLevel.Extreme
            };

            for (int i = 0; i < performanceLevels.Length && i < difficulties.Length; i++)
            {
                BridgeConfig config = generator.GenerateConfig(difficulties[i]);
                string assetPath = $"{path}/Adaptive_{performanceLevels[i]:F1}perf_{difficulties[i]}.asset";
                AssetDatabase.CreateAsset(config, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Adaptive configs saved to:\n{path}", "OK");
        }

        private void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = "Assets";

                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
    }
}
#endif