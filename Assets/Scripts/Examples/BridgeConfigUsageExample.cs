using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRRehab.Bridge;

namespace VRRehab.Examples
{
    /// <summary>
    /// Example showing how to use the BridgeConfigGenerator
    /// Attach this to a GameObject to test bridge configuration generation
    /// </summary>
    public class BridgeConfigUsageExample : MonoBehaviour
    {
        [Header("Generator Reference")]
        [SerializeField] private BridgeConfigGenerator generator;

        [Header("Bridge Builder")]
        [SerializeField] private SOLIDBridgeBuilder bridgeBuilder;

        void Awake()
        {
            if (generator == null)
            {
                generator = GetComponent<BridgeConfigGenerator>() ??
                           gameObject.AddComponent<BridgeConfigGenerator>();
            }

            if (bridgeBuilder == null)
            {
                bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();
            }
        }

        // ===== BASIC USAGE EXAMPLES =====

        /// <summary>
        /// Example 1: Generate a simple bridge config
        /// </summary>
        public void GenerateEasyBridge()
        {
            BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Easy);
            Debug.Log($"Generated Easy Bridge: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        /// <summary>
        /// Example 2: Generate and apply config to bridge builder
        /// </summary>
        public void GenerateAndApplyBridge()
        {
            if (bridgeBuilder == null)
            {
                Debug.LogError("No bridge builder found!");
                return;
            }

            // Generate medium difficulty config
            BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);

            // Apply to bridge builder
            bridgeBuilder.SetBridgeConfiguration(config);
            bridgeBuilder.RebuildBridge();

            Debug.Log("Applied generated config to bridge builder");
        }

        /// <summary>
        /// Example 3: Generate progressive level set
        /// </summary>
        public void GenerateLevelProgression()
        {
            List<BridgeConfig> levels = generator.GenerateProgressiveConfigs(5);

            for (int i = 0; i < levels.Count; i++)
            {
                Debug.Log($"Level {i + 1}: {levels[i].plankCount} planks, {levels[i].bridgeLength}m length");
            }
        }

        /// <summary>
        /// Example 4: Generate adaptive config based on performance
        /// </summary>
        public void GenerateAdaptiveBridge(float successRate, int currentLevel)
        {
            BridgeConfig config = generator.GenerateAdaptiveConfig(successRate, currentLevel);
            Debug.Log($"Adaptive Bridge (Rate: {successRate:F2}, Level: {currentLevel}): {config.plankCount} planks");
        }

        /// <summary>
        /// Example 5: Generate random config for variety
        /// </summary>
        public void GenerateRandomBridge()
        {
            BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Random);
            Debug.Log($"Random Bridge: {config.plankCount} planks, {config.bridgeLength}m length, Gap: {config.plankGap:F3}");
        }

        // ===== ADVANCED USAGE EXAMPLES =====

        /// <summary>
        /// Example 6: Create a custom session with multiple levels
        /// </summary>
        public void StartCustomBridgeSession()
        {
            StartCoroutine(CustomBridgeSession());
        }

        private IEnumerator CustomBridgeSession()
        {
            if (bridgeBuilder == null) yield break;

            // Level 1: Tutorial
            BridgeConfig tutorial = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Tutorial);
            bridgeBuilder.SetBridgeConfiguration(tutorial);
            bridgeBuilder.RebuildBridge();
            yield return new WaitForSeconds(15f); // Wait for player to complete

            // Level 2: Easy
            BridgeConfig easy = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Easy);
            bridgeBuilder.SetBridgeConfiguration(easy);
            bridgeBuilder.RebuildBridge();
            yield return new WaitForSeconds(20f);

            // Level 3: Medium
            BridgeConfig medium = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);
            bridgeBuilder.SetBridgeConfiguration(medium);
            bridgeBuilder.RebuildBridge();
            yield return new WaitForSeconds(25f);

            Debug.Log("Custom bridge session completed!");
        }

        /// <summary>
        /// Example 7: Generate configs for different therapeutic goals
        /// </summary>
        public void GenerateTherapeuticConfigs()
        {
            // Balance training - shorter, wider bridges
            BridgeConfig balanceConfig = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Easy);
            balanceConfig.plankWidth = 0.6f; // Wider planks
            balanceConfig.plankGap = 0.01f; // Smaller gaps
            Debug.Log("Balance training config generated");

            // Strength training - longer, unstable bridges
            BridgeConfig strengthConfig = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Hard);
            strengthConfig.plankGap = 0.08f; // Larger gaps
            strengthConfig.jointSpring = 15f; // Less stable
            Debug.Log("Strength training config generated");
        }

        // ===== EDITOR BUTTONS =====
        [ContextMenu("Test Easy Bridge")]
        private void TestEasyBridge() => GenerateEasyBridge();

        [ContextMenu("Test Medium Bridge")]
        private void TestMediumBridge()
        {
            BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);
            Debug.Log($"Medium Bridge: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Test Hard Bridge")]
        private void TestHardBridge()
        {
            BridgeConfig config = generator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Hard);
            Debug.Log($"Hard Bridge: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Test Random Bridge")]
        private void TestRandomBridge() => GenerateRandomBridge();

        [ContextMenu("Test Progressive Set")]
        private void TestProgressiveSet() => GenerateLevelProgression();

        [ContextMenu("Test Custom Session")]
        private void TestCustomSession() => StartCustomBridgeSession();
    }
}
