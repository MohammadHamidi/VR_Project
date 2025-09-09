// CombatSystem/Portals/PortalController.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using CombatSystem.Events;

namespace CombatSystem.Portals
{
    public enum PortalState { Closed = 0, Opening = 1, Open = 2, Closing = 3 }

    [System.Serializable]
    public class SpawnPoint
    {
        public Transform transform;
        public bool isActive;
        public float activationTime;

        [Header("Visual Effects")]
        public ParticleSystem spawnEffect;
        public Light glowLight;
        public GameObject spawnModel;   // scaled for open/close
    }

    /// <summary>
    /// Controls portal shader state and manages spawn-point visuals (scale/FX).
    /// Uses a *runtime* material instance to keep shader properties in sync with the visible renderer.
    /// </summary>
    public class PortalController : MonoBehaviour
    {
        [Header("Portal References")]
        [SerializeField] private Material portalMaterial;     // Source asset (optional; can be assigned in Inspector)
        [SerializeField] private GameObject quadPortalPrefab; // Optional prefab if no "Quad" found
        [SerializeField] private ParticleSystem portalEffect;
        [SerializeField] private AudioSource audioSource;

        [Header("Portal Audio")]
        [SerializeField] private AudioClip openingSound;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closingSound;
        [SerializeField] private AudioClip ambientHum;

        [Header("Portal Timing")]
        [SerializeField] private float openingDuration = 2f;
        [SerializeField] private float closingDuration = 1.5f;

        [Header("Spawn Points")]
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private bool autoGenerateSpawnPoints = true;
        [SerializeField] private GameObject spawnPointPrefab;
        [SerializeField] private int autoSpawnPointCount = 4;
        [SerializeField] private float spawnPointRadius = 3f;
        [SerializeField] private float spawnPointActivationDelay = 0.2f;

        [Header("SpawnPoint Visual Scaling")]
        [SerializeField] private float spOpenScale = 2f;
        [SerializeField] private float spScaleUpDuration = 0.35f;
        [SerializeField] private float spScaleDownDuration = 0.30f;
        [SerializeField] private Ease spScaleUpEase = Ease.OutBack;
        [SerializeField] private Ease spScaleDownEase = Ease.InQuad;
        [SerializeField] private bool deactivateSPOnClosed = true;

        [Header("Integration")]
        [SerializeField] private bool openOnWaveStart = true;
        [SerializeField] private bool closeOnWaveEnd = true;

        // Runtime state
        private PortalState currentState = PortalState.Closed;
        private Coroutine stateCoroutine;
        private Coroutine spawnPointCoroutine;
        private bool isTransitioning = false;
        private float stateTransition = 0f;

        // Shader property IDs
        private int portalStateID;
        private int stateTransitionID;

        // Tween sequences
        private Sequence openingSequence;
        private Sequence closingSequence;

        // Renderer + runtime material (CRITICAL FIX)
        private Renderer portalRenderer;
        private Material runtimeMaterial;

        public PortalState CurrentState => currentState;
        public bool IsOpen => currentState == PortalState.Open;
        public SpawnPoint[] SpawnPoints => spawnPoints;

        private void Awake()
        {
            portalStateID = Shader.PropertyToID("_PortalState");
            stateTransitionID = Shader.PropertyToID("_StateTransition");

            // Audio
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f;
                    audioSource.playOnAwake = false;
                }
            }

            // Find or create the Quad
            var quadPortal = GameObject.Find("Quad");
            if (quadPortal == null && quadPortalPrefab != null)
            {
                quadPortal = Instantiate(quadPortalPrefab, transform.position, transform.rotation);
                quadPortal.name = "Quad";
                Debug.Log("Instantiated Quad portal prefab");
            }

            // Pick a renderer to drive (prefer the Quad's)
            if (quadPortal != null)
                portalRenderer = quadPortal.GetComponent<Renderer>();
            else
                portalRenderer = GetComponent<Renderer>();

            // Resolve portal material (from Inspector, Resources, or by name)
            if (portalMaterial == null)
            {
                // Try Resources/Shaders/Portal.mat (optional)
                var resMat = Resources.Load<Material>("Shaders/Portal");
                if (resMat != null) portalMaterial = resMat;

                // Fallback: search by name in loaded assets
                if (portalMaterial == null)
                {
                    var found = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "Portal");
                    if (found != null) portalMaterial = found;
                }
            }

            // Assign a runtime instance to the visible renderer
            if (portalRenderer != null && portalMaterial != null)
            {
                portalRenderer.material = Instantiate(portalMaterial);
                runtimeMaterial = portalRenderer.material;
                Debug.Log("Portal material applied to portal renderer (runtime instance)");
            }
            else
            {
                if (portalRenderer == null)
                    Debug.LogWarning("PortalController: No Renderer found for portal!");
                if (portalMaterial == null)
                    Debug.LogWarning("PortalController: Portal material not found! Assign it in Inspector or place it under Resources/Shaders/Portal.mat");
            }

            // If the Quad has a visual controller, ensure its target is set
            if (quadPortal != null)
            {
                var spawnController = quadPortal.GetComponent<SpawnPointVisualController>();
                if (spawnController != null && spawnController.target == null)
                {
                    spawnController.target = quadPortal.transform;
                }
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnWaveStarted += HandleWaveStarted;
            CombatEvents.OnWaveEnded += HandleWaveEnded;
        }

        private void OnDisable()
        {
            CombatEvents.OnWaveStarted -= HandleWaveStarted;
            CombatEvents.OnWaveEnded -= HandleWaveEnded;
            KillAllTweens();
        }

        private void Start()
        {
            // Auto-create points if none assigned
            if (autoGenerateSpawnPoints && (spawnPoints == null || spawnPoints.Length == 0))
                GenerateSpawnPoints();

            // Ensure closed visuals initially
            SetPortalState(PortalState.Closed, true);
            SetAllSpawnPointsClosedImmediate();
            UpdateMaterialProperties();

            Debug.Log($"PortalController initialized with {spawnPoints?.Length ?? 0} spawn points, state: {currentState}");
        }

        // =========================================================
        //                      PUBLIC  API
        // =========================================================

        public void OpenPortal(float duration = -1f)
        {
            if (isTransitioning && currentState == PortalState.Opening) return;

            float openDuration = duration > 0 ? duration : openingDuration;
            SetPortalState(PortalState.Opening);

            if (stateCoroutine != null) StopCoroutine(stateCoroutine);
            stateCoroutine = StartCoroutine(OpeningSequence(openDuration));
        }

        public void ClosePortal(float duration = -1f)
        {
            if (isTransitioning && currentState == PortalState.Closing) return;

            float closeDuration = duration > 0 ? duration : closingDuration;
            SetPortalState(PortalState.Closing);

            if (stateCoroutine != null) StopCoroutine(stateCoroutine);
            stateCoroutine = StartCoroutine(ClosingSequence(closeDuration));
        }

        /// <summary>
        /// Called by the spawner right when it spawns at a specific spawn point index.
        /// Plays the assigned particle and a small scale pulse on the spawn model.
        /// </summary>
        public void TriggerSpawnEffect(int index)
        {
            if (spawnPoints == null || index < 0 || index >= spawnPoints.Length) return;
            var sp = spawnPoints[index];
            if (sp == null) return;

            // particles
            if (sp.spawnEffect != null) sp.spawnEffect.Play(true);

            // quick visual pulse
            if (sp.spawnModel != null)
            {
                Transform t = sp.spawnModel.transform;
                t.DOKill();
                Vector3 baseScale = Vector3.one * spOpenScale;
                t.localScale = baseScale;
                t.DOPunchScale(Vector3.one * 0.25f, 0.25f, vibrato: 6, elasticity: 0.8f);
            }
        }

        /// <summary>
        /// Uses existing spawn points from the Quad prefab instead of generating new ones.
        /// NOTE: We do NOT attach SpawnPointVisualController to spheres to avoid double-scaling conflicts.
        /// </summary>
        public void GenerateSpawnPoints()
        {
            var quadPortal = GameObject.Find("Quad");
            var list = new List<SpawnPoint>();

            if (quadPortal != null)
            {
                // Find all child spheres in the Quad prefab
                var spheres = quadPortal.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.Contains("Sphere") && t != quadPortal.transform)
                    .ToArray();

                Debug.Log($"Found {spheres.Length} sphere spawn points in Quad prefab");

                foreach (var sphereTransform in spheres)
                {
                    var sphereGO = sphereTransform.gameObject;

                    // Try to find optional components
                    var ps = sphereGO.GetComponentInChildren<ParticleSystem>(true);
                    var light = sphereGO.GetComponentInChildren<Light>(true);

                    var sp = new SpawnPoint
                    {
                        transform = sphereTransform,
                        spawnModel = sphereGO,
                        spawnEffect = ps,
                        glowLight = light,
                        isActive = false,
                        activationTime = 0f
                    };

                    // Ensure default closed state
                    sphereTransform.localScale = Vector3.zero;
                    if (light != null) light.intensity = 0f;

                    list.Add(sp);
                }
            }
            else
            {
                Debug.LogWarning("Quad portal not found! Cannot set up spawn points.");
                // Fallback: create basic spawn points
                for (int i = 0; i < autoSpawnPointCount; i++)
                {
                    var fallbackGO = new GameObject($"FallbackSpawnPoint_{i}");
                    fallbackGO.transform.SetParent(transform);
                    fallbackGO.transform.localPosition = Vector3.zero;
                    fallbackGO.transform.localScale = Vector3.zero;

                    var sp = new SpawnPoint
                    {
                        transform = fallbackGO.transform,
                        spawnModel = fallbackGO,
                        spawnEffect = null,
                        glowLight = null,
                        isActive = false,
                        activationTime = 0f
                    };
                    list.Add(sp);
                }
            }

            spawnPoints = list.ToArray();
            Debug.Log($"Set up {list.Count} spawn points from Quad prefab");
        }

        // =========================================================
        //                    OPEN/CLOSE SEQUENCES
        // =========================================================

        private IEnumerator OpeningSequence(float duration)
        {
            Debug.Log($"Portal opening sequence started with duration {duration}");
            isTransitioning = true;

            // 🔔 Broadcast "opening"
            CombatEvents.OnPortalOpening?.Invoke();

            if (audioSource && openingSound) audioSource.PlayOneShot(openingSound);

            openingSequence?.Kill();
            openingSequence = DOTween.Sequence();

            openingSequence
                .Append(DOTween.To(() => stateTransition, x =>
                {
                    stateTransition = x;
                    UpdateMaterialProperties();
                }, 1f, duration).SetEase(Ease.OutQuart))
                .OnComplete(() =>
                {
                    SetPortalState(PortalState.Open);
                    isTransitioning = false;
                    Debug.Log("Portal opening sequence completed");

                    if (audioSource && ambientHum)
                    {
                        audioSource.clip = ambientHum;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                    if (openSound) audioSource.PlayOneShot(openSound);

                    // 🔔 Broadcast "opened"
                    CombatEvents.OnPortalOpened?.Invoke();
                });

            if (spawnPointCoroutine != null) StopCoroutine(spawnPointCoroutine);
            spawnPointCoroutine = StartCoroutine(ActivateSpawnPointsWithScale());

            yield return openingSequence.WaitForCompletion();
        }

        private IEnumerator ClosingSequence(float duration)
        {
            isTransitioning = true;

            // 🔔 Broadcast "closing"
            CombatEvents.OnPortalClosing?.Invoke();

            if (audioSource && audioSource.isPlaying && audioSource.clip == ambientHum) audioSource.Stop();
            if (audioSource && closingSound) audioSource.PlayOneShot(closingSound);

            // scale down SPs first
            yield return StartCoroutine(DeactivateAllSpawnPointsWithScale());

            closingSequence?.Kill();
            closingSequence = DOTween.Sequence();

            closingSequence
                .Append(DOTween.To(() => stateTransition, x =>
                {
                    stateTransition = x;
                    UpdateMaterialProperties();
                }, 0f, duration).SetEase(Ease.InQuart))
                .OnComplete(() =>
                {
                    SetPortalState(PortalState.Closed);
                    isTransitioning = false;

                    // 🔔 Broadcast "closed"
                    CombatEvents.OnPortalClosed?.Invoke();
                });

            yield return closingSequence.WaitForCompletion();
        }

        // =========================================================
        //                  SPAWN POINT HELPERS
        // =========================================================

        private IEnumerator ActivateSpawnPointsWithScale()
        {
            if (spawnPoints == null) yield break;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                ActivateSpawnPoint(i);
                yield return new WaitForSeconds(spawnPointActivationDelay);
            }
        }

        private void ActivateSpawnPoint(int index)
        {
            if (spawnPoints == null || index < 0 || index >= spawnPoints.Length) return;

            var point = spawnPoints[index];
            point.isActive = true;
            point.activationTime = Time.time;

            if (point.spawnModel != null)
            {
                if (deactivateSPOnClosed && !point.spawnModel.activeSelf)
                    point.spawnModel.SetActive(true);

                point.spawnModel.transform.DOKill();
                point.spawnModel.transform.localScale = Vector3.zero;
                point.spawnModel.transform.DOScale(spOpenScale, spScaleUpDuration).SetEase(spScaleUpEase);
            }

            if (point.spawnEffect != null) point.spawnEffect.Play();

            if (point.glowLight != null)
                point.glowLight.DOIntensity(2f, 0.3f).SetEase(Ease.OutQuart);
        }

        private IEnumerator DeactivateAllSpawnPointsWithScale()
        {
            if (spawnPoints == null) yield break;

            var tweeners = new List<Tweener>();
            foreach (var point in spawnPoints)
            {
                point.isActive = false;
                if (point.spawnEffect != null) point.spawnEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (point.glowLight != null) point.glowLight.DOIntensity(0f, 0.2f);

                if (point.spawnModel != null)
                {
                    point.spawnModel.transform.DOKill();
                    var t = point.spawnModel.transform
                        .DOScale(0f, spScaleDownDuration)
                        .SetEase(spScaleDownEase)
                        .OnComplete(() =>
                        {
                            if (deactivateSPOnClosed) point.spawnModel.SetActive(false);
                        });
                    tweeners.Add(t);
                }
            }

            foreach (var t in tweeners)
                yield return t.WaitForCompletion();
        }

        private void SetAllSpawnPointsClosedImmediate()
        {
            if (spawnPoints == null) return;
            foreach (var p in spawnPoints)
            {
                p.isActive = false;
                if (p.spawnEffect != null) p.spawnEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (p.glowLight != null) p.glowLight.intensity = 0f;

                if (p.spawnModel != null)
                {
                    p.spawnModel.transform.DOKill();
                    p.spawnModel.transform.localScale = Vector3.zero;
                    if (deactivateSPOnClosed) p.spawnModel.SetActive(false);
                }
            }
        }

        // =========================================================
        //                         CORE
        // =========================================================

        private void SetPortalState(PortalState newState, bool immediate = false)
        {
            currentState = newState;

            if (immediate)
            {
                stateTransition = newState == PortalState.Open ? 1f : 0f;
                isTransitioning = false;

                if (newState == PortalState.Closed)
                    SetAllSpawnPointsClosedImmediate();
                else if (newState == PortalState.Open)
                    ForceAllSpawnPointsOpenImmediate();
            }

            UpdateMaterialProperties();
        }

        private void ForceAllSpawnPointsOpenImmediate()
        {
            if (spawnPoints == null) return;
            foreach (var p in spawnPoints)
            {
                if (deactivateSPOnClosed && p.spawnModel != null && !p.spawnModel.activeSelf)
                    p.spawnModel.SetActive(true);

                if (p.spawnModel != null) p.spawnModel.transform.localScale = Vector3.one * spOpenScale;
                if (p.glowLight != null) p.glowLight.intensity = 2f;
                p.isActive = true;
            }
        }

        private void UpdateMaterialProperties()
        {
            if (runtimeMaterial == null) return; // ✅ به‌جای portalMaterial
            runtimeMaterial.SetFloat(portalStateID, (float)currentState);
            runtimeMaterial.SetFloat(stateTransitionID, stateTransition);
        }

        private void KillAllTweens()
        {
            openingSequence?.Kill();
            closingSequence?.Kill();
            if (spawnPoints != null)
            {
                foreach (var p in spawnPoints)
                {
                    if (p?.spawnModel != null) p.spawnModel.transform.DOKill();
                    if (p?.glowLight != null) DOTween.Kill(p.glowLight);
                }
            }
        }

        // =========================================================
        //                        EVENTS
        // =========================================================

        private void HandleWaveStarted()
        {
            Debug.Log($"HandleWaveStarted called. openOnWaveStart: {openOnWaveStart}, currentState: {currentState}");
            if (openOnWaveStart && currentState == PortalState.Closed)
            {
                Debug.Log("Opening portal due to wave start");
                OpenPortal();
            }
        }

        private void HandleWaveEnded()
        {
            Debug.Log($"HandleWaveEnded called. closeOnWaveEnd: {closeOnWaveEnd}, currentState: {currentState}");
            if (closeOnWaveEnd && currentState == PortalState.Open)
            {
                Debug.Log("Closing portal due to wave end");
                ClosePortal();
            }
        }

        // Debug (optional)
        [ContextMenu("Debug Open Portal")]
        private void DebugOpenPortal()
        {
            Debug.Log($"Debug opening portal. Current state: {currentState}, IsOpen: {IsOpen}");
            if (!IsOpen)
            {
                OpenPortal();
                Debug.Log("Debug opening portal");
            }
            else
            {
                Debug.Log("Portal is already open");
            }
        }

        [ContextMenu("Debug Close Portal")]
        private void DebugClosePortal()
        {
            if (IsOpen)
            {
                ClosePortal();
                Debug.Log("Debug closing portal");
            }
        }

        [ContextMenu("Debug Refresh Portal Visuals")]
        private void DebugRefreshPortalVisuals()
        {
            UpdateMaterialProperties();
            Debug.Log($"Portal state: {currentState}, Transition: {stateTransition}, Material: {(runtimeMaterial != null ? "runtime OK" : "null")}");
        }

        [ContextMenu("Debug Regenerate Spawn Points")]
        private void DebugRegenerateSpawnPoints()
        {
            GenerateSpawnPoints();
            Debug.Log("Regenerated spawn points");
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoints == null) return;
            foreach (var point in spawnPoints)
            {
                if (point?.transform == null) continue;
                Gizmos.color = point.isActive ? Color.red : Color.gray;
                Gizmos.DrawWireSphere(point.transform.position, 0.2f);
            }
        }
    }
}
