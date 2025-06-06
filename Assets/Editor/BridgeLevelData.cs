// ================================
// LEVEL DATA SCRIPTABLE OBJECTS
// ================================

using UnityEngine;

[CreateAssetMenu(fileName = "BridgeLevel", menuName = "VR Fitness/Bridge Level")]
public class BridgeLevelData : ScriptableObject
{
    [Header("Basic Configuration")]
    public string levelName = "New Bridge Level";
    public string description = "Bridge crossing challenge";
    public float estimatedTime = 60f;
    public int difficultyRating = 1; // 1-5 stars

    [Header("Bridge Structure")]
    public int numberOfPlanks = 8;
    public float totalBridgeLength = 8f;
    public float plankWidth = 0.4f;
    public float plankThickness = 0.05f;
    public float plankGap = 0.02f;

    [Header("Platform Settings")]
    public bool createPlatforms = true;
    public float platformLength = 2f;
    public float platformWidth = 2f;
    public float platformThickness = 0.2f;

    [Header("Physics & Balance")]
    public float plankMass = 2f;
    public float jointSpring = 30f;
    public float jointDamper = 2f;
    public float maxBalanceOffset = 0.25f;
    public float failureDelay = 0.5f;

    [Header("Environmental")]
    public bool useRopes = true;
    public float ropeHeight = 1f;
    public float ropeSag = 0.3f;
    public Material plankMaterial;
    public Material platformMaterial;

    [Header("Spawn Configuration")]
    public Vector3 playerSpawnOffset = new Vector3(0, 1.8f, 0);
    public string nextSceneName = "CombatScene";

    public void ApplyToBridgeBuilder(MultiPlankBridgeBuilder builder)
    {
        builder.SetBridgeConfiguration(numberOfPlanks, totalBridgeLength, plankWidth);
        builder.SetPlatformConfiguration(createPlatforms, platformLength, platformWidth);
        // Apply other settings...
    }
}
