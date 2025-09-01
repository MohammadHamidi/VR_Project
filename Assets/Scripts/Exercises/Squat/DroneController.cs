using System.Collections;
using UnityEngine;
using CombatSystem.Events;
using DG.Tweening;

namespace CombatSystem.Drones
{
    public enum DroneType
    {
        Scout,  // پهپادِ شناس - quick single beam
        Heavy   // پهپادِ سنگین - slow thick beam with splash
    }

    public class DroneController : MonoBehaviour
    {
        [Header("Drone Settings")]
        [SerializeField] private DroneType droneType = DroneType.Scout;
        [SerializeField] private float health = 1f;
        [SerializeField] private float movementSpeed = 2f;
        [SerializeField] private float hoverAmplitude = 0.5f;
        [SerializeField] private float hoverFrequency = 1f;

        [Header("Attack Settings")]
        [SerializeField] private float telegraphTime = 0.6f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private float beamThickness = 0.1f;
        [SerializeField] private float attackRange = 15f;
        [SerializeField] private LayerMask playerLayer = 1;

        [Header("Visual Components")]
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private ParticleSystem telegraphVFX;
        [SerializeField] private ParticleSystem destroyVFX;
        [SerializeField] private GameObject droneModel;
        [SerializeField] private Light spotLight;

        [Header("Audio")]
        [SerializeField] private AudioClip telegraphSound;
        [SerializeField] private AudioClip laserFireSound;
        [SerializeField] private AudioClip destroySound;

        [Header("Materials")]
        [SerializeField] private Material telegraphMaterial;
        [SerializeField] private Material laserMaterial;

        // Private components
        private AudioSource audioSource;
        private Rigidbody rb;
        private Vector3 startPosition;
        private float lastAttackTime;
        private bool isAttacking;
        private bool isDestroyed;
        private bool isStunned;
        private float stunEndTime;

        // Properties
        public bool IsDestroyed => isDestroyed;
        public bool IsStunned => isStunned && Time.time < stunEndTime;
        public DroneType Type => droneType;

        void Awake()
        {
            InitializeComponents();
            ConfigureDroneType();
        }

        void Start()
        {
            startPosition = transform.position;
            StartHoverAnimation();
        }

        void Update()
        {
            if (isDestroyed) return;

            UpdateHover();
            UpdateAttackLogic();
            UpdateStunState();
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            
            rb.useGravity = false;
            rb.isKinematic = true;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            
            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.playOnAwake = false;

            // Setup laser line if not assigned
            if (laserLine == null)
            {
                GameObject laserObj = new GameObject("LaserLine");
                laserObj.transform.SetParent(transform);
                laserLine = laserObj.AddComponent<LineRenderer>();
                SetupLaserLine();
            }

            // Setup spotlight if not assigned
            if (spotLight == null)
            {
                GameObject lightObj = new GameObject("SpotLight");
                lightObj.transform.SetParent(transform);
                spotLight = lightObj.AddComponent<Light>();
                SetupSpotLight();
            }
        }

        private void ConfigureDroneType()
        {
            switch (droneType)
            {
                case DroneType.Scout:
                    health = 1f;
                    telegraphTime = 0.6f;
                    attackCooldown = 1.0f;
                    beamThickness = 0.05f;
                    movementSpeed = 3f;
                    break;
                    
                case DroneType.Heavy:
                    health = 2f; // Requires 2 shockwaves to destroy
                    telegraphTime = 1.2f;
                    attackCooldown = 2.0f;
                    beamThickness = 0.15f;
                    movementSpeed = 1.5f;
                    break;
            }
        }

        private void SetupLaserLine()
        {
            laserLine.material = laserMaterial != null ? laserMaterial : CreateDefaultLaserMaterial();
            laserLine.startWidth = beamThickness;
            laserLine.endWidth = beamThickness;
            laserLine.positionCount = 2;
            laserLine.enabled = false;
            laserLine.useWorldSpace = true;
        }

        private void SetupSpotLight()
        {
            spotLight.type = LightType.Spot;
            spotLight.color = Color.red;
            spotLight.intensity = 2f;
            spotLight.range = attackRange;
            spotLight.spotAngle = 15f;
            spotLight.enabled = false;
        }

        private Material CreateDefaultLaserMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.red;
            return mat;
        }

        private void StartHoverAnimation()
        {
            // DOTween hover animation
            transform.DOMoveY(startPosition.y + hoverAmplitude, 1f / hoverFrequency)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdateHover()
        {
            // Additional random movement for more dynamic feel
            Vector3 randomOffset = new Vector3(
                Mathf.Sin(Time.time * hoverFrequency * 0.7f) * 0.2f,
                0,
                Mathf.Cos(Time.time * hoverFrequency * 0.5f) * 0.2f
            );
            
            Vector3 targetPos = startPosition + randomOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * movementSpeed * 0.5f);
        }

        private void UpdateAttackLogic()
        {
            if (isAttacking || IsStunned) return;
            
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // Find player and attack
                var player = FindPlayerTarget();
                if (player != null)
                {
                    StartCoroutine(AttackSequence(player));
                }
            }
        }

        private void UpdateStunState()
        {
            if (isStunned && Time.time >= stunEndTime)
            {
                isStunned = false;
                // Resume normal behavior
                if (droneModel != null)
                {
                    droneModel.transform.DOKill();
                    droneModel.transform.rotation = Quaternion.identity;
                }
            }
        }

        private Transform FindPlayerTarget()
        {
            // Look for XR Camera or main camera
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
                return xrOrigin.Camera.transform;
                
            return Camera.main?.transform;
        }

        private IEnumerator AttackSequence(Transform target)
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            // Phase 1: Telegraph
            yield return StartCoroutine(TelegraphPhase(target));

            // Phase 2: Fire laser
            yield return StartCoroutine(FireLaserPhase(target));

            // Phase 3: Cooldown
            yield return new WaitForSeconds(0.2f);

            isAttacking = false;
        }

        private IEnumerator TelegraphPhase(Transform target)
        {
            // Play telegraph sound
            if (audioSource && telegraphSound)
                audioSource.PlayOneShot(telegraphSound);

            // Enable telegraph VFX
            if (telegraphVFX) telegraphVFX.Play();

            // Show telegraph line (red, growing intensity)
            laserLine.enabled = true;
            laserLine.material = telegraphMaterial != null ? telegraphMaterial : CreateTelegraphMaterial();
            
            spotLight.enabled = true;
            spotLight.color = Color.red;

            float elapsedTime = 0f;
            while (elapsedTime < telegraphTime)
            {
                // Update line position
                Vector3 startPos = transform.position;
                Vector3 direction = (target.position - startPos).normalized;
                Vector3 endPos = startPos + direction * attackRange;

                laserLine.SetPosition(0, startPos);
                laserLine.SetPosition(1, endPos);

                // Point spotlight at target
                spotLight.transform.LookAt(target.position);

                // Animate intensity
                float intensity = Mathf.Lerp(0.1f, 1f, elapsedTime / telegraphTime);
                Color color = Color.red * intensity;
                color.a = intensity;
                laserLine.material.color = color;
                spotLight.intensity = intensity * 3f;

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator FireLaserPhase(Transform target)
        {
            // Play laser fire sound
            if (audioSource && laserFireSound)
                audioSource.PlayOneShot(laserFireSound);

            // Change to laser material
            laserLine.material = laserMaterial != null ? laserMaterial : CreateDefaultLaserMaterial();
            laserLine.material.color = Color.red;

            spotLight.color = Color.white;
            spotLight.intensity = 5f;

            // Perform raycast for hit detection
            Vector3 startPos = transform.position;
            Vector3 direction = (target.position - startPos).normalized;
            
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, attackRange, playerLayer))
            {
                // Check if player is squatting (dodging)
                bool playerDodging = false;
                var squatDodge = FindObjectOfType<CombatSystem.Player.SquatDodge>();
                if (squatDodge != null)
                {
                    playerDodging = squatDodge.IsDodging;
                }

                if (!playerDodging)
                {
                    // Player hit - trigger damage
                    HitPlayer(hit.point);
                }
                else
                {
                    // Player successfully dodged
                    CombatEvents.OnPlayerDodge?.Invoke();
                }

                // Show laser hitting the point
                laserLine.SetPosition(1, hit.point);
            }
            else
            {
                // Laser missed
                Vector3 endPos = startPos + direction * attackRange;
                laserLine.SetPosition(1, endPos);
            }

            // Keep laser visible for short duration
            yield return new WaitForSeconds(0.3f);

            // Disable visuals
            laserLine.enabled = false;
            spotLight.enabled = false;
            if (telegraphVFX) telegraphVFX.Stop();
        }

        private void HitPlayer(Vector3 hitPoint)
        {
            Debug.Log($"Player hit by {droneType} drone at {hitPoint}");
            
            // Add screen flash or damage effect here
            // This should integrate with your health/lives system
            CombatEvents.OnPlayerHit?.Invoke(hitPoint);

            // Heavy drone splash damage
            if (droneType == DroneType.Heavy)
            {
                // Create explosion effect at hit point
                if (destroyVFX)
                {
                    var explosion = Instantiate(destroyVFX, hitPoint, Quaternion.identity);
                    Destroy(explosion.gameObject, 2f);
                }
            }
        }

        private Material CreateTelegraphMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0f, 0f, 0.5f);
            return mat;
        }

        public void TakeDamage(float damage = 1f)
        {
            if (isDestroyed) return;

            health -= damage;
            
            if (health <= 0)
            {
                DestroyDrone();
            }
            else
            {
                // If not destroyed, get stunned (for Heavy drones)
                StunDrone(1.5f);
            }
        }

        public void StunDrone(float duration)
        {
            if (isDestroyed) return;

            isStunned = true;
            stunEndTime = Time.time + duration;

            // Visual stun effect
            if (droneModel != null)
            {
                droneModel.transform.DOShakeRotation(duration, Vector3.one * 45f, 10, 90f);
            }
        }

        public void DestroyDrone()
        {
            if (isDestroyed) return;

            isDestroyed = true;

            // Stop all animations
            transform.DOKill();
            if (droneModel) droneModel.transform.DOKill();

            // Play destroy effects
            if (destroyVFX) destroyVFX.Play();
            if (audioSource && destroySound) audioSource.PlayOneShot(destroySound);

            // Disable visuals
            laserLine.enabled = false;
            spotLight.enabled = false;
            if (droneModel) droneModel.SetActive(false);

            // Notify combat system
            CombatEvents.OnDroneDestroyed?.Invoke(this);

            // Destroy after effects finish
            Destroy(gameObject, 2f);
        }

        void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw telegraph direction
            var target = FindPlayerTarget();
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
