// Assets/Scripts/GamePlay/Throwing/LevelGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Tooltip("Drag in the ThrowingLevelData asset here")]
    public ThrowingLevelData levelData;
    [Tooltip("Drag in the ThrowingLevelData asset here")]
    public List<ThrowingLevelData> levelDatas;

    [Header("Level Progression")]
    [Tooltip("Enable level progression through multiple levels")]
    public bool enableLevelProgression = true;
    [Tooltip("Current level index (0-based)")]
    public int currentLevelIndex = 0;
    [Tooltip("Automatically advance to next level when current level is completed")]
    public bool autoAdvanceLevels = true;

    [Header("Scene References")]
    [Tooltip("Optional parent for all instantiated rings")]
    public Transform ringsParent;

    // Runtime references:
    private BallSpawner _ballSpawner;
    private BallRespawnZone _respawnZone;
    private ObjectStageManager _stageManager;
    private StageUIController _uiControllerInstance;

    // We'll keep track of instantiated environment GameObjects here:
    private List<GameObject> _instantiatedEnvironments = new List<GameObject>();

    void Start()
    {
        // Determine which level data to use
        ThrowingLevelData selectedLevelData = GetCurrentLevelData();
        if (selectedLevelData == null)
        {
            Debug.LogError("LevelGenerator: No LevelData available!");
            return;
        }

        levelData = selectedLevelData;

        // 0) Instantiate all environment prefabs first:
        GenerateEnvironments();

        // 1) Instantiate XR Rig (if provided)
        if (levelData.xrRigPrefab != null)
        {
            Instantiate(levelData.xrRigPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("LevelGenerator: XR Rig Prefab is null. Assign in LevelData if XR is required.");
        }

        // 2) Instantiate UI Controller prefab (and grab its StageUIController child)
        if (levelData.uiControllerPrefab != null)
        {
            GameObject uiRoot = Instantiate(levelData.uiControllerPrefab, Vector3.zero, Quaternion.identity);
            _uiControllerInstance = uiRoot.GetComponentInChildren<StageUIController>();
            if (_uiControllerInstance == null)
            {
                Debug.LogError(
                    $"LevelGenerator: The instantiated UI Controller Prefab “{levelData.uiControllerPrefab.name}”\n" +
                    "does not have a StageUIController component on itself or any of its children."
                );
            }
        }
        else
        {
            Debug.LogWarning("LevelGenerator: UI Controller Prefab is null. Assign in LevelData if UI is required.");
        }

        // 3) Spawn BallSpawner, RespawnZone, Rings, and StageManager
        GenerateBallSpawner();
        GenerateRespawnZone();
        List<TargetRing> rings = GenerateRings();
        GenerateStageManager(rings);
    }

    /// <summary>
    /// Instantiates each environment prefab under a new "Environments" parent GameObject.
    /// Disables all environments, then enables only the default one (if valid).
    /// </summary>
    private void GenerateEnvironments()
    {
        if (levelData.environmentPrefabs == null || levelData.environmentPrefabs.Count == 0)
            return;

        // Create a parent container for all environments
        GameObject envParent = new GameObject("Environments");
        envParent.transform.parent = this.transform;

        // Instantiate each prefab, parent them under envParent, and disable them
        for (int i = 0; i < levelData.environmentPrefabs.Count; i++)
        {
            GameObject prefab = levelData.environmentPrefabs[i];
            if (prefab == null)
            {
                Debug.LogWarning($"LevelGenerator: environmentPrefabs[{i}] is null—skipping.");
                _instantiatedEnvironments.Add(null);
                continue;
            }

            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, envParent.transform);
            instance.name = prefab.name; // preserve the original name
            instance.SetActive(false);
            _instantiatedEnvironments.Add(instance);
        }

        // Activate the default environment (if index valid)
        int idx = levelData.defaultEnvironmentIndex;
        if (idx >= 0 && idx < _instantiatedEnvironments.Count && _instantiatedEnvironments[idx] != null)
        {
            _instantiatedEnvironments[idx].SetActive(true);
        }
        else if (_instantiatedEnvironments.Count > 0 && _instantiatedEnvironments[0] != null)
        {
            // Fallback: if default index is out of range, activate the first one
            _instantiatedEnvironments[0].SetActive(true);
            Debug.LogWarning(
                $"LevelGenerator: defaultEnvironmentIndex ({idx}) is out of range. " +
                $"Activated environment at index 0 instead."
            );
        }
    }

    private void GenerateBallSpawner()
    {
        GameObject spawnerGO = new GameObject("BallSpawner");
        _ballSpawner = spawnerGO.AddComponent<BallSpawner>();

        GameObject spawnPointGO = new GameObject("SpawnPoint");
        spawnPointGO.transform.parent = spawnerGO.transform;
        spawnPointGO.transform.position = levelData.ballSpawnPosition;

        _ballSpawner.spawnPoint = spawnPointGO.transform;
        _ballSpawner.ballPrefab = levelData.ballPrefab;
        _ballSpawner.respawnDelay = levelData.respawnDelay;
    }

    private void GenerateRespawnZone()
    {
        GameObject zoneGO = Instantiate(
            levelData.respawnZonePrefab,
            levelData.respawnZonePosition,
            Quaternion.identity,
            this.transform
        );

        _respawnZone = zoneGO.GetComponent<BallRespawnZone>();
        if (_respawnZone == null)
        {
            _respawnZone = zoneGO.AddComponent<BallRespawnZone>();
        }

        _respawnZone.spawner = _ballSpawner;
    }

    private List<TargetRing> GenerateRings()
    {
        List<TargetRing> ringList = new List<TargetRing>();

        for (int i = 0; i < levelData.ringPositions.Count; i++)
        {
            Vector3 pos = levelData.ringPositions[i];
            GameObject ringGO = Instantiate(
                levelData.ringPrefab,
                pos,
                Quaternion.identity,
                ringsParent
            );

            TargetRing ring = ringGO.GetComponent<TargetRing>();
            if (ring == null)
            {
                ring = ringGO.AddComponent<TargetRing>();
            }

            ringList.Add(ring);
        }

        return ringList;
    }

    private void GenerateStageManager(List<TargetRing> rings)
    {
        GameObject mgrGO = new GameObject("StageManager");
        _stageManager = mgrGO.AddComponent<ObjectStageManager>();

        _stageManager.targets = rings.ToArray();
        _stageManager.stageTime = levelData.stageTime;
        _stageManager.enableTimer = levelData.enableTimer;

        if (_uiControllerInstance != null)
        {
            _stageManager.uiController = _uiControllerInstance;
        }
        else
        {
            Debug.LogError(
                "LevelGenerator: _uiControllerInstance is null—cannot assign StageUIController to StageManager.\n" +
                "Make sure your uiControllerPrefab has a StageUIController component on itself or a child."
            );
        }
    }

    /// <summary>
    /// Gets the current level data based on the current level index
    /// </summary>
    private ThrowingLevelData GetCurrentLevelData()
    {
        if (!enableLevelProgression)
        {
            return levelData;
        }

        if (levelDatas != null && levelDatas.Count > 0)
        {
            int clampedIndex = Mathf.Clamp(currentLevelIndex, 0, levelDatas.Count - 1);
            return levelDatas[clampedIndex];
        }

        return levelData; // Fallback to single level data
    }

    /// <summary>
    /// Advances to the next level
    /// </summary>
    public void AdvanceToNextLevel()
    {
        if (!enableLevelProgression || levelDatas == null || levelDatas.Count <= 1)
        {
            Debug.LogWarning("Level progression is not enabled or no additional levels available.");
            return;
        }

        currentLevelIndex = (currentLevelIndex + 1) % levelDatas.Count;
        Debug.Log($"Advancing to level {currentLevelIndex + 1}");

        // Reload the scene or regenerate level
        RegenerateLevel();
    }

    /// <summary>
    /// Sets the current level by index
    /// </summary>
    public void SetLevel(int levelIndex)
    {
        if (!enableLevelProgression || levelDatas == null)
        {
            Debug.LogWarning("Level progression is not enabled.");
            return;
        }

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelDatas.Count - 1);
        Debug.Log($"Setting level to {currentLevelIndex + 1}");

        RegenerateLevel();
    }

    /// <summary>
    /// Regenerates the current level
    /// </summary>
    public void RegenerateLevel()
    {
        // Clear existing level elements
        ClearLevel();

        // Regenerate with new level data
        ThrowingLevelData selectedLevelData = GetCurrentLevelData();
        if (selectedLevelData != null)
        {
            levelData = selectedLevelData;
            GenerateEnvironments();
            GenerateBallSpawner();
            GenerateRespawnZone();
            List<TargetRing> rings = GenerateRings();
            GenerateStageManager(rings);
        }
    }

    /// <summary>
    /// Clears all level elements
    /// </summary>
    private void ClearLevel()
    {
        // Clear environments
        foreach (var env in _instantiatedEnvironments)
        {
            if (env != null)
            {
                Destroy(env);
            }
        }
        _instantiatedEnvironments.Clear();

        // Clear other level elements if they exist
        var ringsParentObj = GameObject.Find("Rings");
        if (ringsParentObj != null)
        {
            Destroy(ringsParentObj);
        }

        var ballSpawnerObj = GameObject.Find("BallSpawner");
        if (ballSpawnerObj != null)
        {
            Destroy(ballSpawnerObj);
        }

        var stageManagerObj = GameObject.Find("StageManager");
        if (stageManagerObj != null)
        {
            Destroy(stageManagerObj);
        }
    }

    /// <summary>
    /// Gets the total number of available levels
    /// </summary>
    public int GetTotalLevels()
    {
        if (levelDatas != null && enableLevelProgression)
        {
            return levelDatas.Count;
        }
        return 1;
    }

    /// <summary>
    /// Gets the current level number (1-based)
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }
}
