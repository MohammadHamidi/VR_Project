using UnityEngine;

/// <summary>
/// ScriptableObject for bridge configuration presets.
/// Streamlined to include only essential configuration options.
/// </summary>
[CreateAssetMenu(fileName = "BridgeConfig", menuName = "VR Rehab/Bridge Configuration", order = 1)]
public class BridgeConfig : ScriptableObject
{
    [Header("Bridge Dimensions")]
    [Range(4, 20)] public int plankCount = 8;
    [Range(4f, 20f)] public float bridgeLength = 8f;
    [Range(0.2f, 1f)] public float plankWidth = 0.4f;
    [Range(0.02f, 0.1f)] public float plankThickness = 0.05f;
    [Range(0f, 0.1f)] public float plankGap = 0.02f;

    [Header("Platform Settings")]
    public bool enablePlatforms = true;
    [Range(1f, 4f)] public float platformLength = 2f;
    [Range(1f, 4f)] public float platformWidth = 2f;
    [Range(0.1f, 0.5f)] public float platformThickness = 0.2f;

    [Header("Physics")]
    [Range(0.5f, 5f)] public float plankMass = 2f;
    [Range(20f, 100f)] public float platformMass = 50f;
    [Range(10f, 100f)] public float jointSpring = 30f;
    [Range(1f, 10f)] public float jointDamper = 2f;

    [Header("Materials")]
    public Material plankMaterial;
    public Material platformMaterial;
    public Material anchorMaterial;

    [Header("Player Settings")]
    public bool autoPositionPlayer = true;
    public Vector3 playerSpawnOffset = new Vector3(0, 1.8f, 0);

    // Calculated properties
    public float PlankLength => (bridgeLength - (plankCount - 1) * plankGap) / plankCount;
    public float PlankSpacing => PlankLength + plankGap;
    public float TotalLength => bridgeLength + (enablePlatforms ? (platformLength * 2 + plankGap * 2) : 0);

    // Legacy property names for backward compatibility
    public float totalBridgeLength => bridgeLength;
    public int numberOfPlanks => plankCount;
    public bool createPlatforms => enablePlatforms;
    public float platformGap => plankGap;

    /// <summary>
    /// Creates a default bridge configuration
    /// </summary>
    public static BridgeConfig CreateDefault()
    {
        var config = CreateInstance<BridgeConfig>();
        config.name = "Default Bridge Config";
        return config;
    }

    /// <summary>
    /// Creates a tutorial bridge configuration (shorter, easier)
    /// </summary>
    public static BridgeConfig CreateTutorial()
    {
        var config = CreateDefault();
        config.name = "Tutorial Bridge Config";
        config.plankCount = 5;
        config.bridgeLength = 5f;
        config.jointSpring = 50f; // More forgiving
        return config;
    }

    /// <summary>
    /// Creates a challenge bridge configuration (longer, harder)
    /// </summary>
    public static BridgeConfig CreateChallenge()
    {
        var config = CreateDefault();
        config.name = "Challenge Bridge Config";
        config.plankCount = 12;
        config.bridgeLength = 12f;
        config.plankGap = 0.05f; // Wider gaps
        config.jointSpring = 20f; // Less forgiving
        return config;
    }
}
