using System.Collections;
using System.Collections.Generic;
using CombatSystem;
using UnityEngine;
using VRRehab.Bridge;
using CombatSystem.Spawning;
using VRRehab.DataPersistence;

namespace VRRehab.Examples
{
    /// <summary>
    /// Complete integration guide showing how to use both generators in a real game session
    /// This demonstrates a typical rehabilitation session flow
    /// </summary>
    public class GeneratorIntegrationGuide : MonoBehaviour
    {
        [Header("Bridge System")]
        [SerializeField] private BridgeConfigGenerator bridgeGenerator;
        [SerializeField] private BridgeGameManager bridgeManager;

        [Header("Wave System")]
        [SerializeField] private WaveGenerator waveGenerator;
        [SerializeField] private WaveManager waveManager;

        [Header("Data Persistence")]
        [SerializeField] private DataPersistenceManager dataManager;

        [Header("Session Configuration")]
        [SerializeField] private float sessionDuration = 900f; // 15 minutes
        [SerializeField] private int bridgeLevelsPerSession = 3;
        [SerializeField] private int waveSetsPerSession = 2;

        void Start()
        {
            InitializeSystems();
            StartCompleteSession();
        }

        private void InitializeSystems()
        {
            // Find or create generators
            if (bridgeGenerator == null)
                bridgeGenerator = GetComponent<BridgeConfigGenerator>() ??
                                 gameObject.AddComponent<BridgeConfigGenerator>();

            if (waveGenerator == null)
                waveGenerator = GetComponent<WaveGenerator>() ??
                               gameObject.AddComponent<WaveGenerator>();

            // Find managers
            bridgeManager = FindObjectOfType<BridgeGameManager>();
            waveManager = FindObjectOfType<WaveManager>();
            dataManager = FindObjectOfType<DataPersistenceManager>();
        }

        /// <summary>
        /// Example 1: Complete rehabilitation session with both exercises
        /// </summary>
        public void StartCompleteSession()
        {
            StartCoroutine(CompleteRehabSession());
        }

        private IEnumerator CompleteRehabSession()
        {
            Debug.Log("=== Starting Complete Rehabilitation Session ===");

            // Phase 1: Bridge Exercise (Balance & Coordination)
            Debug.Log("Phase 1: Bridge Exercise Training");
            yield return StartCoroutine(BridgeTrainingPhase());

            // Rest period
            Debug.Log("Rest period: 60 seconds");
            yield return new WaitForSeconds(60f);

            // Phase 2: Wave Exercise (Reflexes & Agility)
            Debug.Log("Phase 2: Wave Exercise Training");
            yield return StartCoroutine(WaveTrainingPhase());

            // Phase 3: Mixed Training (Adaptive difficulty)
            Debug.Log("Phase 3: Mixed Adaptive Training");
            yield return StartCoroutine(MixedAdaptivePhase());

            Debug.Log("=== Rehabilitation Session Complete ===");
        }

        private IEnumerator BridgeTrainingPhase()
        {
            if (bridgeManager == null) yield break;

            // Start with easy difficulty
            bridgeManager.StartNewSession(BridgeConfigGenerator.DifficultyLevel.Easy);

            for (int i = 0; i < bridgeLevelsPerSession; i++)
            {
                // Wait for current level to complete
                while (!bridgeManager.IsLevelCompleted())
                {
                    yield return new WaitForSeconds(1f);
                }

                // Advance to next level with increased difficulty
                if (i < bridgeLevelsPerSession - 1)
                {
                    bridgeManager.AdvanceLevel();
                    yield return new WaitForSeconds(2f); // Brief pause between levels
                }
            }

            // Complete bridge session
            bridgeManager.EndSession();
        }

        private IEnumerator WaveTrainingPhase()
        {
            if (waveManager == null) yield break;

            // Start with medium difficulty
            waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Medium);

            // Let wave manager handle the waves automatically
            float phaseStartTime = Time.time;
            while (Time.time - phaseStartTime < 300f && waveManager.IsSessionActive()) // 5 minutes
            {
                yield return new WaitForSeconds(5f); // Check every 5 seconds
            }

            waveManager.EndCurrentSession();
        }

        private IEnumerator MixedAdaptivePhase()
        {
            // Get player performance data
            float averageBridgeScore = GetAverageBridgeScore();
            float averageWaveScore = GetAverageWaveScore();

            // Calculate overall performance
            float overallPerformance = (averageBridgeScore + averageWaveScore) / 2f;

            Debug.Log($"Overall Performance: {overallPerformance:F2}");

            // Adaptive bridge challenge
            BridgeConfigGenerator.DifficultyLevel bridgeDifficulty =
                overallPerformance > 0.8f ? BridgeConfigGenerator.DifficultyLevel.Hard :
                overallPerformance > 0.6f ? BridgeConfigGenerator.DifficultyLevel.Medium :
                BridgeConfigGenerator.DifficultyLevel.Easy;

            BridgeConfig adaptiveBridge = bridgeGenerator.GenerateConfig(bridgeDifficulty);
            Debug.Log($"Adaptive Bridge: {adaptiveBridge.plankCount} planks, Difficulty: {bridgeDifficulty}");

            // Adaptive wave challenge
            waveManager.StartAdaptiveSession(overallPerformance, 3);

            yield return new WaitForSeconds(180f); // 3 minutes adaptive training
        }

        /// <summary>
        /// Example 2: Quick assessment session
        /// </summary>
        public void StartAssessmentSession()
        {
            StartCoroutine(AssessmentSession());
        }

        private IEnumerator AssessmentSession()
        {
            Debug.Log("=== Starting Assessment Session ===");

            // Quick bridge assessment
            BridgeConfig assessmentBridge = bridgeGenerator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);
            Debug.Log($"Assessment Bridge: {assessmentBridge.plankCount} planks, {assessmentBridge.bridgeLength}m length");

            yield return new WaitForSeconds(60f); // 1 minute assessment

            // Quick wave assessment
            List<WaveConfiguration> assessmentWaves = waveGenerator.GenerateWaves(WaveGenerator.DifficultyLevel.Easy, 2);
            Debug.Log($"Assessment Waves: {assessmentWaves.Count} waves, Max drones: {assessmentWaves[0].maxSimultaneousDrones}");

            yield return new WaitForSeconds(60f); // 1 minute assessment

            // Generate report
            GenerateAssessmentReport();
        }

        /// <summary>
        /// Example 3: Therapeutic customization
        /// </summary>
        public void CreateTherapeuticSession(string therapeuticGoal)
        {
            switch (therapeuticGoal.ToLower())
            {
                case "balance":
                    CreateBalanceTrainingSession();
                    break;
                case "strength":
                    CreateStrengthTrainingSession();
                    break;
                case "reflexes":
                    CreateReflexTrainingSession();
                    break;
                case "endurance":
                    CreateEnduranceTrainingSession();
                    break;
                default:
                    Debug.LogWarning("Unknown therapeutic goal: " + therapeuticGoal);
                    break;
            }
        }

        private void CreateBalanceTrainingSession()
        {
            // Balance training: easy bridges with focus on stability
            BridgeConfig balanceBridge = bridgeGenerator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Easy);
            balanceBridge.plankGap = 0.01f; // Very small gaps
            balanceBridge.plankWidth = 0.6f; // Wider planks
            balanceBridge.jointSpring = 40f; // More stable

            Debug.Log("Balance training session configured");
        }

        private void CreateStrengthTrainingSession()
        {
            // Strength training: challenging bridges requiring more effort
            BridgeConfig strengthBridge = bridgeGenerator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Hard);
            strengthBridge.plankGap = 0.08f; // Large gaps
            strengthBridge.jointSpring = 15f; // Less stable
            strengthBridge.plankMass = 3f; // Heavier planks

            Debug.Log("Strength training session configured");
        }

        private void CreateReflexTrainingSession()
        {
            // Reflex training: fast-paced waves with quick reactions needed
            List<WaveConfiguration> reflexWaves = waveGenerator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 5);

            foreach (var wave in reflexWaves)
            {
                wave.spawnInterval = 1.5f; // Faster spawns
                wave.telegraphDurationMultiplier = 0.8f; // Less warning time
                wave.droneSpeedMultiplier = 1.3f; // Faster drones
            }

            Debug.Log("Reflex training session configured");
        }

        private void CreateEnduranceTrainingSession()
        {
            // Endurance training: longer sessions with consistent difficulty
            List<WaveConfiguration> enduranceWaves = waveGenerator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 8);

            foreach (var wave in enduranceWaves)
            {
                wave.waveDuration = 45f; // Longer waves
                wave.restDuration = 10f; // Shorter rests
                wave.maxSimultaneousDrones = 3; // Consistent drone count
            }

            Debug.Log("Endurance training session configured");
        }

        /// <summary>
        /// Example 4: Performance-based progression
        /// </summary>
        public void StartPerformanceBasedSession()
        {
            StartCoroutine(PerformanceBasedSession());
        }

        private IEnumerator PerformanceBasedSession()
        {
            float currentPerformance = 0.5f; // Starting assumption

            for (int level = 1; level <= 5; level++)
            {
                // Generate adaptive content based on current performance
                BridgeConfig bridge = bridgeGenerator.GenerateAdaptiveConfig(currentPerformance, level);

                if (waveManager != null)
                {
                    waveManager.StartAdaptiveSession(currentPerformance, level);
                }

                // Simulate level completion and performance update
                yield return new WaitForSeconds(120f); // 2 minutes per level

                // Update performance based on simulated results
                currentPerformance = Mathf.Clamp(currentPerformance + Random.Range(-0.1f, 0.2f), 0.2f, 1.5f);

                Debug.Log($"Level {level} completed. Performance: {currentPerformance:F2}");
            }
        }

        // ===== UTILITY METHODS =====

        private float GetAverageBridgeScore()
        {
            if (dataManager != null && dataManager.GetCurrentProfile() != null)
            {
                return (float) dataManager.GetCurrentProfile().GetAverageSessionScore();
            }
            return 0.5f; // Default assumption
        }

        private float GetAverageWaveScore()
        {
            // This would integrate with wave scoring system
            // For now, return a simulated value
            return Random.Range(0.4f, 0.9f);
        }

        private void GenerateAssessmentReport()
        {
            string report = "=== Assessment Report ===\n";
            report += $"Bridge Performance: {GetAverageBridgeScore():F2}\n";
            report += $"Wave Performance: {GetAverageWaveScore():F2}\n";
            report += $"Overall Assessment: {((GetAverageBridgeScore() + GetAverageWaveScore()) / 2f):F2}\n";

            Debug.Log(report);
        }

        // ===== EDITOR BUTTONS =====
        [ContextMenu("Start Complete Session")]
        private void TestCompleteSession() => StartCompleteSession();

        [ContextMenu("Start Assessment Session")]
        private void TestAssessmentSession() => StartAssessmentSession();

        [ContextMenu("Test Balance Training")]
        private void TestBalanceTraining() => CreateTherapeuticSession("balance");

        [ContextMenu("Test Strength Training")]
        private void TestStrengthTraining() => CreateTherapeuticSession("strength");

        [ContextMenu("Test Reflex Training")]
        private void TestReflexTraining() => CreateTherapeuticSession("reflexes");

        [ContextMenu("Test Endurance Training")]
        private void TestEnduranceTraining() => CreateTherapeuticSession("endurance");

        [ContextMenu("Start Performance-Based Session")]
        private void TestPerformanceBasedSession() => StartPerformanceBasedSession();
    }
}
