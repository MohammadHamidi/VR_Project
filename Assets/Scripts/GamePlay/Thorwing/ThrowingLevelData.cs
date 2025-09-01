// Assets/Scripts/ThrowingLevelData.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ThrowingLevelData", menuName = "Game/Throwing Level Data")]
public class ThrowingLevelData : ScriptableObject
{
    [Header("XR Settings")]
    [Tooltip("Prefab containing your XR Origin / Interaction Manager, etc.")]
    public GameObject xrRigPrefab;

    [Header("UI Settings")]
    [Tooltip("Prefab containing your StageUIController (e.g. Canvas + UI scripts)")]
    public GameObject uiControllerPrefab;

    [Header("Environment Settings")]
    [Tooltip("List of environment prefabs (e.g. ground plane, surrounding meshes). Instantiate all and activate one by default.")]
    public List<GameObject> environmentPrefabs = new List<GameObject>();

    [Tooltip("Which environment prefab (by index) should be active at level start.")]
    public int defaultEnvironmentIndex = 0;

    [Header("Environment Generation Settings")]
    [Tooltip("If true, a surrounding environment will be auto-generated OUTSIDE the cubical boundary from ball spawn to last ring.")]
    public bool autoGenerateEnvironment = false;

    [Tooltip("Seed for random generation to get repeatable results.")]
    public int generationSeed = 123;

    [Tooltip("Radius around individual gameplay elements (Ball Spawn, Rings, Respawn Zone, World Origin) where environment should NOT be generated.")]
    [Range(1f, 20f)]
    public float generationExclusionRadius = 5f;

    [Tooltip("How far OUTSIDE the cubical level boundary to extend the generation area. Environment will be generated in this outer ring around the level.")]
    [Range(1f, 50f)]
    public float generationAreaOffsetXZ = 10.0f;

    [Tooltip("How dense the generated environment is (0 = none, 1 = full coverage).")]
    [Range(0f, 1f)]
    public float generationDensity = 0.7f;

    [Tooltip("Base size of generated primitive chunks before variation is applied.")]
    public Vector3 primitiveChunkSize = Vector3.one * 3f;

    [Tooltip("Random variation allowed in chunk size (0 = no variation, 1 = ±100% variation).")]
    [Range(0f, 1f)]
    public float sizeVariation = 0.5f;

    [Tooltip("Min/Max height range for the base Y position of generated chunks.")]
    public Vector2 chunkHeightRange = new Vector2(0f, 5f);

    [Tooltip("Types of primitives or prefabs to use for generated environment pieces.")]
    [System.Serializable]
    public struct GeneratedPrimitiveType
    {
        public enum Type { Cube, Sphere, Capsule, Cylinder, Prefab }
        
        [Tooltip("Type of primitive to generate or Prefab for custom objects.")]
        public Type type;
        
        [Tooltip("Only used if Type is set to Prefab. Must be assigned for Prefab type.")]
        public GameObject prefab;
    }

    public List<GeneratedPrimitiveType> generatedPrimitiveTypes = new List<GeneratedPrimitiveType>
    {
        new GeneratedPrimitiveType { type = GeneratedPrimitiveType.Type.Cube }
    };

    [Header("Ball Settings")]
    [Tooltip("Prefab for the throwable ball (should contain Rigidbody, HoverAndRelease, GrabRespawner, XRGrabInteractable, etc.)")]
    public GameObject ballPrefab;

    [Tooltip("World position where the ball spawns at start")]
    public Vector3 ballSpawnPosition = Vector3.zero;

    [Tooltip("Seconds to wait before respawning a lost ball")]
    [Range(0.1f, 10f)]
    public float respawnDelay = 1f;

    [Header("Respawn Zone")]
    [Tooltip("Prefab for the respawn kill-plane or trigger zone (must have BallRespawnZone attached)")]
    public GameObject respawnZonePrefab;

    [Tooltip("World position of the respawn zone")]
    public Vector3 respawnZonePosition = Vector3.zero;

    [Header("Target Rings")]
    [Tooltip("Prefab used for each TargetRing (must have TargetRing component and a trigger collider)")]
    public GameObject ringPrefab;

    [Tooltip("List of world positions where each ring should be instantiated")]
    public List<Vector3> ringPositions = new List<Vector3>();

    [Header("Stage Settings")]
    [Tooltip("Time (in seconds) allotted to hit all rings")]
    [Range(10f, 300f)]
    public float stageTime = 60f;

    [Tooltip("If true, the countdown timer is enabled; otherwise no timer")]
    public bool enableTimer = true;

    // === UTILITY METHODS ===

    /// <summary>
    /// Gets the cubical boundary that encompasses all gameplay elements
    /// </summary>
    public Bounds GetCubicalBoundary()
    {
        Vector3 ballSpawn = ballSpawnPosition;
        
        // Create initial bounds starting with ball spawn position
        Bounds initialBounds = new Bounds(ballSpawn, Vector3.zero);
        
        // Encapsulate ALL ring positions
        if (ringPositions != null && ringPositions.Count > 0)
        {
            foreach (Vector3 ringPos in ringPositions)
            {
                initialBounds.Encapsulate(ringPos);
            }
        }
        
        // Also encapsulate respawn zone
        initialBounds.Encapsulate(respawnZonePosition);

        // Make it cubical by using the largest dimension
        float maxDimension = Mathf.Max(initialBounds.size.x, initialBounds.size.y, initialBounds.size.z);
        float cubicalSize = Mathf.Max(maxDimension, 10f); // Minimum 10 units

        return new Bounds(initialBounds.center, Vector3.one * cubicalSize);
    }

    /// <summary>
    /// Gets all gameplay element positions that should have exclusion zones
    /// </summary>
    public List<Vector3> GetExclusionPositions()
    {
        List<Vector3> positions = new List<Vector3>
        {
            Vector3.zero, // World origin (XR Origin)
            ballSpawnPosition,
            respawnZonePosition
        };

        if (ringPositions != null)
        {
            positions.AddRange(ringPositions);
        }

        return positions;
    }

    /// <summary>
    /// Validates the level data for common issues
    /// </summary>
    public List<string> ValidateLevelData()
    {
        List<string> issues = new List<string>();

        if (ballPrefab == null)
            issues.Add("Ball Prefab is not assigned");

        if (ringPrefab == null)
            issues.Add("Ring Prefab is not assigned");

        if (respawnZonePrefab == null)
            issues.Add("Respawn Zone Prefab is not assigned");

        if (ringPositions == null || ringPositions.Count == 0)
            issues.Add("No ring positions defined");

        if (autoGenerateEnvironment)
        {
            if (generatedPrimitiveTypes == null || generatedPrimitiveTypes.Count == 0)
            {
                issues.Add("No generated primitive types defined for environment generation");
            }
            else
            {
                for (int i = 0; i < generatedPrimitiveTypes.Count; i++)
                {
                    var primitiveType = generatedPrimitiveTypes[i];
                    if (primitiveType.type == GeneratedPrimitiveType.Type.Prefab && primitiveType.prefab == null)
                    {
                        issues.Add($"Generated Primitive Type {i} is set to Prefab but no prefab is assigned");
                    }
                }
            }

            Bounds cubicalBounds = GetCubicalBoundary();
            if (generationAreaOffsetXZ <= 1f)
            {
                issues.Add("Generation Area Offset should be at least 1 unit to create meaningful outer generation area");
            }
            
            // Debug boundary calculation
            List<Vector3> boundaryPoints = GetExclusionPositions();
            if (boundaryPoints.Count > 0)
            {
                string pointsDebug = "Boundary includes: ";
                foreach (var point in boundaryPoints)
                {
                    pointsDebug += $"{point:F1}, ";
                }
                Debug.Log($"Environment Generation Boundary Calculation: {pointsDebug.TrimEnd(',', ' ')}");
                Debug.Log($"Calculated Cubical Boundary: Center={cubicalBounds.center:F1}, Size={cubicalBounds.size:F1}");
            }
        }

        if (stageTime <= 0 && enableTimer)
            issues.Add("Stage time must be greater than 0 when timer is enabled");

        return issues;
    }

    // === EDITOR ONLY METHODS ===
    #if UNITY_EDITOR
    /// <summary>
    /// Called when the asset is modified in the editor
    /// </summary>
    private void OnValidate()
    {
        // Ensure minimum values
        generationExclusionRadius = Mathf.Max(1f, generationExclusionRadius);
        generationAreaOffsetXZ = Mathf.Max(1f, generationAreaOffsetXZ); // Minimum 1 unit for meaningful outer area
        respawnDelay = Mathf.Max(0.1f, respawnDelay);
        stageTime = Mathf.Max(10f, stageTime);

        // Ensure chunk size is reasonable
        primitiveChunkSize = new Vector3(
            Mathf.Max(0.1f, primitiveChunkSize.x),
            Mathf.Max(0.1f, primitiveChunkSize.y),
            Mathf.Max(0.1f, primitiveChunkSize.z)
        );

        // Ensure height range is valid
        if (chunkHeightRange.x > chunkHeightRange.y)
        {
            chunkHeightRange.y = chunkHeightRange.x;
        }
    }
    #endif
}