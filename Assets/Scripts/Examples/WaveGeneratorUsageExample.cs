using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CombatSystem.Spawning;

namespace CombatSystem.Examples
{
    /// <summary>
    /// Example showing how to use the WaveGenerator
    /// Attach this to a GameObject to test wave generation
    /// </summary>
    public class WaveGeneratorUsageExample : MonoBehaviour
    {
        [Header("Generator Reference")]
        [SerializeField] private WaveGenerator generator;

        [Header("Spawner Reference")]
        [SerializeField] private DroneSpawner droneSpawner;

        [Header("Wave Manager")]
        [SerializeField] private WaveManager waveManager;

        void Awake()
        {
            if (generator == null)
            {
                generator = GetComponent<WaveGenerator>() ??
                           gameObject.AddComponent<WaveGenerator>();
            }

            if (droneSpawner == null)
            {
                droneSpawner = FindObjectOfType<DroneSpawner>();
            }

            if (waveManager == null)
            {
                waveManager = FindObjectOfType<WaveManager>();
            }
        }

        // ===== BASIC USAGE EXAMPLES =====

        /// <summary>
        /// Example 1: Generate basic wave set
        /// </summary>
        public void GenerateEasyWaves()
        {
            List<WaveConfiguration> waves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Easy, 5);

            for (int i = 0; i < waves.Count; i++)
            {
                WaveConfiguration wave = waves[i];
                Debug.Log($"Wave {i + 1}: {wave.maxSimultaneousDrones} drones, {wave.waveDuration}s duration");
            }
        }

        /// <summary>
        /// Example 2: Generate and apply waves to spawner
        /// </summary>
        public void GenerateAndApplyWaves()
        {
            if (waveManager == null)
            {
                Debug.LogError("No wave manager found!");
                return;
            }

            // Generate medium difficulty waves
            waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Medium);
            Debug.Log("Applied generated waves to wave manager");
        }

        /// <summary>
        /// Example 3: Generate adaptive waves based on performance
        /// </summary>
        public void GenerateAdaptiveWaves(float averageScore, int previousWaves)
        {
            List<WaveConfiguration> waves = generator.GenerateAdaptiveWaves(averageScore, previousWaves);

            Debug.Log($"Adaptive Waves ({waves.Count} total):");
            for (int i = 0; i < waves.Count; i++)
            {
                WaveConfiguration wave = waves[i];
                Debug.Log($"  Wave {i + 1}: {wave.maxSimultaneousDrones} drones, Speed: {wave.droneSpeedMultiplier:F1}x");
            }
        }

        /// <summary>
        /// Example 4: Generate single wave with specific parameters
        /// </summary>
        public void GenerateSingleWave()
        {
            WaveConfiguration wave = generator.GenerateSingleWave(1, WaveGenerator.DifficultyLevel.Hard);
            Debug.Log($"Single Hard Wave: {wave.maxSimultaneousDrones} drones, {wave.spawnInterval}s interval");
        }

        // ===== ADVANCED USAGE EXAMPLES =====

        /// <summary>
        /// Example 5: Create a rehabilitation progression session
        /// </summary>
        public void StartRehabProgression()
        {
            StartCoroutine(RehabProgressionSession());
        }

        private IEnumerator RehabProgressionSession()
        {
            if (waveManager == null) yield break;

            // Phase 1: Easy waves for warm-up
            Debug.Log("Starting warm-up phase...");
            waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Easy);
            yield return new WaitForSeconds(60f); // 1 minute warm-up

            // Phase 2: Medium waves for main exercise
            Debug.Log("Starting main exercise phase...");
            waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Medium);
            yield return new WaitForSeconds(120f); // 2 minutes main exercise

            // Phase 3: Hard waves for challenge
            Debug.Log("Starting challenge phase...");
            waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Hard);
            yield return new WaitForSeconds(60f); // 1 minute challenge

            Debug.Log("Rehabilitation session completed!");
        }

        /// <summary>
        /// Example 6: Generate waves for different therapeutic goals
        /// </summary>
        public void GenerateTherapeuticWaveSets()
        {
            // Reflex training - fast, frequent spawns
            List<WaveConfiguration> reflexWaves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Easy, 3);
            foreach (var wave in reflexWaves)
            {
                wave.spawnInterval = 1.0f; // Very frequent spawns
                wave.telegraphDurationMultiplier = 0.7f; // Shorter telegraph time
            }
            Debug.Log("Reflex training waves generated");

            // Endurance training - longer sessions with consistent difficulty
            List<WaveConfiguration> enduranceWaves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 8);
            foreach (var wave in enduranceWaves)
            {
                wave.waveDuration = 45f; // Longer waves
                wave.restDuration = 15f; // Shorter rests
            }
            Debug.Log("Endurance training waves generated");

            // Precision training - slower, more predictable patterns
            List<WaveConfiguration> precisionWaves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Hard, 4);
            foreach (var wave in precisionWaves)
            {
                wave.maxSimultaneousDrones = 2; // Fewer drones for precision focus
                wave.telegraphDurationMultiplier = 1.5f; // More time to react
                wave.droneSpeedMultiplier = 0.8f; // Slower drones
            }
            Debug.Log("Precision training waves generated");
        }

        /// <summary>
        /// Example 7: Real-time difficulty adjustment
        /// </summary>
        public void DemonstrateAdaptiveDifficulty()
        {
            // Simulate different performance levels
            float[] performanceLevels = { 0.3f, 0.6f, 0.8f, 1.2f };
            int[] waveCounts = { 2, 4, 6, 8 };

            for (int i = 0; i < performanceLevels.Length; i++)
            {
                List<WaveConfiguration> waves = generator.GenerateAdaptiveWaves(performanceLevels[i], waveCounts[i]);
                string difficulty = performanceLevels[i] < 0.5f ? "Easy" :
                                  performanceLevels[i] < 0.9f ? "Medium" : "Hard";

                Debug.Log($"Performance {performanceLevels[i]:F1} -> {difficulty} waves ({waves.Count} total)");
            }
        }

        /// <summary>
        /// Example 8: Custom wave configuration for special needs
        /// </summary>
        public void CreateCustomWaveConfig()
        {
            // Create a custom wave configuration
            WaveConfiguration customWave = new WaveConfiguration
            {
                waveNumber = 1,
                waveDuration = 30f,
                restDuration = 10f,
                maxSimultaneousDrones = 1, // Single drone for beginners
                spawnInterval = 3f, // Slow spawn rate
                spawnVariation = 0.2f,
                scoutProbability = 0.9f, // Mostly easy scouts
                heavyProbability = 0.1f,
                droneSpeedMultiplier = 0.7f, // Slow movement
                attackCooldownMultiplier = 1.5f, // Longer cooldowns
                telegraphDurationMultiplier = 1.8f, // More reaction time
                dashSpeedMultiplier = 0.8f
            };

            Debug.Log("Custom wave configuration created for accessibility");
        }

        // ===== EDITOR BUTTONS =====
        [ContextMenu("Test Easy Waves")]
        private void TestEasyWaves() => GenerateEasyWaves();

        [ContextMenu("Test Medium Waves")]
        private void TestMediumWaves()
        {
            List<WaveConfiguration> waves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 5);
            Debug.Log($"Medium Waves: {waves.Count} waves generated");
        }

        [ContextMenu("Test Hard Waves")]
        private void TestHardWaves()
        {
            List<WaveConfiguration> waves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Hard, 5);
            Debug.Log($"Hard Waves: {waves.Count} waves generated");
        }

        [ContextMenu("Test Extreme Waves")]
        private void TestExtremeWaves()
        {
            List<WaveConfiguration> waves = generator.GenerateWaves(WaveGenerator.DifficultyLevel.Extreme, 3);
            Debug.Log($"Extreme Waves: {waves.Count} waves generated");
        }

        [ContextMenu("Test Adaptive Waves")]
        private void TestAdaptiveWaves() => GenerateAdaptiveWaves(0.75f, 3);

        [ContextMenu("Test Rehab Progression")]
        private void TestRehabProgression() => StartRehabProgression();

        [ContextMenu("Test Therapeutic Sets")]
        private void TestTherapeuticSets() => GenerateTherapeuticWaveSets();
    }
}
