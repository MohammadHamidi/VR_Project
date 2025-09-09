using UnityEngine;
using UnityEditor;
using CombatSystem.Spawning;

#if UNITY_EDITOR

namespace CombatSystem.Spawning.Editor
{
    /// <summary>
    /// Custom editor for WaveGenerator
    /// </summary>
    [CustomEditor(typeof(WaveGenerator))]
    public class WaveGeneratorEditor : UnityEditor.Editor
    {
        private WaveGenerator generator;
        private WaveGenerator.DifficultyLevel selectedDifficulty = WaveGenerator.DifficultyLevel.Easy;
        private int waveCount = 5;
        private bool showGeneratedWaves = false;
        private System.Collections.Generic.List<WaveConfiguration> generatedWaves;

        void OnEnable()
        {
            generator = (WaveGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generator Tools", EditorStyles.boldLabel);

            // Difficulty and count selection
            selectedDifficulty = (WaveGenerator.DifficultyLevel)EditorGUILayout.EnumPopup("Difficulty", selectedDifficulty);
            waveCount = EditorGUILayout.IntSlider("Wave Count", waveCount, 1, 10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Waves"))
            {
                generatedWaves = generator.GenerateWaves(selectedDifficulty, waveCount);
                showGeneratedWaves = true;
            }

            if (GUILayout.Button("Generate Adaptive"))
            {
                float testScore = EditorGUILayout.FloatField("Test Score", 0.75f, GUILayout.Width(200));
                int testWaves = EditorGUILayout.IntField("Previous Waves", 3, GUILayout.Width(200));
                generatedWaves = generator.GenerateAdaptiveWaves(testScore, testWaves);
                showGeneratedWaves = true;
                selectedDifficulty = WaveGenerator.DifficultyLevel.Adaptive;
            }
            EditorGUILayout.EndHorizontal();

            // Show generated waves
            if (showGeneratedWaves && generatedWaves != null && generatedWaves.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Generated {generatedWaves.Count} Waves ({selectedDifficulty})", EditorStyles.boldLabel);

                for (int i = 0; i < generatedWaves.Count; i++)
                {
                    WaveConfiguration wave = generatedWaves[i];

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"Wave {wave.waveNumber}", EditorStyles.boldLabel);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntField("Max Drones", wave.maxSimultaneousDrones);
                    EditorGUILayout.FloatField("Spawn Interval", wave.spawnInterval);
                    EditorGUILayout.FloatField("Wave Duration", wave.waveDuration);
                    EditorGUILayout.FloatField("Rest Duration", wave.restDuration);
                    EditorGUILayout.LabelField($"Drone Composition: {wave.scoutProbability * 100:F0}% Scouts, {(wave.heavyProbability) * 100:F0}% Heavies");
                    EditorGUILayout.LabelField($"Modifiers - Speed: {wave.droneSpeedMultiplier:F1}x, Attack: {wave.attackCooldownMultiplier:F1}x");
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();
                }

                if (GUILayout.Button("Save Wave Set as Asset"))
                {
                    SaveWaveSetAsAsset(generatedWaves);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Tests", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Tutorial"))
            {
                generator.GenerateWaves(WaveGenerator.DifficultyLevel.Tutorial, 3);
            }

            if (GUILayout.Button("Test Easy"))
            {
                generator.GenerateWaves(WaveGenerator.DifficultyLevel.Easy, 5);
            }

            if (GUILayout.Button("Test Medium"))
            {
                generator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 5);
            }

            if (GUILayout.Button("Test Hard"))
            {
                generator.GenerateWaves(WaveGenerator.DifficultyLevel.Hard, 5);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Test Extreme"))
            {
                generator.GenerateWaves(WaveGenerator.DifficultyLevel.Extreme, 5);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Performance Tests", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Low Performance (0.4)"))
            {
                generator.GenerateAdaptiveWaves(0.4f, 2);
            }

            if (GUILayout.Button("Test High Performance (1.2)"))
            {
                generator.GenerateAdaptiveWaves(1.2f, 4);
            }
        }

        private void SaveWaveSetAsAsset(System.Collections.Generic.List<WaveConfiguration> waves)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Wave Set",
                $"{selectedDifficulty}WaveSet",
                "asset",
                "Save wave configuration set as asset");

            if (!string.IsNullOrEmpty(path))
            {
                // Create a container scriptable object to hold the wave set
                WaveSetAsset waveSet = ScriptableObject.CreateInstance<WaveSetAsset>();
                waveSet.waves = waves;
                waveSet.difficulty = selectedDifficulty;
                waveSet.generationTime = System.DateTime.Now;

                AssetDatabase.CreateAsset(waveSet, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Wave set saved as:\n{path}", "OK");
            }
        }
    }

    /// <summary>
    /// Container for wave sets that can be saved as assets
    /// </summary>
    public class WaveSetAsset : ScriptableObject
    {
        public WaveGenerator.DifficultyLevel difficulty;
        public System.DateTime generationTime;
        public System.Collections.Generic.List<WaveConfiguration> waves = new System.Collections.Generic.List<WaveConfiguration>();
    }

    /// <summary>
    /// Editor window for advanced wave configuration management
    /// </summary>
    public class WaveConfigManagerWindow : EditorWindow
    {
        private WaveGenerator generator;
        private Vector2 scrollPos;
        private WaveGenerator.DifficultyLevel previewDifficulty = WaveGenerator.DifficultyLevel.Medium;
        private int previewWaveCount = 5;
        private System.Collections.Generic.List<WaveConfiguration> previewWaves;

        [MenuItem("VR Rehab/Wave Config Manager")]
        static void Init()
        {
            WaveConfigManagerWindow window = (WaveConfigManagerWindow)GetWindow(typeof(WaveConfigManagerWindow));
            window.titleContent = new GUIContent("Wave Config Manager");
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Wave Configuration Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Generator selection
            generator = (WaveGenerator)EditorGUILayout.ObjectField("Generator", generator, typeof(WaveGenerator), true);

            if (generator == null)
            {
                EditorGUILayout.HelpBox("Please assign a WaveGenerator", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Preview section
            EditorGUILayout.LabelField("Wave Set Preview", EditorStyles.boldLabel);
            previewDifficulty = (WaveGenerator.DifficultyLevel)EditorGUILayout.EnumPopup("Difficulty", previewDifficulty);
            previewWaveCount = EditorGUILayout.IntSlider("Wave Count", previewWaveCount, 1, 15);

            if (GUILayout.Button("Generate Preview"))
            {
                previewWaves = generator.GenerateWaves(previewDifficulty, previewWaveCount);
            }

            if (previewWaves != null && previewWaves.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Preview Results ({previewWaves.Count} waves):", EditorStyles.boldLabel);

                // Summary statistics
                int totalDrones = 0;
                float totalDuration = 0f;
                float avgSpawnInterval = 0f;

                foreach (var wave in previewWaves)
                {
                    totalDrones += wave.maxSimultaneousDrones;
                    totalDuration += wave.waveDuration + wave.restDuration;
                    avgSpawnInterval += wave.spawnInterval;
                }

                avgSpawnInterval /= previewWaves.Count;

                EditorGUILayout.LabelField($"Total Duration: {totalDuration:F1}s");
                EditorGUILayout.LabelField($"Average Max Drones: {totalDrones / previewWaves.Count:F1}");
                EditorGUILayout.LabelField($"Average Spawn Interval: {avgSpawnInterval:F1}s");

                EditorGUILayout.Space();

                // Detailed wave breakdown
                for (int i = 0; i < Mathf.Min(previewWaves.Count, 5); i++) // Show first 5 waves
                {
                    WaveConfiguration wave = previewWaves[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"Wave {wave.waveNumber}:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField($"  Drones: {wave.maxSimultaneousDrones}, Interval: {wave.spawnInterval:F1}s");
                    EditorGUILayout.LabelField($"  Duration: {wave.waveDuration:F0}s, Scouts: {wave.scoutProbability * 100:F0}%");
                    EditorGUILayout.EndVertical();
                }

                if (previewWaves.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {previewWaves.Count - 5} more waves");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All Difficulties"))
            {
                GenerateAllDifficulties();
            }

            if (GUILayout.Button("Generate Progression Set"))
            {
                GenerateProgressionSet();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Adaptive Sets"))
            {
                GenerateAdaptiveSets();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Analysis Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Analyze Difficulty Scaling"))
            {
                AnalyzeDifficultyScaling();
            }

            if (GUILayout.Button("Generate Performance Report"))
            {
                GeneratePerformanceReport();
            }

            EditorGUILayout.EndScrollView();
        }

        private void GenerateAllDifficulties()
        {
            string basePath = $"Assets/GeneratedWaveSets/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(basePath);

            foreach (WaveGenerator.DifficultyLevel difficulty in System.Enum.GetValues(typeof(WaveGenerator.DifficultyLevel)))
            {
                if (difficulty == WaveGenerator.DifficultyLevel.Adaptive) continue;

                var waves = generator.GenerateWaves(difficulty, 5);
                WaveSetAsset waveSet = ScriptableObject.CreateInstance<WaveSetAsset>();
                waveSet.waves = waves;
                waveSet.difficulty = difficulty;
                waveSet.generationTime = System.DateTime.Now;

                string path = $"{basePath}/{difficulty}.asset";
                AssetDatabase.CreateAsset(waveSet, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"All difficulty wave sets saved to:\n{basePath}", "OK");
        }

        private void GenerateProgressionSet()
        {
            string path = $"Assets/ProgressionWaveSets/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(path);

            // Generate progressive sets for different starting difficulties
            WaveGenerator.DifficultyLevel[] difficulties = {
                WaveGenerator.DifficultyLevel.Easy,
                WaveGenerator.DifficultyLevel.Medium,
                WaveGenerator.DifficultyLevel.Hard
            };

            foreach (var difficulty in difficulties)
            {
                var waves = generator.GenerateWaves(difficulty, 8); // Longer progression
                WaveSetAsset waveSet = ScriptableObject.CreateInstance<WaveSetAsset>();
                waveSet.waves = waves;
                waveSet.difficulty = difficulty;
                waveSet.generationTime = System.DateTime.Now;

                string assetPath = $"{path}/Progression_{difficulty}.asset";
                AssetDatabase.CreateAsset(waveSet, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Progression sets saved to:\n{path}", "OK");
        }

        private void GenerateAdaptiveSets()
        {
            string path = $"Assets/AdaptiveWaveSets/{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
            EnsureFolderExists(path);

            // Generate adaptive sets for different performance scenarios
            float[] performanceLevels = { 0.4f, 0.6f, 0.8f, 1.0f, 1.2f };
            int[] waveCounts = { 3, 4, 5, 6, 7 };

            for (int i = 0; i < performanceLevels.Length; i++)
            {
                var waves = generator.GenerateAdaptiveWaves(performanceLevels[i], waveCounts[i]);
                WaveSetAsset waveSet = ScriptableObject.CreateInstance<WaveSetAsset>();
                waveSet.waves = waves;
                waveSet.difficulty = WaveGenerator.DifficultyLevel.Adaptive;
                waveSet.generationTime = System.DateTime.Now;

                string assetPath = $"{path}/Adaptive_{performanceLevels[i]:F1}perf.asset";
                AssetDatabase.CreateAsset(waveSet, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Adaptive wave sets saved to:\n{path}", "OK");
        }

        private void AnalyzeDifficultyScaling()
        {
            string report = "Difficulty Scaling Analysis\n\n";

            foreach (WaveGenerator.DifficultyLevel difficulty in System.Enum.GetValues(typeof(WaveGenerator.DifficultyLevel)))
            {
                if (difficulty == WaveGenerator.DifficultyLevel.Adaptive) continue;

                var waves = generator.GenerateWaves(difficulty, 5);

                float avgDrones = 0;
                float avgInterval = 0;
                float avgSpeed = 0;

                foreach (var wave in waves)
                {
                    avgDrones += wave.maxSimultaneousDrones;
                    avgInterval += wave.spawnInterval;
                    avgSpeed += wave.droneSpeedMultiplier;
                }

                avgDrones /= waves.Count;
                avgInterval /= waves.Count;
                avgSpeed /= waves.Count;

                report += $"{difficulty}:\n";
                report += $"  Avg Drones: {avgDrones:F1}\n";
                report += $"  Avg Interval: {avgInterval:F1}s\n";
                report += $"  Avg Speed: {avgSpeed:F1}x\n\n";
            }

            EditorUtility.DisplayDialog("Difficulty Analysis", report, "OK");
        }

        private void GeneratePerformanceReport()
        {
            string report = "Performance Analysis Report\n\n";

            // Test different configurations
            var easyWaves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Easy, 5);
            var hardWaves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Hard, 5);
            var adaptiveWaves = generator.GenerateAdaptiveWaves(0.8f, 3);

            report += $"Easy Difficulty ({easyWaves.Count} waves):\n";
            report += $"  Total Duration: {CalculateTotalDuration(easyWaves):F1}s\n";
            report += $"  Difficulty Progression: {CalculateProgressionFactor(easyWaves):F2}\n\n";

            report += $"Hard Difficulty ({hardWaves.Count} waves):\n";
            report += $"  Total Duration: {CalculateTotalDuration(hardWaves):F1}s\n";
            report += $"  Difficulty Progression: {CalculateProgressionFactor(hardWaves):F2}\n\n";

            report += $"Adaptive ({adaptiveWaves.Count} waves):\n";
            report += $"  Total Duration: {CalculateTotalDuration(adaptiveWaves):F1}s\n";
            report += $"  Difficulty Progression: {CalculateProgressionFactor(adaptiveWaves):F2}\n";

            EditorUtility.DisplayDialog("Performance Report", report, "OK");
        }

        private float CalculateTotalDuration(System.Collections.Generic.List<WaveConfiguration> waves)
        {
            float total = 0f;
            foreach (var wave in waves)
            {
                total += wave.waveDuration + wave.restDuration;
            }
            return total;
        }

        private float CalculateProgressionFactor(System.Collections.Generic.List<WaveConfiguration> waves)
        {
            if (waves.Count < 2) return 1f;

            WaveConfiguration first = waves[0];
            WaveConfiguration last = waves[waves.Count - 1];

            float firstDifficulty = first.maxSimultaneousDrones / first.spawnInterval * first.droneSpeedMultiplier;
            float lastDifficulty = last.maxSimultaneousDrones / last.spawnInterval * last.droneSpeedMultiplier;

            return lastDifficulty / firstDifficulty;
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
