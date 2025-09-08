// CombatSystem/Spawning/DroneSpawner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CombatSystem.Drones;
using CombatSystem.Events;
using CombatSystem.Portals;

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
        public float spawnVariation = 0.5f;

        [Header("Composition")]
        [Range(0f, 1f)] public float scoutProbability = 0.8f;
        [Range(0f, 1f)] public float heavyProbability = 0.2f;

        [Header("Difficulty Modifiers (Multipliers)")]
        public float droneSpeedMultiplier = 1f;
        public float attackCooldownMultiplier = 1f;
        public float telegraphDurationMultiplier = 1f;
        public float dashSpeedMultiplier = 1f;
    }

    /// <summary>
    /// Wave manager + object pool + portal/fallback spawning.
    /// Fires wave events so PortalController can react.
    /// </summary>
    public class DroneSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private DroneController scoutPrefab;
        [SerializeField] private DroneController heavyPrefab;

        [Header("Portal")]
        [SerializeField] private PortalController portalController;
        [Range(0f, 1f)][SerializeField] private float portalSpawnChance = 0.7f;

        [Header("Fallback Spawn")]
        [SerializeField] private Transform[] fallbackSpawns;
        [SerializeField] private bool oneShotSpawn = true;               // فقط یک اسپاون در هر موج
        [SerializeField] private bool openPortalPerSpawn = true;         // قبل از اسپاون پورتال را باز کن
        [SerializeField] private bool closePortalImmediately = true;     // بلافاصله بعد از اسپاون ببند

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Limits")]
        [SerializeField] private int poolSizePerType = 6;
        [SerializeField] private float minDistanceBetweenDrones = 1.4f;

        [Header("Waves")]
        [SerializeField] private List<WaveConfiguration> waves = new List<WaveConfiguration>();
        [SerializeField] private bool autoStart = true; // NEW: auto-start waves at Start

        // ===== Runtime =====
        private readonly List<DroneController> activeDrones = new List<DroneController>();
        private readonly Queue<DroneController> scoutPool = new Queue<DroneController>();
        private readonly Queue<DroneController> heavyPool = new Queue<DroneController>();
        private Coroutine wavesRoutine;

        private void Start()
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

            // Find player transform if not assigned
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

            WarmupPool(scoutPrefab, scoutPool);
            WarmupPool(heavyPrefab, heavyPool);

            // If user forgot to add waves, add a safe default so something happens.
            if (waves.Count == 0)
            {
                waves.Add(new WaveConfiguration
                {
                    waveNumber = 1,
                    waveDuration = 25f,
                    restDuration = 6f,
                    maxSimultaneousDrones = 3,
                    spawnInterval = 2.0f,
                    spawnVariation = 0.4f,
                    scoutProbability = 0.85f,
                    heavyProbability = 0.15f,
                    droneSpeedMultiplier = 1f,
                    attackCooldownMultiplier = 1f,
                    telegraphDurationMultiplier = 1f,
                    dashSpeedMultiplier = 1f
                });
            }

            if (autoStart) RestartWaves();
        }

        private void OnDisable()
        {
            if (wavesRoutine != null) StopCoroutine(wavesRoutine);
        }
        private IEnumerator EnsurePortalOpen()
        {
            if (portalController == null) yield break;
            if (!portalController.IsOpen) portalController.OpenPortal();
            yield return new WaitUntil(() => portalController.IsOpen);
        }
        private IEnumerator EnsurePortalClosed()
        {
            if (portalController == null) yield break;
            portalController.ClosePortal();
            yield return new WaitUntil(() => portalController.CurrentState == PortalState.Closed);
        }

        // ===== Public API (used by SquatGameManager) =====
        public void RestartWaves()
        {
            StopAllWaves();
            if (waves.Count > 0)
                wavesRoutine = StartCoroutine(RunWaves());
        }

        public void StopAllWaves()
        {
            if (wavesRoutine != null)
            {
                StopCoroutine(wavesRoutine);
                wavesRoutine = null;
            }
            DespawnAll();
            // End signal in case a controller listens for global wave stop
            CombatEvents.OnWaveEnded?.Invoke();
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                var cfg = waves[i];

                if (oneShotSpawn)
                {
                    // 🟢 فقط برای اسپاون، پورتال باز/بسته شود
                    if (openPortalPerSpawn && portalController != null)
                        yield return EnsurePortalOpen();

                    yield return SpawnRandomDrone(cfg, forcePortal: true); // اجباری از پورتال

                    if (closePortalImmediately && portalController != null)
                        yield return EnsurePortalClosed();

                    // استراحت بین موج‌ها
                    yield return new WaitForSeconds(Mathf.Max(0f, cfg.restDuration));
                    continue;
                }

                // --- جریان قدیمیِ چند اسپاون در طول مدت موج (در صورت نیاز نگه‌دار) ---
                // CombatEvents.OnWaveStarted?.Invoke();
                float waveEnd = Time.time + cfg.waveDuration;
                float nextSpawn = 0f;

                while (Time.time < waveEnd)
                {
                    if (activeDrones.Count < Mathf.Max(1, cfg.maxSimultaneousDrones) && Time.time >= nextSpawn)
                    {
                        yield return SpawnRandomDrone(cfg);
                        float delay = Mathf.Max(0.05f, cfg.spawnInterval + Random.Range(-cfg.spawnVariation, cfg.spawnVariation));
                        nextSpawn = Time.time + delay;
                    }
                    yield return null;
                }

                // CombatEvents.OnWaveEnded?.Invoke();
                yield return new WaitForSeconds(Mathf.Max(0f, cfg.restDuration));
            }

            wavesRoutine = null;
        }


private IEnumerator SpawnRandomDrone(WaveConfiguration config, bool forcePortal = false)
{
    DroneController prefab = (Random.value <= config.scoutProbability) ? scoutPrefab : heavyPrefab;
    if (prefab == null) yield break;

    bool usePortal = portalController != null && (forcePortal || (portalController.IsOpen && Random.value <= portalSpawnChance));
    Vector3 spawnPos;
    Transform portalPoint = null;
    int portalIndex = -1;

    if (usePortal && portalController.SpawnPoints != null && portalController.SpawnPoints.Length > 0)
    {
        var candidates = new List<int>();
        for (int i = 0; i < portalController.SpawnPoints.Length; i++)
        {
            var sp = portalController.SpawnPoints[i];
            if (sp != null && sp.isActive && sp.transform != null)
                candidates.Add(i);
        }

        if (candidates.Count > 0)
        {
            portalIndex = candidates[Random.Range(0, candidates.Count)];
            portalPoint = portalController.SpawnPoints[portalIndex].transform;
            spawnPos = portalPoint.position;
        }
        else
        {
            portalIndex = Random.Range(0, portalController.SpawnPoints.Length);
            portalPoint = portalController.SpawnPoints[portalIndex].transform;
            spawnPos = portalPoint.position;
            usePortal = true;
        }

        // For portal spawns, ignore distance checks - spawn at exact portal position
        Debug.Log($"Portal spawn at: {spawnPos} (portal index: {portalIndex})");
    }
    else
    {
        spawnPos = GetFallbackSpawnPosition();
        usePortal = false;
        
        // Only check distance for fallback spawns
        if (!IsFarEnoughFromOthers(spawnPos, minDistanceBetweenDrones))
        {
            spawnPos = GetFallbackSpawnPosition();
        }
    }

    DroneController drone = GetFromPool(prefab);
    drone.transform.position = spawnPos;

    if (drone.player == null && playerTransform != null)
        drone.player = playerTransform;

    // Initial direction
    Vector3 lookAt = (portalPoint != null ? portalPoint.position + portalPoint.forward : spawnPos + Vector3.forward);
    Vector3 dir = (lookAt - spawnPos); dir.y = 0f;
    if (dir.sqrMagnitude > 0.0001f)
        drone.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

    drone.OnSpawned();

    ApplyWaveDifficulty(drone, config);

    activeDrones.Add(drone);
    CombatEvents.OnDroneSpawned?.Invoke(drone);
    CombatEvents.OnActiveDronesCountChanged?.Invoke(activeDrones.Count);

    if (usePortal && portalIndex >= 0 && portalPoint != null)
    {
        portalController.TriggerSpawnEffect(portalIndex);
        drone.PlayPortalEntry(portalPoint);
        Debug.Log($"Playing portal entry for drone at: {drone.transform.position}");
    }
    else
    {
        Debug.Log($"Fallback spawn for drone at: {drone.transform.position}");
    }

    System.Action<DroneController> onDestroyed = null;
    onDestroyed = (d) =>
    {
        if (d == drone)
        {
            activeDrones.Remove(drone);
            CombatEvents.OnActiveDronesCountChanged?.Invoke(activeDrones.Count);
            CombatEvents.OnDroneDestroyed -= onDestroyed;
        }
    };
    CombatEvents.OnDroneDestroyed += onDestroyed;

    yield return null;
}
        // ================== Positions ==================
        private Vector3 GetFallbackSpawnPosition()
        {
            if (fallbackSpawns != null && fallbackSpawns.Length > 0)
            {
                var t = fallbackSpawns[Random.Range(0, fallbackSpawns.Length)];
                if (t != null) return t.position + Vector3.up * 1.6f;
            }

            Vector3 basePos = transform.position;
            float dist = Random.Range(2.2f, 4.0f);
            Vector2 jitter = Random.insideUnitCircle * 1.5f;

            Vector3 pos = basePos + transform.forward * dist + new Vector3(jitter.x, 0f, jitter.y);
            pos.y = basePos.y + 1.6f;
            return pos;
        }

        private bool IsFarEnoughFromOthers(Vector3 pos, float minDistance)
        {
            for (int i = 0; i < activeDrones.Count; i++)
            {
                var d = activeDrones[i];
                if (d == null) continue;
                if (Vector3.Distance(pos, d.transform.position) < minDistance) return false;
            }
            return true;
        }

        // ================== Difficulty ==================
        private void ApplyWaveDifficulty(DroneController drone, WaveConfiguration cfg)
        {
            var t = drone.GetType();
            TrySetField(drone, t, "_wanderSpeed_Internal", Mathf.Max(0.5f, cfg.droneSpeedMultiplier));
            TrySetField(drone, t, "_dashSpeed_Internal", Mathf.Max(0.5f, cfg.dashSpeedMultiplier));
            TrySetField(drone, t, "_attackCooldown_Internal", Mathf.Max(0.25f, cfg.attackCooldownMultiplier));
            TrySetField(drone, t, "_telegraphDuration_Internal", Mathf.Max(0.4f, cfg.telegraphDurationMultiplier));
        }

        private void TrySetField(DroneController d, System.Type t, string field, float value)
        {
            var f = t.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(d, value);
        }

        // ================== Pool ==================
        private void WarmupPool(DroneController prefab, Queue<DroneController> pool)
        {
            if (prefab == null) return;
            for (int i = 0; i < poolSizePerType; i++)
            {
                var inst = Instantiate(prefab, transform.position, Quaternion.identity);
                inst.gameObject.SetActive(false);
                inst.SetPool(ReturnToPool);
                pool.Enqueue(inst);
            }
        }

        private DroneController GetFromPool(DroneController prefab)
        {
            var pool = (prefab == scoutPrefab) ? scoutPool : heavyPool;
            DroneController d = null;
            while (pool.Count > 0 && d == null)
            {
                d = pool.Dequeue();
            }

            if (d == null)
            {
                d = Instantiate(prefab, transform.position, Quaternion.identity);
                d.SetPool(ReturnToPool);
            }

            d.gameObject.SetActive(true); // ensure enabled
            return d;
        }

        private void ReturnToPool(IPoolable p)
        {
            var d = p as DroneController;
            if (d == null) return;

            d.OnDespawned();
            d.gameObject.SetActive(false);

            if (d.type == DroneType.Scout) scoutPool.Enqueue(d);
            else heavyPool.Enqueue(d);

            activeDrones.Remove(d);
            CombatEvents.OnActiveDronesCountChanged?.Invoke(activeDrones.Count);
        }

        // ================== Debug / Utilities ==================
        [ContextMenu("Despawn All")]
        private void DespawnAll()
        {
            var snapshot = new List<DroneController>(activeDrones);
            foreach (var d in snapshot)
            {
                if (d == null) continue;
                ReturnToPool(d);
            }
            activeDrones.Clear();
            CombatEvents.OnActiveDronesCountChanged?.Invoke(0);
        }

        [ContextMenu("Spawn Test Drone")]
        private void DebugSpawnDrone()
        {
            if (waves.Count > 0 && portalController != null && portalController.IsOpen)
            {
                StartCoroutine(SpawnRandomDrone(waves[0]));
                Debug.Log("Debug spawning drone through portal");
            }
            else
            {
                Debug.LogWarning("Cannot spawn: no waves configured or portal not open");
            }
        }
    }
}
