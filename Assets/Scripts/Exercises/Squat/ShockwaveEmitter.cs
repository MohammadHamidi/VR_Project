using System.Collections;
using UnityEngine;
using CombatSystem.Events;
using DG.Tweening;

namespace CombatSystem.Combat
{
    public class ShockwaveEmitter : MonoBehaviour
    {
        [Header("Shockwave Settings")]
        [SerializeField] private float shockwaveRadius = 6f;
        [SerializeField] private float shockwaveExpansionTime = 0.8f;
        [SerializeField] private LayerMask affectedLayers = -1;
        [SerializeField] private float knockbackForce = 15f;
        [SerializeField] private float upwardForce = 5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject shockwaveRingPrefab;
        [SerializeField] private ParticleSystem shockwaveParticles;
        [SerializeField] private Color shockwaveColor = Color.cyan;
        [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseOutQuad(0, 0, 1, 1);

        [Header("Audio")]
        [SerializeField] private AudioClip shockwaveSound;
        [SerializeField] private float soundVolume = 1f;

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        // Private components
        private AudioSource audioSource;
        private PowerMeter powerMeter;
        private CombatSystem.Player.SquatDodge squatDodge;
        private bool wasSquattingLastFrame;

        // Ring materials for shockwave effect
        private Material shockwaveRingMaterial;

        void Awake()
        {
            InitializeComponents();
        }

        void Start()
        {
            SubscribeToEvents();
            FindPlayerReferences();
        }

        void Update()
        {
            CheckForShockwaveTrigger();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;

            // Create default shockwave ring material if not provided
            if (shockwaveRingPrefab == null)
            {
                CreateDefaultShockwaveRing();
            }
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnOverchargeStateChanged += HandleOverchargeStateChanged;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnOverchargeStateChanged -= HandleOverchargeStateChanged;
        }

        private void FindPlayerReferences()
        {
            // Find PowerMeter
            powerMeter = FindObjectOfType<PowerMeter>();
            if (powerMeter == null)
            {
                Debug.LogWarning("ShockwaveEmitter: No PowerMeter found in scene");
            }

            // Find SquatDodge
            squatDodge = FindObjectOfType<CombatSystem.Player.SquatDodge>();
            if (squatDodge == null)
            {
                Debug.LogWarning("ShockwaveEmitter: No SquatDodge found in scene");
            }

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
        }

        private void CreateDefaultShockwaveRing()
        {
            // Create a simple ring GameObject for shockwave effect
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockwaveRing";
            
            // Remove collider and make it thin
            Destroy(ring.GetComponent<Collider>());
            ring.transform.localScale = new Vector3(1f, 0.01f, 1f);
            
            // Create material
            shockwaveRingMaterial = new Material(Shader.Find("Sprites/Default"));
            shockwaveRingMaterial.color = shockwaveColor;
            shockwaveRingMaterial.SetFloat("_Mode", 3); // Transparent mode
            
            ring.GetComponent<Renderer>().material = shockwaveRingMaterial;
            
            // Convert to prefab reference
            shockwaveRingPrefab = ring;
            ring.SetActive(false);
        }

        private void HandleOverchargeStateChanged(bool isOvercharged)
        {
            // Reset squat state when overcharge changes
            wasSquattingLastFrame = false;
        }

        private void CheckForShockwaveTrigger()
        {
            if (powerMeter == null || squatDodge == null || !powerMeter.IsOvercharged) 
                return;

            bool isSquattingNow = squatDodge.IsDodging;
            
            // Trigger shockwave on squat start during overcharge
            if (isSquattingNow && !wasSquattingLastFrame)
            {
                TriggerShockwave();
            }
            
            wasSquattingLastFrame = isSquattingNow;
        }

        public void TriggerShockwave()
        {
            if (playerTransform == null) return;

            Vector3 shockwaveOrigin = playerTransform.position;
            
            // Visual effects
            StartCoroutine(ShockwaveVisualEffect(shockwaveOrigin));
            
            // Audio effect
            PlayShockwaveAudio();
            
            // Physics effects
            ApplyShockwavePhysics(shockwaveOrigin);
            
            // Damage effects
            DamageEnemiesInRange(shockwaveOrigin);
            
            // Notify other systems
            CombatEvents.OnShockwaveTriggered?.Invoke(shockwaveOrigin);
            
            Debug.Log($"Shockwave triggered at {shockwaveOrigin}");
        }

        private IEnumerator ShockwaveVisualEffect(Vector3 origin)
        {
            // Spawn visual ring if available
            GameObject ringInstance = null;
            if (shockwaveRingPrefab != null)
            {
                ringInstance = Instantiate(shockwaveRingPrefab, origin, Quaternion.identity);
                ringInstance.SetActive(true);
                
                // Animate the ring expansion
                Vector3 startScale = Vector3.zero;
                Vector3 endScale = Vector3.one * shockwaveRadius * 2f;
                endScale.y = 0.01f; // Keep it flat
                
                ringInstance.transform.localScale = startScale;
                
                // DOTween expansion animation
                ringInstance.transform.DOScale(endScale, shockwaveExpansionTime)
                    .SetEase(Ease.OutQuad);
                
                // Fade out animation
                Renderer ringRenderer = ringInstance.GetComponent<Renderer>();
                if (ringRenderer != null)
                {
                    Material ringMat = ringRenderer.material;
                    Color startColor = ringMat.color;
                    Color endColor = startColor;
                    endColor.a = 0f;
                    
                    DOTween.To(() => ringMat.color, x => ringMat.color = x, endColor, shockwaveExpansionTime)
                        .SetEase(Ease.OutQuad);
                }
            }

            // Trigger particle effect
            if (shockwaveParticles != null)
            {
                shockwaveParticles.transform.position = origin;
                shockwaveParticles.Play();
            }

            // Camera shake
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.5f, Vector3.one * 0.15f, 20, 90f);
            }

            yield return new WaitForSeconds(shockwaveExpansionTime);

            // Cleanup ring
            if (ringInstance != null)
            {
                Destroy(ringInstance);
            }
        }

        private void PlayShockwaveAudio()
        {
            if (audioSource && shockwaveSound)
            {
                audioSource.PlayOneShot(shockwaveSound, soundVolume);
            }
        }

        private void ApplyShockwavePhysics(Vector3 origin)
        {
            // Find all rigidbodies in range and apply force
            Collider[] hitColliders = Physics.OverlapSphere(origin, shockwaveRadius, affectedLayers);
            
            foreach (Collider col in hitColliders)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && rb.gameObject != playerTransform.gameObject)
                {
                    Vector3 direction = (col.transform.position - origin).normalized;
                    Vector3 force = direction * knockbackForce + Vector3.up * upwardForce;
                    
                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
        }

        private void DamageEnemiesInRange(Vector3 origin)
        {
            // Find all drones in range
            var drones = FindObjectsOfType<CombatSystem.Drones.DroneController>();
            
            foreach (var drone in drones)
            {
                if (drone.IsDestroyed) continue;
                
                float distance = Vector3.Distance(origin, drone.transform.position);
                if (distance <= shockwaveRadius)
                {
                    // Apply damage based on drone type
                    switch (drone.Type)
                    {
                        case CombatSystem.Drones.DroneType.Scout:
                            // Scouts are destroyed instantly
                            drone.DestroyDrone();
                            break;
                            
                        case CombatSystem.Drones.DroneType.Heavy:
                            // Heavy drones are stunned on first hit, destroyed on second
                            if (drone.IsStunned)
                            {
                                drone.DestroyDrone();
                            }
                            else
                            {
                                drone.StunDrone(1.5f);
                            }
                            break;
                    }
                }
            }
            
            // Also damage legacy cubes if any exist
            var cubes = FindObjectsOfType<CombatSystem.Obstacles.CubeMover>();
            foreach (var cube in cubes)
            {
                float distance = Vector3.Distance(origin, cube.transform.position);
                if (distance <= shockwaveRadius)
                {
                    cube.DestroyAndSpawnCoin();
                }
            }
        }

        // Public API
        public void SetShockwaveRadius(float radius)
        {
            shockwaveRadius = radius;
        }

        public void SetKnockbackForce(float force)
        {
            knockbackForce = force;
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTriggerShockwave()
        {
            if (playerTransform != null)
            {
                TriggerShockwave();
            }
        }

        void OnDrawGizmosSelected()
        {
            // Draw shockwave radius
            if (playerTransform != null)
            {
                Gizmos.color = shockwaveColor;
                Gizmos.color = new Color(shockwaveColor.r, shockwaveColor.g, shockwaveColor.b, 0.3f);
                Gizmos.DrawSphere(playerTransform.position, shockwaveRadius);
                
                Gizmos.color = shockwaveColor;
                Gizmos.DrawWireSphere(playerTransform.position, shockwaveRadius);
            }
            else
            {
                // Draw at this object's position if no player reference
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
            }
        }
    }
}
