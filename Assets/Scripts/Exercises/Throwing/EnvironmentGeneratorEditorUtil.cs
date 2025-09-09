#if UNITY_EDITOR// Assets/Editor/EnvironmentGeneratorEditorUtil.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using System;

public static class EnvironmentGeneratorEditorUtil
{
    private const string GENERATED_ENVIRONMENT_PARENT_NAME = "_GeneratedEnvironment";
    private const string GENERATED_GROUND_NAME = "_GeneratedGround";
    private const float MINIMUM_CUBICAL_DIMENSION = 10f;
    private const float GenerationGridStep = 2.5f;

    [MenuItem("Tools/Throwing Level/Generate Environment in Scene")]
    public static void GenerateEnvironmentFromSelectedLevelDataMenu()
    {
        ThrowingLevelData levelData = Selection.activeObject as ThrowingLevelData;
        if (levelData == null)
        {
            Debug.LogWarning("Please select a ThrowingLevelData asset in the Project window to generate environment.");
            return;
        }

        GenerateEnvironment(levelData);
    }

    /// <summary>
    /// Generates surrounding environment based on the provided LevelData.
    /// Creates a cubical boundary from ball spawn to last ring, applies XZ offsets,
    /// and generates a ground plane scaled 2x the cubical boundary size.
    /// </summary>
    public static void GenerateEnvironment(ThrowingLevelData data)
    {
        if (data == null || !data.autoGenerateEnvironment)
        {
            Debug.LogWarning("Environment generation is not enabled on the selected LevelData or LevelData is null.");
            return;
        }

        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogError("Environment Generation: No valid scene is open or loaded.");
            return;
        }

        // === 1. Setup Parent GameObject ===
        GameObject parentGO = SetupParentGameObject(activeScene);
        if (parentGO == null) return;

        // === 2. Initialize Random State ===
        UnityEngine.Random.InitState(data.generationSeed);

        // === 3. Calculate Cubical Boundary ===
        CubicalBounds bounds = CalculateCubicalBoundary(data);
        
        Debug.Log($"Level Boundary (EXCLUSION): Center={bounds.center}, Size={bounds.size}");
        Debug.Log($"Outer Generation Area: Min={bounds.outerMin}, Max={bounds.outerMax}");

        // === 4. Generate Ground Plane (Always 2x cubical size) ===
        GenerateGroundPlane(bounds, parentGO);

        // === 5. Generate Environment Objects OUTSIDE the level boundary ===
        if (bounds.IsGenerationAreaValid())
        {
            GenerateEnvironmentObjects(data, bounds, parentGO);
        }
        else
        {
            Debug.LogWarning($"Environment Generation: XZ offset ({data.generationAreaOffsetXZ}) creates invalid outer generation area. Only ground plane generated.");
        }

        Debug.Log($"Generated environment for {data.name} with {parentGO.transform.childCount} objects.");
        EditorUtility.SetDirty(parentGO);
        EditorSceneManager.MarkSceneDirty(activeScene);
    }

    /// <summary>
    /// Calculates generation bounds for visualization and actual generation.
    /// Used by both the generator and the editor visualization.
    /// </summary>
    public static void CalculateGenerationBounds(ThrowingLevelData data, 
        out Vector3 cubicalBoundsMin, out Vector3 cubicalBoundsMax, 
        out Vector3 effectiveGenerationMin, out Vector3 effectiveGenerationMax)
    {
        CubicalBounds bounds = CalculateCubicalBoundary(data);
        
        cubicalBoundsMin = bounds.min;
        cubicalBoundsMax = bounds.max;
        effectiveGenerationMin = bounds.outerMin;  // Outer area where generation happens
        effectiveGenerationMax = bounds.outerMax;  // Outer area where generation happens
    }

    // === PRIVATE HELPER METHODS ===

    /// <summary>
    /// Represents a cubical boundary with outer generation area
    /// </summary>
    private struct CubicalBounds
    {
        public Vector3 center;
        public float size;
        public Vector3 min;
        public Vector3 max;
        public Vector3 outerMin;
        public Vector3 outerMax;

        public bool IsGenerationAreaValid()
        {
            return outerMin.x < outerMax.x && 
                   outerMin.z < outerMax.z && 
                   outerMin.y < outerMax.y;
        }

        public bool IsInsideLevelBoundary(Vector3 position)
        {
            return position.x >= min.x && position.x <= max.x &&
                   position.y >= min.y && position.y <= max.y &&
                   position.z >= min.z && position.z <= max.z;
        }
    }

    /// <summary>
    /// Calculates the cubical boundary from ball spawn to last ring position
    /// The boundary itself is an exclusion zone, generation happens OUTSIDE it
    /// </summary>
    private static CubicalBounds CalculateCubicalBoundary(ThrowingLevelData data)
    {
        Vector3 ballSpawn = data.ballSpawnPosition;
        
        // Create initial bounds starting with ball spawn position
        Bounds initialBounds = new Bounds(ballSpawn, Vector3.zero);
        
        // Encapsulate ALL ring positions, not just the last one
        if (data.ringPositions != null && data.ringPositions.Count > 0)
        {
            foreach (Vector3 ringPos in data.ringPositions)
            {
                initialBounds.Encapsulate(ringPos);
            }
        }
        
        // Also encapsulate respawn zone to ensure complete gameplay area coverage
        initialBounds.Encapsulate(data.respawnZonePosition);

        // Calculate cubical dimension (largest of the three dimensions)
        float maxDimension = Mathf.Max(initialBounds.size.x, initialBounds.size.y, initialBounds.size.z);
        float cubicalSize = Mathf.Max(maxDimension, MINIMUM_CUBICAL_DIMENSION);
        
        Vector3 center = initialBounds.center;
        Vector3 halfSize = Vector3.one * (cubicalSize * 0.5f);

        CubicalBounds result = new CubicalBounds
        {
            center = center,
            size = cubicalSize,
            min = center - halfSize,
            max = center + halfSize
        };

        // Calculate outer generation area - environment generates AROUND the cubical boundary
        float xzOffset = data.generationAreaOffsetXZ;
        result.outerMin = new Vector3(
            result.min.x - xzOffset,  // Expand outward
            result.min.y - xzOffset,  // Also expand Y for full 3D generation
            result.min.z - xzOffset
        );
        result.outerMax = new Vector3(
            result.max.x + xzOffset,  // Expand outward
            result.max.y + xzOffset,  // Also expand Y for full 3D generation
            result.max.z + xzOffset
        );

        Debug.Log($"Boundary calculation: Ball spawn={ballSpawn}, Rings={data.ringPositions?.Count ?? 0}, Respawn={data.respawnZonePosition}");
        Debug.Log($"Initial bounds: center={initialBounds.center:F1}, size={initialBounds.size:F1}");
        Debug.Log($"Final cubical boundary: center={center:F1}, size={cubicalSize:F1}");
        Debug.Log($"Outer generation area: min={result.outerMin:F1}, max={result.outerMax:F1}");
        
        if (data.ringPositions != null && data.ringPositions.Count > 0)
        {
            Debug.Log($"All ring positions: {string.Join(", ", data.ringPositions.Select(r => r.ToString("F1")))}");
        }

        return result;
    }

    /// <summary>
    /// Sets up or clears the parent GameObject for generated environment
    /// </summary>
    private static GameObject SetupParentGameObject(UnityEngine.SceneManagement.Scene activeScene)
    {
        GameObject parentGO = GameObject.Find(GENERATED_ENVIRONMENT_PARENT_NAME);
        
        if (parentGO == null)
        {
            parentGO = new GameObject(GENERATED_ENVIRONMENT_PARENT_NAME);
            EditorSceneManager.MoveGameObjectToScene(parentGO, activeScene);
            Undo.RegisterCreatedObjectUndo(parentGO, "Create Generated Environment Parent");
        }
        else
        {
            if (parentGO.scene != activeScene)
            {
                Debug.LogError($"Environment Generation: Found '{GENERATED_ENVIRONMENT_PARENT_NAME}' but it is not in the active scene '{activeScene.name}'.");
                return null;
            }

            // Clear existing children
            ClearExistingChildren(parentGO);
        }

        return parentGO;
    }

    /// <summary>
    /// Clears all existing children from the parent GameObject
    /// </summary>
    private static void ClearExistingChildren(GameObject parentGO)
    {
        Undo.RecordObject(parentGO.transform, "Clear Existing Generated Environment");
        List<GameObject> childrenToDestroy = new List<GameObject>();
        
        foreach (Transform child in parentGO.transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
        {
            Undo.DestroyObjectImmediate(child);
        }
    }

    /// <summary>
    /// Generates ground plane scaled to 2x the cubical boundary size
    /// </summary>
    private static void GenerateGroundPlane(CubicalBounds bounds, GameObject parentGO)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = GENERATED_GROUND_NAME;
        
        // Position at bottom center of cubical boundary
        ground.transform.position = new Vector3(bounds.center.x,0f, bounds.center.z);
        
        // Scale to 2x cubical size (Unity Plane is 10x10 units at scale 1)
        float groundScale = (bounds.size * 5f) / 10f;
        ground.transform.localScale = new Vector3(groundScale, 1f, groundScale);
        
        ground.transform.parent = parentGO.transform;
        Undo.RegisterCreatedObjectUndo(ground, "Generate Ground Plane");
        EditorUtility.SetDirty(ground);
    }

    /// <summary>
    /// Generates environment objects OUTSIDE the level boundary within the outer generation area
    /// </summary>
    private static void GenerateEnvironmentObjects(ThrowingLevelData data, CubicalBounds bounds, GameObject parentGO)
    {
        // Validate primitive types
        var validTypes = data.generatedPrimitiveTypes?
            .Where(p => p.type != ThrowingLevelData.GeneratedPrimitiveType.Type.Prefab || p.prefab != null)
            .ToList();

        if (validTypes == null || validTypes.Count == 0)
        {
            Debug.LogWarning("Environment Generation: No valid primitive types specified.");
            return;
        }

        // Calculate exclusion areas around key gameplay elements
        HashSet<Vector3> exclusionCenters = CalculateExclusionCenters(data);
        float exclusionRadius = data.generationExclusionRadius;

        int generatedCount = 0;

        // Generate objects on a grid within outer area, but OUTSIDE level boundary
        for (float x = bounds.outerMin.x; x <= bounds.outerMax.x; x += GenerationGridStep)
        {
            for (float z = bounds.outerMin.z; z <= bounds.outerMax.z; z += GenerationGridStep)
            {
                for (float y = bounds.outerMin.y; y <= bounds.outerMax.y; y += GenerationGridStep)
                {
                    if (ShouldGenerateAtPosition(x, y, z, bounds, exclusionCenters, exclusionRadius, data.generationDensity))
                    {
                        GameObject newObj = CreateEnvironmentObject(data, validTypes, new Vector3(x, y, z));
                        if (newObj != null)
                        {
                            newObj.transform.parent = parentGO.transform;
                            newObj.name = $"EnvObj_{generatedCount++}";
                            Undo.RegisterCreatedObjectUndo(newObj, "Generate Environment Object");
                            EditorUtility.SetDirty(newObj);
                        }
                    }
                }
            }
        }

        Debug.Log($"Generated {generatedCount} environment objects outside level boundary.");
    }

    /// <summary>
    /// Calculates exclusion center points around gameplay elements
    /// </summary>
    private static HashSet<Vector3> CalculateExclusionCenters(ThrowingLevelData data)
    {
        HashSet<Vector3> exclusionCenters = new HashSet<Vector3>
        {
            Vector3.zero, // World origin (XR Origin)
            data.ballSpawnPosition,
            data.respawnZonePosition
        };

        if (data.ringPositions != null)
        {
            foreach (var ringPos in data.ringPositions)
            {
                exclusionCenters.Add(ringPos);
            }
        }

        return exclusionCenters;
    }

    /// <summary>
    /// Determines if an object should be generated at the given position
    /// Must be within outer area, OUTSIDE level boundary, and not in exclusion zones
    /// </summary>
    private static bool ShouldGenerateAtPosition(float x, float y, float z, CubicalBounds bounds, 
        HashSet<Vector3> exclusionCenters, float exclusionRadius, float density)
    {
        // Add random offset within grid cell
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-GenerationGridStep * 0.4f, GenerationGridStep * 0.4f),
            UnityEngine.Random.Range(-GenerationGridStep * 0.4f, GenerationGridStep * 0.4f),
            UnityEngine.Random.Range(-GenerationGridStep * 0.4f, GenerationGridStep * 0.4f)
        );

        Vector3 finalPosition = new Vector3(x, y, z) + randomOffset;

        // Check if still within outer generation bounds after random offset
        if (finalPosition.x < bounds.outerMin.x || finalPosition.x > bounds.outerMax.x ||
            finalPosition.y < bounds.outerMin.y || finalPosition.y > bounds.outerMax.y ||
            finalPosition.z < bounds.outerMin.z || finalPosition.z > bounds.outerMax.z)
        {
            return false;
        }

        // CRITICAL: Don't generate INSIDE the level boundary (this is the key fix!)
        if (bounds.IsInsideLevelBoundary(finalPosition))
        {
            return false;
        }

        // Check individual exclusion zones around gameplay elements
        foreach (var exclusionCenter in exclusionCenters)
        {
            if (Vector3.Distance(finalPosition, exclusionCenter) < exclusionRadius)
            {
                return false;
            }
        }

        // Check density probability
        return UnityEngine.Random.value < density;
    }

    /// <summary>
    /// Creates a single environment object at the specified position
    /// </summary>
    private static GameObject CreateEnvironmentObject(ThrowingLevelData data, 
        List<ThrowingLevelData.GeneratedPrimitiveType> validTypes, Vector3 position)
    {
        var primitiveSettings = validTypes[UnityEngine.Random.Range(0, validTypes.Count)];
        GameObject newObj = null;

        if (primitiveSettings.type == ThrowingLevelData.GeneratedPrimitiveType.Type.Prefab)
        {
            newObj = PrefabUtility.InstantiatePrefab(primitiveSettings.prefab) as GameObject;
            if (newObj != null)
            {
                newObj.transform.position = position;
            }
        }
        else
        {
            PrimitiveType primitiveType = GetUnityPrimitiveType(primitiveSettings.type);
            newObj = GameObject.CreatePrimitive(primitiveType);
            newObj.transform.position = position;
            newObj.transform.localScale = data.primitiveChunkSize;
        }

        if (newObj != null)
        {
            ApplyRandomTransformation(newObj, data, primitiveSettings.type);
        }

        return newObj;
    }

    /// <summary>
    /// Converts custom primitive type to Unity PrimitiveType
    /// </summary>
    private static PrimitiveType GetUnityPrimitiveType(ThrowingLevelData.GeneratedPrimitiveType.Type type)
    {
        switch (type)
        {
            case ThrowingLevelData.GeneratedPrimitiveType.Type.Cube: return PrimitiveType.Cube;
            case ThrowingLevelData.GeneratedPrimitiveType.Type.Sphere: return PrimitiveType.Sphere;
            case ThrowingLevelData.GeneratedPrimitiveType.Type.Capsule: return PrimitiveType.Capsule;
            case ThrowingLevelData.GeneratedPrimitiveType.Type.Cylinder: return PrimitiveType.Cylinder;
            default: return PrimitiveType.Cube;
        }
    }

    /// <summary>
    /// Applies random rotation and scale variation to the generated object
    /// </summary>
    private static void ApplyRandomTransformation(GameObject obj, ThrowingLevelData data, 
        ThrowingLevelData.GeneratedPrimitiveType.Type type)
    {
        // Random rotation around Y axis
        obj.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.Self);

        // Apply scale variation only to primitives, not prefabs
        if (type != ThrowingLevelData.GeneratedPrimitiveType.Type.Prefab)
        {
            Vector3 scaleVariation = new Vector3(
                1 + UnityEngine.Random.Range(-data.sizeVariation, data.sizeVariation),
                1 + UnityEngine.Random.Range(-data.sizeVariation, data.sizeVariation),
                1 + UnityEngine.Random.Range(-data.sizeVariation, data.sizeVariation)
            );
            obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scaleVariation);
        }

        // Apply height range variation
        if (data.chunkHeightRange.y > data.chunkHeightRange.x)
        {
            float randomHeight = UnityEngine.Random.Range(data.chunkHeightRange.x, data.chunkHeightRange.y);
            Vector3 pos = obj.transform.position;
            pos.y = 0;
            obj.transform.position = pos;
        }
        else
        {
            Vector3 pos = obj.transform.position;
            pos.y = 0;
            obj.transform.position = pos;
        }
    }
}
#endif