using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CombatSystem.Events;
using CombatSystem.Drones;

namespace CombatSystem.Spawning
{
    [System.Serializable]
    public class WaveConfiguration
    {
        [Header("Wave Settings")]
        public int waveNumber = 1;
        public float waveDuration = 30f;
        public float restDuration = 10f;
        
        [Header("Spawn Settings")]
        public int maxSimultaneousDrones = 3;
        public float spawnInterval = 2f;
        public float spawnVariation = 0.5f; // ±0.5s variation
        
        [Header("Drone Composition")]
        [Range(0f, 1f)] public float scoutProbability = 0.8f;
        [Range(0f, 1f)] public float heavyProbability = 0.2f;
        
        [Header("Difficulty Modifiers")]
        public float droneSpeedMultiplier = 1f;
        public float attackCooldownMultiplier = 1f;
        public float telegraphTimeMultiplier = 1f;
    }

    public class DroneSpawner : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject scoutDronePrefab;
        [SerializeField] private GameObject heavyDronePrefab;

        [Header("Spawn Configuration")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float spawnDistanceMin = 8f;
        [SerializeField] private float spawnDistanceMax = 12f;
        [SerializeField] private float spawnHeightMin = 2f;
        [SerializeField] private float spawnHeightMax = 4f;
        [SerializeField] private int maxSpawnAttempts = 10;

        [Header("Wave System")]
        [SerializeField] private WaveConfiguration[] waves;
        [SerializeField] private bool autoStartWaves = true;
        [SerializeField] private float initialDelay = 3f;

        [Header("State Management")]
        [SerializeField] private bool infiniteWaves = true;
        [SerializeField] private float difficultyIncreasePerWave = 0.1f;

        // Runtime state
        private List<DroneController> activeDrones = new List<DroneController>();
        private int currentWaveIndex = 0;
        private bool isSpawning = false;
        private bool isInWave = false;
        private bool isResting = false;
        private Coroutine waveCoroutine;
        private Coroutine spawnCoroutine;

        // Properties
        public int CurrentWave => currentWaveIndex + 1;
        public int ActiveDroneCount => activeDrones.Count;
        public bool IsInWave => isInWave;
        public bool IsResting => isResting;
        public WaveConfiguration CurrentWaveConfig => 
            waves != null && currentWaveIndex < waves.Length ? waves[currentWaveIndex] : null;

        void Start()
        {
            InitializeSpawner();
            
            if (autoStartWaves)
            {
                StartCoroutine(DelayedWaveStart());
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeSpawner()
        {
            // Find player if not assigned
            if (playerTransform == null)
            {
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    playerTransform = xrOrigin.Camera.transform;
                }
                else if (Camera.main != null)
                {
                    playerTransform = Camera.main.transform;
                }
            }

            // Subscribe to events
            CombatEvents.OnDroneDestroyed += HandleDroneDestroyed;
            
            // Validate prefabs
            if (scoutDronePrefab == null)
                Debug.LogWarning("DroneSpawner: Scout drone prefab not assigned!");
            if (heavyDronePrefab == null)
                Debug.LogWarning("DroneSpawner: Heavy drone prefab not assigned!");

            // Create default wave if none configured
            if (waves == null || waves.Length == 0)
            {
                CreateDefaultWaves();
            }
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnDroneDestroyed -= HandleDroneDestroyed;
        }

        private void CreateDefaultWaves()
        {
            waves = new WaveConfiguration[]
            {
                new WaveConfiguration
                {
                    waveNumber = 1,
                    waveDuration = 30f,
                    restDuration = 10f,
                    maxSimultaneousDrones = 2,
                    spawnInterval = 3f,
                    scoutProbability = 1f,
                    heavyProbability = 0f
                },
                new WaveConfiguration
                {
                    waveNumber = 2,
                    waveDuration = 45f,
                    restDuration = 15f,
                    maxSimultaneousDrones = 3,
                    spawnInterval = 2.5f,
                    scoutProbability = 0.8f,
                    heavyProbability = 0.2f
                },
                new WaveConfiguration
                {
                    waveNumber = 3,
                    waveDuration = 60f,
                    restDuration = 20f,
                    maxSimultaneousDrones = 4,
                    spawnInterval = 2f,
                    scoutProbability = 0.6f,
                    heavyProbability = 0.4f
                }
            };
        }

        private IEnumerator DelayedWaveStart()
        {
            yield return new WaitForSeconds(initialDelay);
            StartNextWave();
        }

        public void StartNextWave()
        {
            if (isInWave || isResting) return;

            if (waveCoroutine != null)
                StopCoroutine(waveCoroutine);

            waveCoroutine = StartCoroutine(WaveSequence());
        }

        private IEnumerator WaveSequence()
        {
            WaveConfiguration config = GetCurrentWaveConfig();
            if (config == null)
            {
                Debug.LogError("DroneSpawner: No wave configuration available!");
                yield break;
            }

            Debug.Log($"Starting Wave {CurrentWave}");

            // Wave start
            isInWave = true;
            isResting = false;

            // Start spawning
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnLoop(config));

            // Wait for wave duration
            yield return new WaitForSeconds(config.waveDuration);

            // Stop spawning new drones
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            isSpawning = false;

            // Wait for remaining drones to be cleared or timeout
            float clearanceWaitTime = 10f;
            float elapsedTime = 0f;
            while (activeDrones.Count > 0 && elapsedTime < clearanceWaitTime)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Force clear remaining drones
            ClearAllDrones();

            // Wave complete
            isInWave = false;
            isResting = true;

            Debug.Log($"Wave {CurrentWave} completed. Rest for {config.restDuration}s");

            // Rest period
            yield return new WaitForSeconds(config.restDuration);

            isResting = false;

            // Advance to next wave
            currentWaveIndex++;
            
            // Check if we should continue
            if (infiniteWaves || currentWaveIndex < waves.Length)
            {
                // Loop back to first wave if infinite and we've reached the end
                if (infiniteWaves && currentWaveIndex >= waves.Length)
                {
                    currentWaveIndex = 0;
                }
                
                StartNextWave();
            }
            else
            {
                Debug.Log("All waves completed!");
                // Could trigger end game sequence here
            }
        }

        private IEnumerator SpawnLoop(WaveConfiguration config)
        {
            isSpawning = true;

            while (isSpawning)
            {
                // Check if we can spawn more drones
                if (activeDrones.Count < config.maxSimultaneousDrones)
                {
                    SpawnRandomDrone(config);
                }

                // Wait for next spawn with variation
                float waitTime = config.spawnInterval + Random.Range(-config.spawnVariation, config.spawnVariation);
                waitTime = Mathf.Max(0.5f, waitTime); // Minimum spawn interval
                
                yield return new WaitForSeconds(waitTime);
            }
        }

        private void SpawnRandomDrone(WaveConfiguration config)
        {
            // Determine drone type
            DroneType droneType = Random.value <= config.scoutProbability ? DroneType.Scout : DroneType.Heavy;
            GameObject prefabToSpawn = droneType == DroneType.Scout ? scoutDronePrefab : heavyDronePrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"DroneSpawner: No prefab assigned for {droneType} drone!");
                return;
            }

            // Find spawn position
            Vector3 spawnPosition = FindValidSpawnPosition();
            if (spawnPosition == Vector3.zero)
            {
                Debug.LogWarning("DroneSpawner: Could not find valid spawn position!");
                return;
            }

            // Spawn drone
            GameObject droneObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            DroneController drone = droneObj.GetComponent<DroneController>();
            
            if (drone != null)
            {
                // Apply wave difficulty modifiers
                ApplyWaveDifficulty(drone, config);
                
                // Track drone
                activeDrones.Add(drone);
                
                // Notify systems
                CombatEvents.OnDroneSpawned?.Invoke(drone);
                CombatEvents.OnActiveDronesCountChanged?.Invoke(activeDrones.Count);
                
                Debug.Log($"Spawned {droneType} drone at {spawnPosition}");
            }
            else
            {
                Debug.LogError("DroneSpawner: Spawned object does not have DroneController component!");
                Destroy(droneObj);
            }
        }

        private Vector3 FindValidSpawnPosition()
        {
            if (playerTransform == null) return Vector3.zero;

            Vector3 playerPos = playerTransform.position;

            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                // Random angle around player
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
                float height = Random.Range(spawnHeightMin, spawnHeightMax);

                Vector3 spawnPos = playerPos + new Vector3(
                    Mathf.Cos(angle) * distance,
                    height,
                    Mathf.Sin(angle) * distance
                );

                // Check if position is clear (basic check)
                if (!Physics.CheckSphere(spawnPos, 1f))
                {
                    return spawnPos;
                }
            }

            // Fallback: spawn at a basic position
            return playerPos + Vector3.forward * spawnDistanceMin + Vector3.up * spawnHeightMin;
        }

        private void ApplyWaveDifficulty(DroneController drone, WaveConfiguration config)
        {
            // Apply difficulty scaling based on wave number
            float difficultyMultiplier = 1f + (currentWaveIndex * difficultyIncreasePerWave);
            
            // Note: This would require exposing properties on DroneController
            // For now, we'll just track that difficulty should be applied
            Debug.Log($"Applied difficulty multiplier {difficultyMultiplier} to drone");
        }

        private WaveConfiguration GetCurrentWaveConfig()
        {
            if (waves == null || waves.Length == 0) return null;
            
            int index = currentWaveIndex;
            if (infiniteWaves && index >= waves.Length)
            {
                index = index % waves.Length; // Loop back
            }
            
            return index < waves.Length ? waves[index] : null;
        }

        private void HandleDroneDestroyed(DroneController drone)
        {
            if (activeDrones.Contains(drone))
            {
                activeDrones.Remove(drone);
                CombatEvents.OnActiveDronesCountChanged?.Invoke(activeDrones.Count);
            }
        }

        public void ClearAllDrones()
        {
            for (int i = activeDrones.Count - 1; i >= 0; i--)
            {
                if (activeDrones[i] != null)
                {
                    activeDrones[i].DestroyDrone();
                }
            }
            activeDrones.Clear();
            CombatEvents.OnActiveDronesCountChanged?.Invoke(0);
        }

        public void StopAllWaves()
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }
            
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            isSpawning = false;
            isInWave = false;
            isResting = false;
            
            ClearAllDrones();
        }

        // Public API for external control
        public void ForceNextWave()
        {
            StopAllWaves();
            currentWaveIndex++;
            if (infiniteWaves && currentWaveIndex >= waves.Length)
                currentWaveIndex = 0;
            StartNextWave();
        }

        public void RestartWaves()
        {
            StopAllWaves();
            currentWaveIndex = 0;
            StartCoroutine(DelayedWaveStart());
        }

        void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;

            Vector3 playerPos = playerTransform.position;
            
            // Draw spawn range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerPos, spawnDistanceMin);
            Gizmos.DrawWireSphere(playerPos, spawnDistanceMax);
            
            // Draw height range
            Gizmos.color = Color.cyan;
            Vector3 minHeightPos = playerPos + Vector3.up * spawnHeightMin;
            Vector3 maxHeightPos = playerPos + Vector3.up * spawnHeightMax;
            Gizmos.DrawWireCube(minHeightPos, Vector3.one * 0.5f);
            Gizmos.DrawWireCube(maxHeightPos, Vector3.one * 0.5f);
        }
    }
}
