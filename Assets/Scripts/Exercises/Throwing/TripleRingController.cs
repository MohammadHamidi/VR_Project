using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace ThrowingExercise
{
    [System.Serializable]
    public class RingConfiguration
    {
        [Header("Ring Settings")]
        public float radius = 0.35f;
        public float centerTolerance = 0.07f; // Perfect tunnel tolerance
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public bool randomizePosition = false;
        public Vector2 positionJitter = new Vector2(0.03f, 0.03f); // X, Z jitter range
        
        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color activeColor = Color.green;
        public Color successColor = new Color(1f, 0.84f, 0f); // Gold color
    }

    [System.Serializable]
    public class TunnelDifficulty
    {
        public string difficultyName = "Easy";
        public RingConfiguration[] rings = new RingConfiguration[3];
        public float timeLimit = 60f;
        public bool enableTimeLimit = false;
        public int requiredSuccesses = 1; // How many perfect tunnels needed to complete
    }

    public class TripleRingController : MonoBehaviour
    {
        [Header("Ring References")]
        [SerializeField] private GameObject ringPrefab;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform ballSpawn;

        [Header("Difficulty Settings")]
        [SerializeField] private TunnelDifficulty[] difficulties;
        [SerializeField] private int currentDifficultyIndex = 0;

        [Header("Tunnel Detection")]
        [SerializeField] private LayerMask ballLayer = 1;
        [SerializeField] private float ballSpeedThreshold = 1f; // Minimum speed for valid tunnel
        [SerializeField] private bool requireSequentialHits = true;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem perfectTunnelVFX;
        [SerializeField] private AudioClip perfectTunnelSound;
        [SerializeField] private AudioClip ringHitSound;
        [SerializeField] private AudioClip tunnelFailSound;

        [Header("UI Feedback")]
        [SerializeField] private Canvas feedbackCanvas;
        [SerializeField] private UnityEngine.UI.Text statusText;
        [SerializeField] private UnityEngine.UI.Text progressText;

        // Runtime state
        private RingCenter[] ringCenters;
        private GameObject[] ringObjects;
        private int nextRequiredRing = 0; // Which ring should be hit next (0, 1, 2)
        private int perfectTunnels = 0;
        private bool isTunnelInProgress = false;
        private float tunnelStartTime;
        private Vector3 ballLastPosition;
        private Vector3 ballVelocity;
        private AudioSource audioSource;

        // Events
        public System.Action<int> OnRingHit; // Ring index
        public System.Action OnPerfectTunnel;
        public System.Action OnTunnelFailed;
        public System.Action<int, int> OnProgressChanged; // current, required
        public System.Action<TunnelDifficulty> OnDifficultyChanged;

        // Properties
        public bool IsTunnelMode { get; private set; } = true;
        public int CurrentDifficulty => currentDifficultyIndex;
        public TunnelDifficulty CurrentDifficultyConfig => 
            difficulties != null && currentDifficultyIndex < difficulties.Length ? 
            difficulties[currentDifficultyIndex] : null;
        public int PerfectTunnels => perfectTunnels;
        public bool IsCompleted => perfectTunnels >= (CurrentDifficultyConfig?.requiredSuccesses ?? 1);

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            audioSource.spatialBlend = 0f; // 2D UI sound
            audioSource.playOnAwake = false;
        }

        void Start()
        {
            InitializeDefaultDifficulties();
            SetupTunnelRings();
            UpdateUI();
        }

        void Update()
        {
            if (isTunnelInProgress)
            {
                TrackBallMovement();
                CheckTunnelTimeout();
            }
        }

        private void InitializeDefaultDifficulties()
        {
            if (difficulties == null || difficulties.Length == 0)
            {
                difficulties = new TunnelDifficulty[]
                {
                    CreateDifficulty("Easy", 
                        new float[] { 0.40f, 0.35f, 0.30f }, 
                        new float[] { 2f, 3f, 4f }, 
                        0.07f, 1),
                    CreateDifficulty("Medium", 
                        new float[] { 0.35f, 0.30f, 0.25f }, 
                        new float[] { 2.5f, 3.5f, 4.5f }, 
                        0.06f, 2),
                    CreateDifficulty("Hard", 
                        new float[] { 0.28f, 0.24f, 0.20f }, 
                        new float[] { 3f, 4f, 5f }, 
                        0.05f, 3)
                };
            }
        }

        private TunnelDifficulty CreateDifficulty(string name, float[] radii, float[] distances, float tolerance, int required)
        {
            var difficulty = new TunnelDifficulty
            {
                difficultyName = name,
                requiredSuccesses = required,
                rings = new RingConfiguration[3]
            };

            for (int i = 0; i < 3; i++)
            {
                difficulty.rings[i] = new RingConfiguration
                {
                    radius = radii[i],
                    centerTolerance = tolerance,
                    position = Vector3.forward * distances[i],
                    randomizePosition = true,
                    positionJitter = Vector2.one * 0.03f
                };
            }

            return difficulty;
        }

        private void SetupTunnelRings()
        {
            // Clean up existing rings
            if (ringObjects != null)
            {
                foreach (var ring in ringObjects)
                {
                    if (ring != null) Destroy(ring);
                }
            }

            TunnelDifficulty config = CurrentDifficultyConfig;
            if (config == null)
            {
                Debug.LogError("TripleRingController: No difficulty configuration found!");
                return;
            }

            // Find player reference if not set
            if (playerTransform == null)
            {
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                    playerTransform = xrOrigin.transform;
            }

            // Create ring objects
            ringObjects = new GameObject[3];
            ringCenters = new RingCenter[3];

            for (int i = 0; i < 3; i++)
            {
                CreateRing(i, config.rings[i]);
            }

            ResetTunnel();
        }

        private void CreateRing(int index, RingConfiguration config)
        {
            // Create ring object
            Vector3 worldPosition = config.position;
            if (playerTransform != null)
                worldPosition += playerTransform.position;

            if (config.randomizePosition)
            {
                worldPosition += new Vector3(
                    Random.Range(-config.positionJitter.x, config.positionJitter.x),
                    0f,
                    Random.Range(-config.positionJitter.y, config.positionJitter.y)
                );
            }

            GameObject ringObj;
            if (ringPrefab != null)
            {
                ringObj = Instantiate(ringPrefab, worldPosition, Quaternion.Euler(config.rotation));
            }
            else
            {
                // Create default ring
                ringObj = CreateDefaultRing(worldPosition, config);
            }

            ringObj.name = $"TunnelRing_{index}";
            ringObjects[index] = ringObj;

            // Setup center detection
            GameObject centerObj = new GameObject($"RingCenter_{index}");
            centerObj.transform.SetParent(ringObj.transform);
            centerObj.transform.localPosition = Vector3.zero;

            // Add center trigger collider
            SphereCollider centerCollider = centerObj.AddComponent<SphereCollider>();
            centerCollider.radius = config.centerTolerance;
            centerCollider.isTrigger = true;

            // Add RingCenter component
            RingCenter centerComponent = centerObj.AddComponent<RingCenter>();
            centerComponent.ringIndex = index;
            centerComponent.OnCenterHit += HandleRingHit;

            ringCenters[index] = centerComponent;

            // Scale ring to match radius
            float scale = config.radius * 2f;
            ringObj.transform.localScale = Vector3.one * scale;

            // Apply visual styling
            ApplyRingVisuals(ringObj, config, index);
        }

        private GameObject CreateDefaultRing(Vector3 position, RingConfiguration config)
        {
            // Create torus-like ring using primitives
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.position = position;
            ring.transform.localScale = new Vector3(1f, 0.05f, 1f); // Thin disk

            // Remove collider (we'll use custom center collider)
            Destroy(ring.GetComponent<Collider>());

            // Create hole in center (simple approach - just make it transparent in center)
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = config.normalColor;
            }

            return ring;
        }

        private void ApplyRingVisuals(GameObject ringObj, RingConfiguration config, int index)
        {
            var renderer = ringObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Set initial color
                Color targetColor = (index == nextRequiredRing) ? config.activeColor : config.normalColor;
                renderer.material.color = targetColor;

                // Add glow effect for active ring
                if (index == nextRequiredRing)
                {
                    ringObj.transform.DOScale(ringObj.transform.localScale * 1.1f, 0.5f)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }
        }

        private void HandleRingHit(int ringIndex)
        {
            if (!isTunnelInProgress)
            {
                StartTunnel();
            }

            // Check if this is the correct ring in sequence
            if (requireSequentialHits && ringIndex != nextRequiredRing)
            {
                // Wrong order - fail tunnel
                FailTunnel($"Hit ring {ringIndex + 1} but expected ring {nextRequiredRing + 1}");
                return;
            }

            // Correct ring hit
            OnRingHit?.Invoke(ringIndex);
            PlayRingHitFeedback(ringIndex);
            
            nextRequiredRing++;

            // Check if tunnel completed
            if (nextRequiredRing >= 3)
            {
                CompletePerfectTunnel();
            }
            else
            {
                // Update visuals for next ring
                UpdateRingVisuals();
            }
        }

        private void StartTunnel()
        {
            isTunnelInProgress = true;
            tunnelStartTime = Time.time;
            UpdateStatusText("Tunnel in progress...");
        }

        private void CompletePerfectTunnel()
        {
            perfectTunnels++;
            isTunnelInProgress = false;

            // Play success feedback
            PlayPerfectTunnelFeedback();
            
            // Notify systems
            OnPerfectTunnel?.Invoke();
            OnProgressChanged?.Invoke(perfectTunnels, CurrentDifficultyConfig.requiredSuccesses);
            
            UpdateStatusText($"Perfect Tunnel! ({perfectTunnels}/{CurrentDifficultyConfig.requiredSuccesses})");
            UpdateUI();

            // Check completion
            if (IsCompleted)
            {
                HandleDifficultyCompleted();
            }
            else
            {
                // Reset for next attempt
                StartCoroutine(DelayedReset(2f));
            }
        }

        private void FailTunnel(string reason)
        {
            isTunnelInProgress = false;
            
            // Play fail feedback
            if (audioSource && tunnelFailSound)
                audioSource.PlayOneShot(tunnelFailSound);
            
            OnTunnelFailed?.Invoke();
            UpdateStatusText($"Tunnel failed: {reason}");
            
            // Reset after delay
            StartCoroutine(DelayedReset(1.5f));
        }

        private IEnumerator DelayedReset(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetTunnel();
        }

        private void ResetTunnel()
        {
            nextRequiredRing = 0;
            isTunnelInProgress = false;
            UpdateRingVisuals();
            UpdateStatusText("Throw ball through all rings in order");
        }

        private void UpdateRingVisuals()
        {
            var config = CurrentDifficultyConfig;
            if (config == null || ringObjects == null) return;

            for (int i = 0; i < ringObjects.Length; i++)
            {
                if (ringObjects[i] == null) continue;

                var renderer = ringObjects[i].GetComponent<Renderer>();
                if (renderer == null) continue;

                // Stop any existing animations
                ringObjects[i].transform.DOKill();

                if (i < nextRequiredRing)
                {
                    // Already hit - success color
                    renderer.material.color = config.rings[i].successColor;
                }
                else if (i == nextRequiredRing)
                {
                    // Next target - active color with glow
                    renderer.material.color = config.rings[i].activeColor;
                    ringObjects[i].transform.DOScale(ringObjects[i].transform.localScale * 1.1f, 0.5f)
                        .SetLoops(-1, LoopType.Yoyo);
                }
                else
                {
                    // Future target - normal color
                    renderer.material.color = config.rings[i].normalColor;
                }
            }
        }

        private void TrackBallMovement()
        {
            // Find ball in scene
            var ball = GameObject.FindWithTag("Throwable");
            if (ball != null)
            {
                Vector3 currentPos = ball.transform.position;
                ballVelocity = (currentPos - ballLastPosition) / Time.deltaTime;
                ballLastPosition = currentPos;

                // Check if ball is moving too slowly (might have stopped)
                if (ballVelocity.magnitude < ballSpeedThreshold && Vector3.Distance(currentPos, ballSpawn.position) > 1f)
                {
                    FailTunnel("Ball stopped moving");
                }
            }
        }

        private void CheckTunnelTimeout()
        {
            var config = CurrentDifficultyConfig;
            if (config != null && config.enableTimeLimit)
            {
                if (Time.time - tunnelStartTime > config.timeLimit)
                {
                    FailTunnel("Time limit exceeded");
                }
            }
        }

        private void PlayRingHitFeedback(int ringIndex)
        {
            if (audioSource && ringHitSound)
            {
                audioSource.pitch = 1f + (ringIndex * 0.2f); // Higher pitch for each ring
                audioSource.PlayOneShot(ringHitSound);
            }

            // Ring flash effect
            if (ringObjects[ringIndex] != null)
            {
                var renderer = ringObjects[ringIndex].GetComponent<Renderer>();
                if (renderer != null)
                {
                    var originalColor = renderer.material.color;
                    renderer.material.color = Color.white;
                    DOTween.To(() => renderer.material.color, x => renderer.material.color = x, originalColor, 0.3f);
                }
            }
        }

        private void PlayPerfectTunnelFeedback()
        {
            if (audioSource && perfectTunnelSound)
                audioSource.PlayOneShot(perfectTunnelSound);

            if (perfectTunnelVFX)
            {
                perfectTunnelVFX.transform.position = ringObjects[2].transform.position; // Play at final ring
                perfectTunnelVFX.Play();
            }

            // Screen flash or other dramatic effect
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.3f, Vector3.one * 0.05f, 10, 90f);
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void UpdateUI()
        {
            if (progressText != null)
            {
                var config = CurrentDifficultyConfig;
                if (config != null)
                {
                    progressText.text = $"Progress: {perfectTunnels}/{config.requiredSuccesses}\nDifficulty: {config.difficultyName}";
                }
            }
        }

        private void HandleDifficultyCompleted()
        {
            UpdateStatusText($"Difficulty '{CurrentDifficultyConfig.difficultyName}' completed!");
            
            // Auto-advance to next difficulty or restart
            if (currentDifficultyIndex < difficulties.Length - 1)
            {
                StartCoroutine(AdvanceToNextDifficulty());
            }
            else
            {
                UpdateStatusText("All difficulties completed! Restarting...");
                StartCoroutine(RestartFromBeginning());
            }
        }

        private IEnumerator AdvanceToNextDifficulty()
        {
            yield return new WaitForSeconds(3f);
            SetDifficulty(currentDifficultyIndex + 1);
        }

        private IEnumerator RestartFromBeginning()
        {
            yield return new WaitForSeconds(3f);
            SetDifficulty(0);
        }

        // Public API
        public void SetDifficulty(int difficultyIndex)
        {
            if (difficultyIndex >= 0 && difficultyIndex < difficulties.Length)
            {
                currentDifficultyIndex = difficultyIndex;
                perfectTunnels = 0;
                SetupTunnelRings();
                OnDifficultyChanged?.Invoke(CurrentDifficultyConfig);
                UpdateUI();
            }
        }

        public void ResetProgress()
        {
            perfectTunnels = 0;
            ResetTunnel();
            UpdateUI();
        }

        public void ToggleTunnelMode(bool enabled)
        {
            IsTunnelMode = enabled;
            gameObject.SetActive(enabled);
        }

        void OnDrawGizmosSelected()
        {
            // Draw ring positions and tolerances
            var config = CurrentDifficultyConfig;
            if (config == null) return;

            Vector3 basePos = playerTransform != null ? playerTransform.position : transform.position;

            for (int i = 0; i < config.rings.Length; i++)
            {
                Vector3 ringPos = basePos + config.rings[i].position;
                
                // Draw ring outline
                Gizmos.color = i == nextRequiredRing ? Color.green : Color.white;
                Gizmos.DrawWireSphere(ringPos, config.rings[i].radius);
                
                // Draw center tolerance
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(ringPos, config.rings[i].centerTolerance);
                
                // Draw connection line
                if (i > 0)
                {
                    Vector3 prevPos = basePos + config.rings[i - 1].position;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(prevPos, ringPos);
                }
            }
        }
    }

    // Ring center detection component
    public class RingCenter : MonoBehaviour
    {
        public int ringIndex;
        public System.Action<int> OnCenterHit;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Throwable")) return;

            // Ensure forward crossing to prevent cheating
            var ballRb = other.attachedRigidbody;
            if (ballRb != null && Vector3.Dot(ballRb.velocity, transform.forward) > 0f)
            {
                OnCenterHit?.Invoke(ringIndex);
            }
        }
    }
}
