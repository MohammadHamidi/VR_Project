using System.Collections;
using UnityEngine;
using VRRehab.Bridge;
using VRRehab.DataPersistence;
using VRRehab.UI;

namespace VRRehab.Bridge
{
    /// <summary>
    /// Game manager for the bridge exercise that integrates with the config generator
    /// </summary>
    public class BridgeGameManager : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private BridgeConfigGenerator configGenerator;
        [SerializeField] private SOLIDBridgeBuilder bridgeBuilder;
        [SerializeField] private BridgeTracker bridgeTracker;
        [SerializeField] private DataPersistenceManager dataManager;

        [Header("Current Session")]
        [SerializeField] private BridgeConfigGenerator.DifficultyLevel currentDifficulty = BridgeConfigGenerator.DifficultyLevel.Easy;
        [SerializeField] private BridgeConfig currentConfig;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float sessionTimeLimit = 300f; // 5 minutes

        [Header("Progress Tracking")]
        private float sessionStartTime;
        private float levelStartTime;
        private bool levelCompleted = false;
        private float currentScore = 0f;

        // Events
        public static event System.Action<BridgeConfig, int> OnLevelStarted;
        public static event System.Action<bool, float> OnLevelCompleted;
        public static event System.Action OnSessionCompleted;

        void Awake()
        {
            InitializeComponents();
        }

        void Start()
        {
            StartNewSession();
        }

        private void InitializeComponents()
        {
            // Find components if not assigned
            if (configGenerator == null)
                configGenerator = GetComponent<BridgeConfigGenerator>() ?? gameObject.AddComponent<BridgeConfigGenerator>();

            if (bridgeBuilder == null)
                bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();

            if (bridgeTracker == null)
                bridgeTracker = FindObjectOfType<BridgeTracker>();

            if (dataManager == null)
                dataManager = FindObjectOfType<DataPersistenceManager>();
        }

        /// <summary>
        /// Starts a new bridge session with the specified difficulty
        /// </summary>
        public void StartNewSession(BridgeConfigGenerator.DifficultyLevel difficulty = BridgeConfigGenerator.DifficultyLevel.Easy)
        {
            currentDifficulty = difficulty;
            currentLevel = 1;
            sessionStartTime = Time.time;
            levelCompleted = false;

            Debug.Log($"Starting new bridge session with {difficulty} difficulty");
            StartCoroutine(SessionRoutine());
        }

        /// <summary>
        /// Generates and starts a new level
        /// </summary>
        public void StartNewLevel()
        {
            if (configGenerator == null)
            {
                Debug.LogError("BridgeConfigGenerator not found!");
                return;
            }

            // Generate new configuration
            currentConfig = configGenerator.GenerateConfig(currentDifficulty);
            levelStartTime = Time.time;
            levelCompleted = false;

            Debug.Log($"Starting level {currentLevel} with {currentConfig.plankCount} planks");

            // Apply configuration to bridge builder
            if (bridgeBuilder != null)
            {
                bridgeBuilder.SetBridgeConfiguration(currentConfig);
                bridgeBuilder.RebuildBridge();
            }

            OnLevelStarted?.Invoke(currentConfig, currentLevel);
        }

        /// <summary>
        /// Advances to the next level
        /// </summary>
        public void AdvanceLevel()
        {
            float levelTime = Time.time - levelStartTime;
            float timeBonus = Mathf.Max(0, (60f - levelTime) / 60f); // Bonus for completing quickly
            currentScore = 1f + timeBonus; // Base score + time bonus

            levelCompleted = true;
            currentLevel++;

            // Increase difficulty slightly for next level
            if (currentLevel % 3 == 0 && currentDifficulty != BridgeConfigGenerator.DifficultyLevel.Extreme)
            {
                currentDifficulty++;
                Debug.Log($"Difficulty increased to {currentDifficulty}");
            }

            OnLevelCompleted?.Invoke(true, currentScore);

            // Record result in data persistence
            RecordExerciseResult(true, currentScore);

            // Start next level after delay
            StartCoroutine(DelayedNextLevel(3f));
        }

        /// <summary>
        /// Handles level failure
        /// </summary>
        public void FailLevel()
        {
            float levelTime = Time.time - levelStartTime;
            currentScore = Mathf.Max(0.1f, 0.5f - (levelTime / 120f)); // Partial score based on time

            levelCompleted = false;

            OnLevelCompleted?.Invoke(false, currentScore);

            // Record result in data persistence
            RecordExerciseResult(false, currentScore);

            Debug.Log($"Level {currentLevel} failed with score {currentScore:F2}");
        }

        /// <summary>
        /// Restarts the current level
        /// </summary>
        public void RestartLevel()
        {
            levelCompleted = false;
            StartNewLevel();
        }

        /// <summary>
        /// Ends the current session
        /// </summary>
        public void EndSession()
        {
            float sessionTime = Time.time - sessionStartTime;
            Debug.Log($"Bridge session ended after {sessionTime:F1} seconds");

            OnSessionCompleted?.Invoke();
        }

        private IEnumerator SessionRoutine()
        {
            // Start the first level
            StartNewLevel();
            
            while (Time.time - sessionStartTime < sessionTimeLimit)
            {
                if (!levelCompleted)
                {
                    // Wait for level completion instead of restarting
                    yield return new WaitUntil(() => levelCompleted);
                }
                else
                {
                    // Level completed, wait a bit before ending session
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }

            EndSession();
        }

        private IEnumerator DelayedNextLevel(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!levelCompleted) // Only advance if not manually restarted
            {
                StartNewLevel();
            }
        }

        private void RecordExerciseResult(bool completed, float score)
        {
            if (dataManager != null && dataManager.GetCurrentProfile() != null)
            {
                dataManager.RecordExerciseResult("BridgeBuilding", currentLevel, completed, score);
                dataManager.EndCurrentSession(score * 10f, $"Completed level {currentLevel} with score {score:F2}");
            }
        }

        /// <summary>
        /// Gets the current level progress
        /// </summary>
        public float GetLevelProgress()
        {
            if (bridgeTracker != null)
            {
                return bridgeTracker.GetProgressPercentage() / 100f; // Convert percentage to 0-1 range
            }
            return 0f;
        }

        /// <summary>
        /// Gets the current session time remaining
        /// </summary>
        public float GetSessionTimeRemaining()
        {
            return Mathf.Max(0, sessionTimeLimit - (Time.time - sessionStartTime));
        }

        #region Public API

        public BridgeConfig GetCurrentConfig()
        {
            return currentConfig;
        }

        public int GetCurrentLevel()
        {
            return currentLevel;
        }

        public BridgeConfigGenerator.DifficultyLevel GetCurrentDifficulty()
        {
            return currentDifficulty;
        }

        public void SetDifficulty(BridgeConfigGenerator.DifficultyLevel difficulty)
        {
            currentDifficulty = difficulty;
            StartNewLevel();
        }

        public bool IsLevelCompleted()
        {
            return levelCompleted;
        }

        public float GetCurrentScore()
        {
            return currentScore;
        }

        #endregion

        #region Editor Tools

        [ContextMenu("Start Easy Session")]
        private void StartEasySession()
        {
            StartNewSession(BridgeConfigGenerator.DifficultyLevel.Easy);
        }

        [ContextMenu("Start Medium Session")]
        private void StartMediumSession()
        {
            StartNewSession(BridgeConfigGenerator.DifficultyLevel.Medium);
        }

        [ContextMenu("Start Hard Session")]
        private void StartHardSession()
        {
            StartNewSession(BridgeConfigGenerator.DifficultyLevel.Hard);
        }

        [ContextMenu("Advance Level")]
        private void ForceAdvanceLevel()
        {
            AdvanceLevel();
        }

        [ContextMenu("Fail Level")]
        private void ForceFailLevel()
        {
            FailLevel();
        }

        #endregion
    }
}
