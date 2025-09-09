using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRRehab.Bridge
{
    /// <summary>
    /// Generator for bridge configurations with difficulty-based presets and randomization
    /// </summary>
    public class BridgeConfigGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class DifficultySettings
        {
            [Header("Bridge Dimensions")]
            public int minPlankCount = 5;
            public int maxPlankCount = 8;
            public float minBridgeLength = 5f;
            public float maxBridgeLength = 8f;

            [Header("Difficulty Modifiers")]
            [Range(0.1f, 0.5f)] public float plankGapMultiplier = 0.02f;
            [Range(20f, 100f)] public float jointSpringRange = 30f;
            [Range(1f, 10f)] public float jointDamperRange = 2f;

            [Header("Stability Factors")]
            [Range(0.5f, 2f)] public float plankMassMultiplier = 1f;
            [Range(0.8f, 1.5f)] public float plankWidthMultiplier = 1f;
        }

        [Header("Difficulty Configurations")]
        [SerializeField] private DifficultySettings easySettings;
        [SerializeField] private DifficultySettings mediumSettings;
        [SerializeField] private DifficultySettings hardSettings;

        [Header("Generation Settings")]
        [SerializeField] private bool useRandomization = true;
        [SerializeField] private float randomizationVariance = 0.2f; // ±20% variance
        [SerializeField] private Material[] plankMaterials;
        [SerializeField] private Material[] platformMaterials;

        // Events
        public static event Action<BridgeConfig, DifficultyLevel> OnConfigGenerated;

        public enum DifficultyLevel
        {
            Tutorial,
            Easy,
            Medium,
            Hard,
            Extreme,
            Random
        }

        void Awake()
        {
            InitializeDefaultSettings();
        }

        private void InitializeDefaultSettings()
        {
            if (easySettings == null)
            {
                easySettings = new DifficultySettings
                {
                    minPlankCount = 4,
                    maxPlankCount = 6,
                    minBridgeLength = 4f,
                    maxBridgeLength = 6f,
                    plankGapMultiplier = 0.01f,
                    jointSpringRange = 50f,
                    jointDamperRange = 3f,
                    plankMassMultiplier = 1.5f,
                    plankWidthMultiplier = 1.2f
                };
            }

            if (mediumSettings == null)
            {
                mediumSettings = new DifficultySettings
                {
                    minPlankCount = 7,
                    maxPlankCount = 10,
                    minBridgeLength = 7f,
                    maxBridgeLength = 10f,
                    plankGapMultiplier = 0.02f,
                    jointSpringRange = 30f,
                    jointDamperRange = 2f,
                    plankMassMultiplier = 1f,
                    plankWidthMultiplier = 1f
                };
            }

            if (hardSettings == null)
            {
                hardSettings = new DifficultySettings
                {
                    minPlankCount = 10,
                    maxPlankCount = 15,
                    minBridgeLength = 10f,
                    maxBridgeLength = 15f,
                    plankGapMultiplier = 0.04f,
                    jointSpringRange = 20f,
                    jointDamperRange = 1.5f,
                    plankMassMultiplier = 0.8f,
                    plankWidthMultiplier = 0.9f
                };
            }
        }

        /// <summary>
        /// Generates a bridge configuration based on the specified difficulty level
        /// </summary>
        public BridgeConfig GenerateConfig(DifficultyLevel difficulty)
        {
            BridgeConfig config = ScriptableObject.CreateInstance<BridgeConfig>();
            config.name = $"{difficulty} Bridge Config";

            DifficultySettings settings = GetDifficultySettings(difficulty);

            if (difficulty == DifficultyLevel.Random)
            {
                settings = GenerateRandomSettings();
            }

            // Apply settings to config
            ApplySettingsToConfig(config, settings);

            if (useRandomization)
            {
                ApplyRandomization(config);
            }

            // Assign random materials if available
            AssignRandomMaterials(config);

            OnConfigGenerated?.Invoke(config, difficulty);
            return config;
        }

        /// <summary>
        /// Generates a collection of bridge configurations for progressive difficulty
        /// </summary>
        public List<BridgeConfig> GenerateProgressiveConfigs(int count)
        {
            List<BridgeConfig> configs = new List<BridgeConfig>();
            DifficultyLevel[] difficulties = { DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard };

            for (int i = 0; i < count; i++)
            {
                DifficultyLevel difficulty = difficulties[Mathf.Min(i, difficulties.Length - 1)];
                configs.Add(GenerateConfig(difficulty));
            }

            return configs;
        }

        /// <summary>
        /// Generates a bridge configuration based on patient progress
        /// </summary>
        public BridgeConfig GenerateAdaptiveConfig(float successRate, int currentLevel)
        {
            DifficultyLevel difficulty = DetermineDifficultyFromProgress(successRate, currentLevel);
            return GenerateConfig(difficulty);
        }

        private DifficultySettings GetDifficultySettings(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Tutorial:
                    return new DifficultySettings
                    {
                        minPlankCount = 3,
                        maxPlankCount = 5,
                        minBridgeLength = 3f,
                        maxBridgeLength = 5f,
                        plankGapMultiplier = 0.005f,
                        jointSpringRange = 60f,
                        jointDamperRange = 4f,
                        plankMassMultiplier = 2f,
                        plankWidthMultiplier = 1.5f
                    };

                case DifficultyLevel.Easy:
                    return easySettings;

                case DifficultyLevel.Medium:
                    return mediumSettings;

                case DifficultyLevel.Hard:
                    return hardSettings;

                case DifficultyLevel.Extreme:
                    return new DifficultySettings
                    {
                        minPlankCount = 15,
                        maxPlankCount = 20,
                        minBridgeLength = 15f,
                        maxBridgeLength = 20f,
                        plankGapMultiplier = 0.06f,
                        jointSpringRange = 15f,
                        jointDamperRange = 1f,
                        plankMassMultiplier = 0.6f,
                        plankWidthMultiplier = 0.8f
                    };

                default:
                    return mediumSettings;
            }
        }

        private DifficultySettings GenerateRandomSettings()
        {
            return new DifficultySettings
            {
                minPlankCount = UnityEngine.Random.Range(3, 12),
                maxPlankCount = UnityEngine.Random.Range(8, 18),
                minBridgeLength = UnityEngine.Random.Range(3f, 8f),
                maxBridgeLength = UnityEngine.Random.Range(8f, 16f),
                plankGapMultiplier = UnityEngine.Random.Range(0.01f, 0.05f),
                jointSpringRange = UnityEngine.Random.Range(20f, 60f),
                jointDamperRange = UnityEngine.Random.Range(1f, 4f),
                plankMassMultiplier = UnityEngine.Random.Range(0.8f, 1.5f),
                plankWidthMultiplier = UnityEngine.Random.Range(0.9f, 1.3f)
            };
        }

        private void ApplySettingsToConfig(BridgeConfig config, DifficultySettings settings)
        {
            // Randomize within the difficulty range
            config.plankCount = UnityEngine.Random.Range(settings.minPlankCount, settings.maxPlankCount + 1);
            config.bridgeLength = UnityEngine.Random.Range(settings.minBridgeLength, settings.maxBridgeLength);

            // Apply difficulty modifiers
            config.plankGap = settings.plankGapMultiplier;
            config.jointSpring = settings.jointSpringRange;
            config.jointDamper = settings.jointDamperRange;

            // Apply stability factors
            config.plankMass = 2f * settings.plankMassMultiplier;
            config.platformMass = 50f * settings.plankMassMultiplier;
            config.plankWidth = 0.4f * settings.plankWidthMultiplier;
            config.plankThickness = 0.05f * settings.plankWidthMultiplier;

            // Set default platform settings
            config.enablePlatforms = true;
            config.platformLength = 2f;
            config.platformWidth = 2f;
            config.platformThickness = 0.2f;

            // Player settings
            config.autoPositionPlayer = true;
            config.playerSpawnOffset = new Vector3(0, 1.8f, 0);
        }

        private void ApplyRandomization(BridgeConfig config)
        {
            // Add variance to key parameters
            config.plankCount = Mathf.RoundToInt(config.plankCount * (1 + UnityEngine.Random.Range(-randomizationVariance, randomizationVariance)));
            config.bridgeLength *= (1 + UnityEngine.Random.Range(-randomizationVariance, randomizationVariance));
            config.plankGap *= (1 + UnityEngine.Random.Range(-randomizationVariance, randomizationVariance));
            config.jointSpring *= (1 + UnityEngine.Random.Range(-randomizationVariance, randomizationVariance));
            config.plankMass *= (1 + UnityEngine.Random.Range(-randomizationVariance * 0.5f, randomizationVariance * 0.5f));

            // Clamp values to reasonable ranges
            config.plankCount = Mathf.Clamp(config.plankCount, 3, 20);
            config.bridgeLength = Mathf.Clamp(config.bridgeLength, 3f, 20f);
            config.plankGap = Mathf.Clamp(config.plankGap, 0f, 0.1f);
            config.jointSpring = Mathf.Clamp(config.jointSpring, 10f, 100f);
            config.plankMass = Mathf.Clamp(config.plankMass, 0.5f, 5f);
        }

        private void AssignRandomMaterials(BridgeConfig config)
        {
            if (plankMaterials != null && plankMaterials.Length > 0)
            {
                config.plankMaterial = plankMaterials[UnityEngine.Random.Range(0, plankMaterials.Length)];
            }

            if (platformMaterials != null && platformMaterials.Length > 0)
            {
                config.platformMaterial = platformMaterials[UnityEngine.Random.Range(0, platformMaterials.Length)];
            }
        }

        private DifficultyLevel DetermineDifficultyFromProgress(float successRate, int currentLevel)
        {
            if (successRate >= 0.8f && currentLevel >= 3)
                return DifficultyLevel.Hard;
            else if (successRate >= 0.6f && currentLevel >= 2)
                return DifficultyLevel.Medium;
            else if (successRate >= 0.4f || currentLevel >= 1)
                return DifficultyLevel.Easy;
            else
                return DifficultyLevel.Tutorial;
        }

        #region Editor Tools

        [ContextMenu("Generate Tutorial Config")]
        private void GenerateTutorialConfig()
        {
            BridgeConfig config = GenerateConfig(DifficultyLevel.Tutorial);
            Debug.Log($"Generated Tutorial Config: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Generate Easy Config")]
        private void GenerateEasyConfig()
        {
            BridgeConfig config = GenerateConfig(DifficultyLevel.Easy);
            Debug.Log($"Generated Easy Config: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Generate Medium Config")]
        private void GenerateMediumConfig()
        {
            BridgeConfig config = GenerateConfig(DifficultyLevel.Medium);
            Debug.Log($"Generated Medium Config: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Generate Hard Config")]
        private void GenerateHardConfig()
        {
            BridgeConfig config = GenerateConfig(DifficultyLevel.Hard);
            Debug.Log($"Generated Hard Config: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Generate Random Config")]
        private void GenerateRandomConfig()
        {
            BridgeConfig config = GenerateConfig(DifficultyLevel.Random);
            Debug.Log($"Generated Random Config: {config.plankCount} planks, {config.bridgeLength}m length");
        }

        [ContextMenu("Generate Progressive Set")]
        private void GenerateProgressiveSet()
        {
            List<BridgeConfig> configs = GenerateProgressiveConfigs(5);
            for (int i = 0; i < configs.Count; i++)
            {
                Debug.Log($"Level {i + 1}: {configs[i].plankCount} planks, {configs[i].bridgeLength}m length");
            }
        }

        #endregion
    }
}
