using CombatSystem;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.Bridge;
using CombatSystem.Spawning;

namespace VRRehab.Examples
{
    /// <summary>
    /// Simple menu controller to demonstrate generator usage through UI
    /// Attach this to a Canvas with buttons for testing
    /// </summary>
    public class GeneratorMenuController : MonoBehaviour
    {
        [Header("Bridge Generator")]
        [SerializeField] private BridgeConfigGenerator bridgeGenerator;
        [SerializeField] private BridgeGameManager bridgeManager;

        [Header("Wave Generator")]
        [SerializeField] private WaveGenerator waveGenerator;
        [SerializeField] private WaveManager waveManager;

        [Header("UI References")]
        [SerializeField] private Button bridgeEasyBtn;
        [SerializeField] private Button bridgeMediumBtn;
        [SerializeField] private Button bridgeHardBtn;
        [SerializeField] private Button bridgeRandomBtn;

        [SerializeField] private Button waveEasyBtn;
        [SerializeField] private Button waveMediumBtn;
        [SerializeField] private Button waveHardBtn;
        [SerializeField] private Button waveAdaptiveBtn;

        [SerializeField] private Button startBridgeSessionBtn;
        [SerializeField] private Button startWaveSessionBtn;
        [SerializeField] private Button startCompleteSessionBtn;

        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI bridgeInfoText;
        [SerializeField] private TextMeshProUGUI waveInfoText;

        void Awake()
        {
            InitializeGenerators();
            SetupButtonListeners();
            UpdateUI();
        }

        private void InitializeGenerators()
        {
            if (bridgeGenerator == null)
            {
                bridgeGenerator = FindObjectOfType<BridgeConfigGenerator>();
                if (bridgeGenerator == null)
                {
                    GameObject genObj = new GameObject("BridgeConfigGenerator");
                    bridgeGenerator = genObj.AddComponent<BridgeConfigGenerator>();
                }
            }

            if (waveGenerator == null)
            {
                waveGenerator = FindObjectOfType<WaveGenerator>();
                if (waveGenerator == null)
                {
                    GameObject genObj = new GameObject("WaveGenerator");
                    waveGenerator = genObj.AddComponent<WaveGenerator>();
                }
            }

            bridgeManager = FindObjectOfType<BridgeGameManager>();
            waveManager = FindObjectOfType<WaveManager>();
        }

        private void SetupButtonListeners()
        {
            // Bridge buttons
            if (bridgeEasyBtn != null)
                bridgeEasyBtn.onClick.AddListener(() => GenerateBridgeConfig(BridgeConfigGenerator.DifficultyLevel.Easy));

            if (bridgeMediumBtn != null)
                bridgeMediumBtn.onClick.AddListener(() => GenerateBridgeConfig(BridgeConfigGenerator.DifficultyLevel.Medium));

            if (bridgeHardBtn != null)
                bridgeHardBtn.onClick.AddListener(() => GenerateBridgeConfig(BridgeConfigGenerator.DifficultyLevel.Hard));

            if (bridgeRandomBtn != null)
                bridgeRandomBtn.onClick.AddListener(() => GenerateBridgeConfig(BridgeConfigGenerator.DifficultyLevel.Random));

            // Wave buttons
            if (waveEasyBtn != null)
                waveEasyBtn.onClick.AddListener(() => GenerateWaveSet(WaveGenerator.DifficultyLevel.Easy));

            if (waveMediumBtn != null)
                waveMediumBtn.onClick.AddListener(() => GenerateWaveSet(WaveGenerator.DifficultyLevel.Medium));

            if (waveHardBtn != null)
                waveHardBtn.onClick.AddListener(() => GenerateWaveSet(WaveGenerator.DifficultyLevel.Hard));

            if (waveAdaptiveBtn != null)
                waveAdaptiveBtn.onClick.AddListener(() => GenerateAdaptiveWaves());

            // Session buttons
            if (startBridgeSessionBtn != null)
                startBridgeSessionBtn.onClick.AddListener(StartBridgeSession);

            if (startWaveSessionBtn != null)
                startWaveSessionBtn.onClick.AddListener(StartWaveSession);

            if (startCompleteSessionBtn != null)
                startCompleteSessionBtn.onClick.AddListener(StartCompleteSession);
        }

        // ===== BRIDGE METHODS =====

        private void GenerateBridgeConfig(BridgeConfigGenerator.DifficultyLevel difficulty)
        {
            if (bridgeGenerator == null) return;

            BridgeConfig config = bridgeGenerator.GenerateConfig(difficulty);
            UpdateBridgeInfo(config, difficulty);
            SetStatusText($"Generated {difficulty} bridge configuration");

            // Apply to bridge manager if available
            if (bridgeManager != null)
            {
                bridgeManager.SetDifficulty(difficulty);
            }
        }

        private void UpdateBridgeInfo(BridgeConfig config, BridgeConfigGenerator.DifficultyLevel difficulty)
        {
            if (bridgeInfoText != null)
            {
                bridgeInfoText.text = $"Bridge Config ({difficulty})\n" +
                                    $"Planks: {config.plankCount}\n" +
                                    $"Length: {config.bridgeLength}m\n" +
                                    $"Gap: {config.plankGap:F3}\n" +
                                    $"Spring: {config.jointSpring:F0}";
            }
        }

        private void StartBridgeSession()
        {
            if (bridgeManager != null)
            {
                bridgeManager.StartNewSession(BridgeConfigGenerator.DifficultyLevel.Medium);
                SetStatusText("Bridge session started");
            }
            else
            {
                SetStatusText("Bridge manager not found");
            }
        }

        // ===== WAVE METHODS =====

        private void GenerateWaveSet(WaveGenerator.DifficultyLevel difficulty)
        {
            if (waveGenerator == null) return;

            var waves = waveGenerator.GenerateWaves(difficulty, 5);
            UpdateWaveInfo(waves, difficulty);
            SetStatusText($"Generated {difficulty} wave set ({waves.Count} waves)");
        }

        private void GenerateAdaptiveWaves()
        {
            if (waveGenerator == null) return;

            var waves = waveGenerator.GenerateAdaptiveWaves(0.75f, 3);
            UpdateWaveInfo(waves, WaveGenerator.DifficultyLevel.Adaptive);
            SetStatusText("Generated adaptive wave set");
        }

        private void UpdateWaveInfo(System.Collections.Generic.List<WaveConfiguration> waves, WaveGenerator.DifficultyLevel difficulty)
        {
            if (waveInfoText != null && waves.Count > 0)
            {
                WaveConfiguration firstWave = waves[0];
                waveInfoText.text = $"Wave Set ({difficulty})\n" +
                                   $"Total Waves: {waves.Count}\n" +
                                   $"Max Drones: {firstWave.maxSimultaneousDrones}\n" +
                                   $"Spawn Interval: {firstWave.spawnInterval:F1}s\n" +
                                   $"Wave Duration: {firstWave.waveDuration:F0}s";
            }
        }

        private void StartWaveSession()
        {
            if (waveManager != null)
            {
                waveManager.StartNewSession(WaveGenerator.DifficultyLevel.Medium);
                SetStatusText("Wave session started");
            }
            else
            {
                SetStatusText("Wave manager not found");
            }
        }

        // ===== COMPLETE SESSION =====

        private void StartCompleteSession()
        {
            // This would typically use the GeneratorIntegrationGuide
            var integrationGuide = FindObjectOfType<GeneratorIntegrationGuide>();
            if (integrationGuide != null)
            {
                integrationGuide.StartCompleteSession();
                SetStatusText("Complete rehabilitation session started");
            }
            else
            {
                SetStatusText("Integration guide not found");
            }
        }

        // ===== UTILITY METHODS =====

        private void SetStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                Debug.Log(message);
            }
        }

        private void UpdateUI()
        {
            SetStatusText("Generators ready for use");
        }

        // ===== EDITOR HELPERS =====
        [ContextMenu("Setup Default UI")]
        private void SetupDefaultUI()
        {
            // This would automatically create and assign UI elements
            // For now, just log the setup requirements
            Debug.Log("UI Setup Requirements:");
            Debug.Log("- 8 Buttons (Bridge: Easy, Medium, Hard, Random | Wave: Easy, Medium, Hard, Adaptive)");
            Debug.Log("- 2 Buttons (Start Bridge Session, Start Wave Session)");
            Debug.Log("- 1 Button (Start Complete Session)");
            Debug.Log("- 3 TextMeshProUGUI (Status, Bridge Info, Wave Info)");
        }

        [ContextMenu("Test All Generators")]
        private void TestAllGenerators()
        {
            Debug.Log("=== Testing All Generators ===");

            // Test bridge generator
            var bridgeConfig = bridgeGenerator.GenerateConfig(BridgeConfigGenerator.DifficultyLevel.Medium);
            Debug.Log($"Bridge Test: {bridgeConfig.plankCount} planks generated");

            // Test wave generator
            var waves = waveGenerator.GenerateWaves(WaveGenerator.DifficultyLevel.Medium, 3);
            Debug.Log($"Wave Test: {waves.Count} waves generated");

            SetStatusText("All generators tested successfully");
        }
    }
}
