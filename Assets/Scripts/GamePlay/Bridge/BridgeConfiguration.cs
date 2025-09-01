using System;
using UnityEngine;

[Serializable]
public class BridgeConfiguration
{
    [Header("Bridge Configuration")]
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
    public float platformGap = 0.1f;

    [Header("Support Structure")]
    public bool useRopes = true;
    public float ropeHeight = 1f;
    public float ropeSag = 0.3f;
    public int supportPostsCount = 0;

    [Header("Physics Settings")]
    public float plankMass = 2f;
    public float platformMass = 50f;
    public float jointSpring = 30f;
    public float jointDamper = 2f;
    public float ropeSpring = 100f;
    public float ropeDamper = 5f;

    [Header("Materials")]
    public Material plankMaterial;
    public Material platformMaterial;
    public Material ropeMaterial;
    public Material anchorMaterial;

    [Header("Player Spawn")]
    public bool autoPositionPlayer = true;
    public Vector3 playerSpawnOffset = new Vector3(0, 1.8f, 0);

    // Calculated properties
    public float PlankLength => (totalBridgeLength - (numberOfPlanks - 1) * plankGap) / numberOfPlanks;
    public float PlankSpacing => PlankLength + plankGap;
    public float TotalSystemLength => totalBridgeLength + (createPlatforms ? (platformLength * 2 + platformGap * 2) : 0);
}