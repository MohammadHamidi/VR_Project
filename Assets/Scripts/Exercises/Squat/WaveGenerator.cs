using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CombatSystem.Spawning
{
    /// <summary>
    /// Generator for wave configurations with difficulty-based scaling and progression
    /// </summary>
    public class WaveGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class DifficultyParameters
        {
            [Header("Spawn Settings")]
            public int minSimultaneousDrones = 2;
            public int maxSimultaneousDrones = 4;
            public float minSpawnInterval = 1.5f;
            public float maxSpawnInterval = 3f;
            public float spawnVariation = 0.5f;

            [Header("Wave Timing")]
            public float minWaveDuration = 20f;
            public float maxWaveDuration = 35f;
            public float minRestDuration = 8f;
            public float maxRestDuration = 15f;

            [Header("Drone Composition")]
            [Range(0f, 1f)] public float scoutProbability = 0.8f;
            [Range(0f, 1f)] public float heavyProbability = 0.2f;

            [Header("Difficulty Modifiers")]
            public float speedMultiplier = 1f;
            public float attackCooldownMultiplier = 1f;
            public float telegraphDurationMultiplier = 1f;
            public float dashSpeedMultiplier = 1f;
        }

        [Header("Difficulty Configurations")]
        [SerializeField] private DifficultyParameters easyParams;
        [SerializeField] private DifficultyParameters mediumParams;
        [SerializeField] private DifficultyParameters hardParams;

        [Header("Generation Settings")]
        [SerializeField] private bool useProgression = true;
        [SerializeField] private float progressionRate = 0.1f; // How much harder each wave gets
        [SerializeField] private int maxWavesPerSession = 10;
        [SerializeField] private bool randomizeWithinDifficulty = true;
        [SerializeField] private float randomizationVariance = 0.15f; // ±15% variance

        // Events
        public static event Action<List<WaveConfiguration>, DifficultyLevel> OnWavesGenerated;

        public enum DifficultyLevel
        {
            Tutorial,
            Easy,
            Medium,
            Hard,
            Extreme,
            Adaptive
        }

        void Awake()
        {
            InitializeDefaultParameters();
        }

        private void InitializeDefaultParameters()
        {
            if (easyParams == null)
            {
                easyParams = new DifficultyParameters
                {
                    minSimultaneousDrones = 1,
                    maxSimultaneousDrones = 3,
                    minSpawnInterval = 2.5f,
                    maxSpawnInterval = 4f,
                    spawnVariation = 0.3f,
                    minWaveDuration = 25f,
                    maxWaveDuration = 35f,
                    minRestDuration = 10f,
                    maxRestDuration = 15f,
                    scoutProbability = 0.9f,
                    heavyProbability = 0.1f,
                    speedMultiplier = 0.8f,
                    attackCooldownMultiplier = 1.2f,
                    telegraphDurationMultiplier = 1.3f,
                    dashSpeedMultiplier = 0.9f
                };
            }

            if (mediumParams == null)
            {
                mediumParams = new DifficultyParameters
                {
                    minSimultaneousDrones = 2,
                    maxSimultaneousDrones = 4,
                    minSpawnInterval = 2f,
                    maxSpawnInterval = 3.5f,
                    spawnVariation = 0.4f,
                    minWaveDuration = 20f,
                    maxWaveDuration = 30f,
                    minRestDuration = 8f,
                    maxRestDuration = 12f,
                    scoutProbability = 0.8f,
                    heavyProbability = 0.2f,
                    speedMultiplier = 1f,
                    attackCooldownMultiplier = 1f,
                    telegraphDurationMultiplier = 1f,
                    dashSpeedMultiplier = 1f
                };
            }

            if (hardParams == null)
            {
                hardParams = new DifficultyParameters
                {
                    minSimultaneousDrones = 3,
                    maxSimultaneousDrones = 6,
                    minSpawnInterval = 1.5f,
                    maxSpawnInterval = 2.5f,
                    spawnVariation = 0.5f,
                    minWaveDuration = 15f,
                    maxWaveDuration = 25f,
                    minRestDuration = 6f,
                    maxRestDuration = 10f,
                    scoutProbability = 0.6f,
                    heavyProbability = 0.4f,
                    speedMultiplier = 1.3f,
                    attackCooldownMultiplier = 0.8f,
                    telegraphDurationMultiplier = 0.8f,
                    dashSpeedMultiplier = 1.2f
                };
            }
        }

        /// <summary>
        /// Generates a list of wave configurations based on difficulty level
        /// </summary>
        public List<WaveConfiguration> GenerateWaves(DifficultyLevel difficulty, int waveCount = 5)
        {
            List<WaveConfiguration> waves = new List<WaveConfiguration>();
            DifficultyParameters baseParams = GetDifficultyParameters(difficulty);

            for (int i = 0; i < waveCount; i++)
            {
                WaveConfiguration wave = CreateWaveConfiguration(i + 1, baseParams);

                if (useProgression && i > 0)
                {
                    ApplyProgressionScaling(wave, i);
                }

                waves.Add(wave);
            }

            OnWavesGenerated?.Invoke(waves, difficulty);
            return waves;
        }

        /// <summary>
        /// Generates waves adaptively based on player performance
        /// </summary>
        public List<WaveConfiguration> GenerateAdaptiveWaves(float averageScore, int completedWaves)
        {
            DifficultyLevel difficulty = DetermineAdaptiveDifficulty(averageScore, completedWaves);
            int waveCount = Mathf.Clamp(completedWaves + 3, 3, maxWavesPerSession);

            return GenerateWaves(difficulty, waveCount);
        }

        /// <summary>
        /// Generates a single wave configuration with specific parameters
        /// </summary>
        public WaveConfiguration GenerateSingleWave(int waveNumber, DifficultyLevel difficulty)
        {
            DifficultyParameters parameters = GetDifficultyParameters(difficulty);
            WaveConfiguration wave = CreateWaveConfiguration(waveNumber, parameters);

            if (useProgression && waveNumber > 1)
            {
                ApplyProgressionScaling(wave, waveNumber - 1);
            }

            return wave;
        }

        private DifficultyParameters GetDifficultyParameters(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Tutorial:
                    return new DifficultyParameters
                    {
                        minSimultaneousDrones = 1,
                        maxSimultaneousDrones = 2,
                        minSpawnInterval = 3f,
                        maxSpawnInterval = 5f,
                        spawnVariation = 0.2f,
                        minWaveDuration = 30f,
                        maxWaveDuration = 40f,
                        minRestDuration = 12f,
                        maxRestDuration = 18f,
                        scoutProbability = 1f,
                        heavyProbability = 0f,
                        speedMultiplier = 0.6f,
                        attackCooldownMultiplier = 1.5f,
                        telegraphDurationMultiplier = 1.5f,
                        dashSpeedMultiplier = 0.7f
                    };

                case DifficultyLevel.Easy:
                    return easyParams;

                case DifficultyLevel.Medium:
                    return mediumParams;

                case DifficultyLevel.Hard:
                    return hardParams;

                case DifficultyLevel.Extreme:
                    return new DifficultyParameters
                    {
                        minSimultaneousDrones = 4,
                        maxSimultaneousDrones = 8,
                        minSpawnInterval = 1f,
                        maxSpawnInterval = 2f,
                        spawnVariation = 0.6f,
                        minWaveDuration = 12f,
                        maxWaveDuration = 20f,
                        minRestDuration = 4f,
                        maxRestDuration = 8f,
                        scoutProbability = 0.4f,
                        heavyProbability = 0.6f,
                        speedMultiplier = 1.5f,
                        attackCooldownMultiplier = 0.6f,
                        telegraphDurationMultiplier = 0.6f,
                        dashSpeedMultiplier = 1.4f
                    };

                default:
                    return mediumParams;
            }
        }

        private WaveConfiguration CreateWaveConfiguration(int waveNumber, DifficultyParameters parameters)
        {
            WaveConfiguration wave = new WaveConfiguration
            {
                waveNumber = waveNumber,
                maxSimultaneousDrones = Random.Range(parameters.minSimultaneousDrones, parameters.maxSimultaneousDrones + 1),
                spawnInterval = Random.Range(parameters.minSpawnInterval, parameters.maxSpawnInterval),
                spawnVariation = parameters.spawnVariation,
                waveDuration = Random.Range(parameters.minWaveDuration, parameters.maxWaveDuration),
                restDuration = Random.Range(parameters.minRestDuration, parameters.maxRestDuration),
                scoutProbability = parameters.scoutProbability,
                heavyProbability = parameters.heavyProbability,
                droneSpeedMultiplier = parameters.speedMultiplier,
                attackCooldownMultiplier = parameters.attackCooldownMultiplier,
                telegraphDurationMultiplier = parameters.telegraphDurationMultiplier,
                dashSpeedMultiplier = parameters.dashSpeedMultiplier
            };

            if (randomizeWithinDifficulty)
            {
                ApplyRandomization(wave, parameters);
            }

            return wave;
        }

        private void ApplyProgressionScaling(WaveConfiguration wave, int waveIndex)
        {
            float progressionMultiplier = 1f + (waveIndex * progressionRate);

            wave.maxSimultaneousDrones = Mathf.RoundToInt(wave.maxSimultaneousDrones * progressionMultiplier);
            wave.spawnInterval = Mathf.Max(0.5f, wave.spawnInterval * (1f / progressionMultiplier));
            wave.droneSpeedMultiplier *= progressionMultiplier;
            wave.dashSpeedMultiplier *= progressionMultiplier;
            wave.attackCooldownMultiplier *= (1f / progressionMultiplier);
            wave.telegraphDurationMultiplier *= (1f / progressionMultiplier);

            // Slightly increase heavy drone probability over time
            wave.scoutProbability = Mathf.Max(0.3f, wave.scoutProbability - (waveIndex * 0.05f));
            wave.heavyProbability = 1f - wave.scoutProbability;

            // Reduce rest time slightly for later waves
            wave.restDuration = Mathf.Max(3f, wave.restDuration * (1f - (waveIndex * 0.1f)));
        }

        private void ApplyRandomization(WaveConfiguration wave, DifficultyParameters parameters)
        {
            // Add variance to key parameters
            wave.maxSimultaneousDrones = Mathf.RoundToInt(wave.maxSimultaneousDrones *
                (1 + Random.Range(-randomizationVariance, randomizationVariance)));
            wave.spawnInterval *= (1 + Random.Range(-randomizationVariance, randomizationVariance));
            wave.waveDuration *= (1 + Random.Range(-randomizationVariance * 0.5f, randomizationVariance * 0.5f));
            wave.droneSpeedMultiplier *= (1 + Random.Range(-randomizationVariance * 0.3f, randomizationVariance * 0.3f));

            // Clamp values to reasonable ranges
            wave.maxSimultaneousDrones = Mathf.Clamp(wave.maxSimultaneousDrones, 1, 10);
            wave.spawnInterval = Mathf.Clamp(wave.spawnInterval, 0.5f, 5f);
            wave.waveDuration = Mathf.Clamp(wave.waveDuration, 10f, 60f);
            wave.restDuration = Mathf.Clamp(wave.restDuration, 3f, 20f);
            wave.scoutProbability = Mathf.Clamp01(wave.scoutProbability);
            wave.heavyProbability = Mathf.Clamp01(wave.heavyProbability);
        }

        private DifficultyLevel DetermineAdaptiveDifficulty(float averageScore, int completedWaves)
        {
            float performanceScore = averageScore * (1f + (completedWaves * 0.1f));

            if (performanceScore >= 1.5f)
                return DifficultyLevel.Extreme;
            else if (performanceScore >= 1.2f)
                return DifficultyLevel.Hard;
            else if (performanceScore >= 0.8f)
                return DifficultyLevel.Medium;
            else if (performanceScore >= 0.5f)
                return DifficultyLevel.Easy;
            else
                return DifficultyLevel.Tutorial;
        }

        /// <summary>
        /// Creates a wave configuration from existing wave data for modification
        /// </summary>
        public WaveConfiguration CloneWaveConfiguration(WaveConfiguration original)
        {
            return new WaveConfiguration
            {
                waveNumber = original.waveNumber,
                waveDuration = original.waveDuration,
                restDuration = original.restDuration,
                maxSimultaneousDrones = original.maxSimultaneousDrones,
                spawnInterval = original.spawnInterval,
                spawnVariation = original.spawnVariation,
                scoutProbability = original.scoutProbability,
                heavyProbability = original.heavyProbability,
                droneSpeedMultiplier = original.droneSpeedMultiplier,
                attackCooldownMultiplier = original.attackCooldownMultiplier,
                telegraphDurationMultiplier = original.telegraphDurationMultiplier,
                dashSpeedMultiplier = original.dashSpeedMultiplier
            };
        }

        #region Editor Tools

        [ContextMenu("Generate Tutorial Waves")]
        private void GenerateTutorialWaves()
        {
            List<WaveConfiguration> waves = GenerateWaves(DifficultyLevel.Tutorial, 3);
            LogWaveSummary("Tutorial", waves);
        }

        [ContextMenu("Generate Easy Waves")]
        private void GenerateEasyWaves()
        {
            List<WaveConfiguration> waves = GenerateWaves(DifficultyLevel.Easy, 5);
            LogWaveSummary("Easy", waves);
        }

        [ContextMenu("Generate Medium Waves")]
        private void GenerateMediumWaves()
        {
            List<WaveConfiguration> waves = GenerateWaves(DifficultyLevel.Medium, 5);
            LogWaveSummary("Medium", waves);
        }

        [ContextMenu("Generate Hard Waves")]
        private void GenerateHardWaves()
        {
            List<WaveConfiguration> waves = GenerateWaves(DifficultyLevel.Hard, 5);
            LogWaveSummary("Hard", waves);
        }

        [ContextMenu("Generate Extreme Waves")]
        private void GenerateExtremeWaves()
        {
            List<WaveConfiguration> waves = GenerateWaves(DifficultyLevel.Extreme, 5);
            LogWaveSummary("Extreme", waves);
        }

        [ContextMenu("Test Adaptive Generation")]
        private void TestAdaptiveGeneration()
        {
            List<WaveConfiguration> waves = GenerateAdaptiveWaves(0.75f, 3);
            LogWaveSummary("Adaptive", waves);
        }

        private void LogWaveSummary(string difficultyName, List<WaveConfiguration> waves)
        {
            Debug.Log($"=== {difficultyName} Waves Generated ===");
            for (int i = 0; i < waves.Count; i++)
            {
                WaveConfiguration wave = waves[i];
                Debug.Log($"Wave {wave.waveNumber}: {wave.maxSimultaneousDrones} drones, " +
                         $"{wave.spawnInterval:F1}s interval, {wave.waveDuration:F0}s duration, " +
                         $"Speed: {wave.droneSpeedMultiplier:F1}x, Scouts: {(wave.scoutProbability * 100):F0}%");
            }
        }

        #endregion
    }
}
