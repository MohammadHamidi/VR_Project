using System.Collections;
using System.Collections.Generic;
using CombatSystem.Combat;
using UnityEngine;
using CombatSystem.Spawning;
using CombatSystem.Events;
using VRRehab.DataPersistence;
using VRRehab.UI;

namespace CombatSystem
{
    /// <summary>
    /// Manager for wave-based combat that integrates with the wave generator
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private WaveGenerator waveGenerator;
        [SerializeField] private DroneSpawner droneSpawner;
        [SerializeField] private CombatScoring combatScoring;
        [SerializeField] private DataPersistenceManager dataManager;

        [Header("Session Settings")]
        [SerializeField] private WaveGenerator.DifficultyLevel currentDifficulty = WaveGenerator.DifficultyLevel.Easy;
        [SerializeField] private bool useAdaptiveDifficulty = true;
        [SerializeField] private float sessionDuration = 300f; // 5 minutes
        [SerializeField] private int maxWavesPerSession = 10;

        [Header("Current Session State")]
        private List<WaveConfiguration> currentWaves;
        private int currentWaveIndex = 0;
        private float sessionStartTime;
        private bool sessionActive = false;
        private float currentScore = 0f;

        // Events
        public static event System.Action<int, WaveConfiguration> OnWaveStarted;
        public static event System.Action<int, bool> OnWaveCompleted;
        public static event System.Action OnSessionCompleted;
        public static event System.Action<WaveGenerator.DifficultyLevel> OnDifficultyChanged;

        void Awake()
        {
            InitializeComponents();
            SubscribeToEvents();
        }

        void Start()
        {
            // Auto-start if configured
            if (droneSpawner != null && droneSpawner.autoStart)
            {
                StartNewSession(currentDifficulty);
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            // Find components if not assigned
            if (waveGenerator == null)
                waveGenerator = GetComponent<WaveGenerator>() ?? gameObject.AddComponent<WaveGenerator>();

            if (droneSpawner == null)
                droneSpawner = FindObjectOfType<DroneSpawner>();

            if (combatScoring == null)
                combatScoring = FindObjectOfType<CombatScoring>();

            if (dataManager == null)
                dataManager = FindObjectOfType<DataPersistenceManager>();
        }

        private void SubscribeToEvents()
        {
            if (combatScoring != null)
            {
                CombatEvents.OnWaveEnded += HandleWaveEnded;
            }

            if (waveGenerator != null)
            {
                WaveGenerator.OnWavesGenerated += HandleWavesGenerated;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (combatScoring != null)
            {
                CombatEvents.OnWaveEnded -= HandleWaveEnded;
            }

            if (waveGenerator != null)
            {
                WaveGenerator.OnWavesGenerated -= HandleWavesGenerated;
            }
        }

        /// <summary>
        /// Starts a new session with the specified difficulty
        /// </summary>
        public void StartNewSession(WaveGenerator.DifficultyLevel difficulty)
        {
            currentDifficulty = difficulty;
            currentWaveIndex = 0;
            sessionStartTime = Time.time;
            sessionActive = true;
            currentScore = 0f;

            Debug.Log($"Starting new squat session with {difficulty} difficulty");

            // Generate waves for this session
            if (waveGenerator != null)
            {
                currentWaves = waveGenerator.GenerateWaves(difficulty, maxWavesPerSession);
            }
            else
            {
                // Fallback to default waves
                currentWaves = CreateDefaultWaves(difficulty);
            }

            // Start the session
            StartCoroutine(SessionRoutine());

            OnDifficultyChanged?.Invoke(difficulty);
        }

        /// <summary>
        /// Starts an adaptive session based on player performance
        /// </summary>
        public void StartAdaptiveSession(float averageScore, int previousWaves)
        {
            if (!useAdaptiveDifficulty)
            {
                StartNewSession(currentDifficulty);
                return;
            }

            if (waveGenerator != null)
            {
                currentWaves = waveGenerator.GenerateAdaptiveWaves(averageScore, previousWaves);
                currentDifficulty = WaveGenerator.DifficultyLevel.Adaptive;
                currentWaveIndex = 0;
                sessionStartTime = Time.time;
                sessionActive = true;
                currentScore = 0f;

                StartCoroutine(SessionRoutine());
                Debug.Log("Starting adaptive squat session");
            }
            else
            {
                StartNewSession(WaveGenerator.DifficultyLevel.Medium);
            }
        }

        private IEnumerator SessionRoutine()
        {
            while (sessionActive && currentWaveIndex < currentWaves.Count &&
                   Time.time - sessionStartTime < sessionDuration)
            {
                StartCurrentWave();
                yield return new WaitUntil(() => !sessionActive || !IsWaveActive());
                currentWaveIndex++;
            }

            EndSession();
        }

        private void StartCurrentWave()
        {
            if (currentWaveIndex >= currentWaves.Count) return;

            WaveConfiguration waveConfig = currentWaves[currentWaveIndex];

            // Apply wave configuration to drone spawner
            if (droneSpawner != null)
            {
                // Clear existing waves and add our generated wave
                droneSpawner.RestartWaves();

                // Since DroneSpawner uses a list of waves, we need to set it directly
                // This might require modifying the DroneSpawner to accept single waves
                StartCoroutine(StartWaveWithConfig(waveConfig));
            }

            OnWaveStarted?.Invoke(currentWaveIndex + 1, waveConfig);
            Debug.Log($"Starting wave {currentWaveIndex + 1}: {waveConfig.maxSimultaneousDrones} drones, " +
                     $"{waveConfig.waveDuration}s duration");
        }

        private IEnumerator StartWaveWithConfig(WaveConfiguration config)
        {
            if (droneSpawner == null) yield break;

            // Temporarily replace the drone spawner's waves with our configuration
            var originalWaves = new List<WaveConfiguration>(droneSpawner.GetWaves());
            droneSpawner.SetWaves(new List<WaveConfiguration> { config });

            // Start the wave
            droneSpawner.RestartWaves();

            // Wait for wave to complete
            yield return new WaitForSeconds(config.waveDuration + config.restDuration);

            // Restore original waves
            droneSpawner.SetWaves(originalWaves);
        }

        private void HandleWaveEnded()
        {
            if (!sessionActive) return;

            bool waveSuccessful = combatScoring != null ? combatScoring.GetWaveSuccess() : true;
            float waveScore = combatScoring != null ? combatScoring.GetWaveScore() : 1f;

            currentScore += waveScore;

            OnWaveCompleted?.Invoke(currentWaveIndex + 1, waveSuccessful);

            // Record wave result
            RecordWaveResult(waveSuccessful, waveScore);

            Debug.Log($"Wave {currentWaveIndex + 1} completed. Success: {waveSuccessful}, Score: {waveScore:F2}");

            // Check if we should adjust difficulty
            if (useAdaptiveDifficulty && waveGenerator != null)
            {
                float averageScore = currentScore / (currentWaveIndex + 1);
                if (averageScore > 1.2f && currentDifficulty != WaveGenerator.DifficultyLevel.Extreme)
                {
                    currentDifficulty++;
                    Debug.Log($"Difficulty increased to {currentDifficulty}");
                }
                else if (averageScore < 0.6f && currentDifficulty != WaveGenerator.DifficultyLevel.Tutorial)
                {
                    currentDifficulty--;
                    Debug.Log($"Difficulty decreased to {currentDifficulty}");
                }
            }
        }

        private void EndSession()
        {
            sessionActive = false;
            float sessionTime = Time.time - sessionStartTime;

            // Record final session results
            RecordSessionResult();

            Debug.Log($"Squat session ended after {sessionTime:F1} seconds. Final score: {currentScore:F2}");

            OnSessionCompleted?.Invoke();
        }

        private void RecordWaveResult(bool successful, float score)
        {
            if (dataManager != null && dataManager.GetCurrentProfile() != null)
            {
                dataManager.RecordExerciseResult("SquatDodge", currentWaveIndex + 1, successful, score);
            }
        }

        private void RecordSessionResult()
        {
            if (dataManager != null && dataManager.GetCurrentProfile() != null)
            {
                float averageScore = currentScore / Mathf.Max(1, currentWaveIndex + 1);
                dataManager.EndCurrentSession(averageScore * 10f,
                    $"Completed {currentWaveIndex + 1} waves with average score {averageScore:F2}");
            }
        }

        private bool IsWaveActive()
        {
            // This would need to be implemented based on DroneSpawner's state
            // For now, we'll assume waves are active for their duration
            return Time.time - sessionStartTime < sessionDuration;
        }

        private List<WaveConfiguration> CreateDefaultWaves(WaveGenerator.DifficultyLevel difficulty)
        {
            List<WaveConfiguration> waves = new List<WaveConfiguration>();

            // Create basic waves based on difficulty
            int waveCount = 5;
            float baseSpawnInterval = difficulty == WaveGenerator.DifficultyLevel.Easy ? 2.5f :
                                    difficulty == WaveGenerator.DifficultyLevel.Hard ? 1.5f : 2f;

            for (int i = 0; i < waveCount; i++)
            {
                WaveConfiguration wave = new WaveConfiguration
                {
                    waveNumber = i + 1,
                    waveDuration = 25f - (i * 2f), // Shorter waves as they progress
                    restDuration = 8f,
                    maxSimultaneousDrones = 2 + i,
                    spawnInterval = Mathf.Max(1f, baseSpawnInterval - (i * 0.2f)),
                    spawnVariation = 0.4f,
                    scoutProbability = 0.8f - (i * 0.1f), // More heavies later
                    heavyProbability = 0.2f + (i * 0.1f),
                    droneSpeedMultiplier = 1f + (i * 0.1f),
                    attackCooldownMultiplier = 1f,
                    telegraphDurationMultiplier = 1f,
                    dashSpeedMultiplier = 1f + (i * 0.1f)
                };

                waves.Add(wave);
            }

            return waves;
        }

        private void HandleWavesGenerated(List<WaveConfiguration> waves, WaveGenerator.DifficultyLevel difficulty)
        {
            Debug.Log($"WaveGenerator created {waves.Count} waves for {difficulty} difficulty");
        }

        #region Public API

        public WaveGenerator.DifficultyLevel GetCurrentDifficulty()
        {
            return currentDifficulty;
        }

        public int GetCurrentWaveNumber()
        {
            return currentWaveIndex + 1;
        }

        public int GetTotalWaves()
        {
            return currentWaves != null ? currentWaves.Count : 0;
        }

        public float GetSessionProgress()
        {
            if (!sessionActive) return 1f;
            return Mathf.Clamp01((Time.time - sessionStartTime) / sessionDuration);
        }

        public float GetCurrentScore()
        {
            return currentScore;
        }

        public bool IsSessionActive()
        {
            return sessionActive;
        }

        public void PauseSession()
        {
            sessionActive = false;
            if (droneSpawner != null)
            {
                droneSpawner.StopAllWaves();
            }
        }

        public void ResumeSession()
        {
            sessionActive = true;
            if (droneSpawner != null && currentWaveIndex < currentWaves.Count)
            {
                StartCurrentWave();
            }
        }

        public void EndCurrentSession()
        {
            EndSession();
        }

        #endregion

        #region Editor Tools

        [ContextMenu("Start Easy Session")]
        private void StartEasySession()
        {
            StartNewSession(WaveGenerator.DifficultyLevel.Easy);
        }

        [ContextMenu("Start Medium Session")]
        private void StartMediumSession()
        {
            StartNewSession(WaveGenerator.DifficultyLevel.Medium);
        }

        [ContextMenu("Start Hard Session")]
        private void StartHardSession()
        {
            StartNewSession(WaveGenerator.DifficultyLevel.Hard);
        }

        [ContextMenu("Start Extreme Session")]
        private void StartExtremeSession()
        {
            StartNewSession(WaveGenerator.DifficultyLevel.Extreme);
        }

        [ContextMenu("End Session")]
        private void ForceEndSession()
        {
            EndSession();
        }

        [ContextMenu("Test Adaptive Session")]
        private void TestAdaptiveSession()
        {
            StartAdaptiveSession(0.8f, 3);
        }

        #endregion
    }

    // Extension methods for DroneSpawner (would need to be added to the actual DroneSpawner class)
    public static class DroneSpawnerExtensions
    {
        public static List<WaveConfiguration> GetWaves(this DroneSpawner spawner)
        {
            // This would need to be implemented in the actual DroneSpawner class
            // For now, return empty list
            return new List<WaveConfiguration>();
        }

        public static void SetWaves(this DroneSpawner spawner, List<WaveConfiguration> waves)
        {
            // This would need to be implemented in the actual DroneSpawner class
            // For now, do nothing
            Debug.LogWarning("SetWaves not implemented in DroneSpawner - this is a placeholder");
        }
    }
}
