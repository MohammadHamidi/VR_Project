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
        [SerializeField] private float shockwaveDamage = 50f; // Base damage for shockwave

        [Header("Visual Effects")]
        [SerializeField] private GameObject shockwaveRingPrefab;
        [SerializeField] private ParticleSystem shockwaveParticles;
        [SerializeField] private Color shockwaveColor = Color.cyan;
        [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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
        private bool wasOverchargedLastFrame;

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
            CombatEvents.OnShockwaveActivated += HandleShockwaveActivated;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnOverchargeStateChanged -= HandleOverchargeStateChanged;
            CombatEvents.OnShockwaveActivated -= HandleShockwaveActivated;
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
            
            if (isOvercharged)
            {
                Debug.Log("Shockwave system activated - squat to trigger shockwave!");
            }
        }

        private void HandleShockwaveActivated()
        {
            // This is called when the PowerMeter triggers a shockwave
            TriggerShockwave();
        }

        private void CheckForShockwaveTrigger()
        {
            if (powerMeter == null || squatDodge == null) 
                return;

            // Check for overcharge state changes
            bool isOverchargedNow = powerMeter.IsOvercharged;
            if (isOverchargedNow != wasOverchargedLastFrame)
            {
                CombatEvents.OnOverchargeStateChanged?.Invoke(isOverchargedNow);
            }
            wasOverchargedLastFrame = isOverchargedNow;

            // Only check for squat triggers during overcharge
            if (!isOverchargedNow) return;

            bool isSquattingNow = squatDodge.IsDodging;
            
            // Trigger shockwave on squat start during overcharge
            if (isSquattingNow && !wasSquattingLastFrame)
            {
                TriggerShockwave();
            }
            
            wasSquattingLastFrame = isSquattingNow;
        }

        // Parameterless version for event system
        public void TriggerShockwave()
        {
            if (playerTransform == null) return;

            Vector3 shockwaveOrigin = playerTransform.position;
            TriggerShockwave(shockwaveOrigin, shockwaveRadius, shockwaveDamage);
        }

        // Overloaded version with parameters for PowerMeter integration
        public void TriggerShockwave(Vector3 origin, float radius, float damage)
        {
            // Visual effects
            StartCoroutine(ShockwaveVisualEffect(origin, radius));
            
            // Audio effect
            PlayShockwaveAudio();
            
            // Physics effects
            ApplyShockwavePhysics(origin, radius);
            
            // Damage effects
            DamageEnemiesInRange(origin, radius, damage);
            
            // Notify other systems
            CombatEvents.OnShockwaveTriggered?.Invoke(origin);
            
            Debug.Log($"Shockwave triggered at {origin} with radius {radius} and damage {damage}");
        }

        private IEnumerator ShockwaveVisualEffect(Vector3 origin, float radius)
        {
            // Spawn visual ring if available
            GameObject ringInstance = null;
            if (shockwaveRingPrefab != null)
            {
                ringInstance = Instantiate(shockwaveRingPrefab, origin, Quaternion.identity);
                ringInstance.SetActive(true);
                
                // Animate the ring expansion
                Vector3 startScale = Vector3.zero;
                Vector3 endScale = Vector3.one * radius * 2f;
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

        private void ApplyShockwavePhysics(Vector3 origin, float radius)
        {
            // Find all rigidbodies in range and apply force
            Collider[] hitColliders = Physics.OverlapSphere(origin, radius, affectedLayers);
            
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

        private void DamageEnemiesInRange(Vector3 origin, float radius, float damage)
        {
            // Find all drones in range
            var drones = FindObjectsOfType<CombatSystem.Drones.DroneController>();
            
            foreach (var drone in drones)
            {
                if (drone.IsDestroyed) continue;
                
                float distance = Vector3.Distance(origin, drone.transform.position);
                if (distance <= radius)
                {
                    // Apply damage based on drone type
                    switch (drone.type)
                    {
                        case CombatSystem.Drones.DroneType.Scout:
                            // Scouts are destroyed instantly
                            drone.DestroyDrone();
                            break;
                            
                        case CombatSystem.Drones.DroneType.Heavy:
                            // Heavy drones are stunned on first hit, destroyed on second
                            if (drone.state == CombatSystem.Drones.DroneState.Stunned)
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
                if (distance <= radius)
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

        public void SetShockwaveDamage(float damage)
        {
            shockwaveDamage = damage;
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
            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
            
            Gizmos.color = new Color(shockwaveColor.r, shockwaveColor.g, shockwaveColor.b, 0.3f);
            Gizmos.DrawSphere(center, shockwaveRadius);
            
            Gizmos.color = shockwaveColor;
            Gizmos.DrawWireSphere(center, shockwaveRadius);
        }
    }
}