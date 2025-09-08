// CombatSystem/Drones/DroneController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CombatSystem.Events;

namespace CombatSystem.Drones
{
    public enum DroneType { Scout, Heavy }
    public enum DroneState { Idle, Aggressive, Telegraphing, Dashing, Recovering, Stunned, Destroyed }

    public interface IPoolable
    {
        void SetPool(System.Action<IPoolable> returnToPool);
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Drone behavior with direct aggro (no wandering) + head targeting for VR dodging.
    /// </summary>
    public class DroneController : MonoBehaviour, IPoolable
    {
        [Header("Identity")]
        public DroneType type = DroneType.Scout;
        [SerializeField] private bool despawnAfterFirstAttack = true;    // Return to pool after first dash
        private Vector3 homePosition;                                    // Spawn location (for reference)
        public void SetHome(Vector3 pos) => homePosition = pos;

        [Header("References")]
        [SerializeField] private Transform droneModel;    // Visual body
        [SerializeField] private AudioSource audioSource; // Created in Awake if missing
        [SerializeField] public Transform player;        // Player target (will be set to head)

        [Header("Hover")]
        [SerializeField] private float baseHoverHeight = 1.4f;
        [SerializeField] private float hoverAmplitude = 0.15f;
        [SerializeField] private float hoverSpeed = 1.2f;

        [Header("Aggro / Attack")]
        [SerializeField] private float detectionRange = 5.0f;
        [SerializeField] private float aggroBuildPerSecond = 1.5f;      // Faster aggro build
        [SerializeField] private float aggroDecayPerSecond = 0.8f;
        [SerializeField] private float telegraphDuration = 0.45f;
        [SerializeField] private float dashSpeed = 7.5f;
        [SerializeField] private float dashTime = 0.34f;
        [SerializeField] private float recoverTime = 0.45f;
        [SerializeField] private float initialAggroDelay = 0.5f;        // Brief delay before starting aggro

        [Header("Head Targeting")]
        [SerializeField] private Vector3 headOffset = Vector3.zero;     // Additional offset for head targeting

        [Header("HP")]
        [SerializeField] private float maxHP = 10f;

        // ======= Internal state =======
        public DroneState state { get; private set; } = DroneState.Idle;
        public bool IsDestroyed => state == DroneState.Destroyed;

        private float hp;
        private float aggroValue;
        private float spawnTime;
        private Transform headTarget; // The actual head/camera transform

        // ======= Tweens =======
        private Tween hoverTween;
        private bool hoverEnabled = true;
        private bool inPortalEntry = false;

        // ======= Pooling =======
        private System.Action<IPoolable> _returnToPool;

        // ======= Difficulty multipliers (set via reflection by spawner) =======
        private float _dashSpeed_Internal = 1f;
        private float _attackCooldown_Internal = 1f;
        private float _telegraphDuration_Internal = 1f;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        private void OnEnable()
        {
            inPortalEntry = false;
            hoverEnabled = true;
            KillAllTweens();
            StartHoverAnimation();
        }

        private void OnDisable()
        {
            KillAllTweens();
        }

        public void SetPool(System.Action<IPoolable> returnToPool) => _returnToPool = returnToPool;

        public void OnSpawned()
        {
            hp = maxHP;
            state = DroneState.Idle; // Start idle, will quickly become aggressive
            aggroValue = 0f;
            inPortalEntry = false;
            hoverEnabled = true;
            spawnTime = Time.time;

            homePosition = transform.position;

            // Find and set head target for VR
            SetupHeadTarget();

            if (droneModel != null)
            {
                droneModel.gameObject.SetActive(true);
                droneModel.localScale = Vector3.one;
            }

            Debug.Log($"{gameObject.name} spawned, targeting head at: {(headTarget != null ? headTarget.name : "null")}");
        }

        private void SetupHeadTarget()
        {
            // Try to find the VR head/camera
            if (player != null)
            {
                // If player is already the camera, use it directly
                var camera = player.GetComponent<Camera>();
                if (camera != null)
                {
                    headTarget = player;
                    return;
                }
            }

            // Look for XR Origin and get the camera
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                headTarget = xrOrigin.Camera.transform;
                player = headTarget; // Set player reference to head for compatibility
                Debug.Log($"Found XR head target: {headTarget.name}");
                return;
            }

            // Fallback to main camera
            if (Camera.main != null)
            {
                headTarget = Camera.main.transform;
                player = headTarget;
                Debug.Log($"Using main camera as head target: {headTarget.name}");
                return;
            }

            // Last resort - use existing player reference
            headTarget = player;
            Debug.LogWarning("Could not find specific head target, using player reference");
        }

        public void OnDespawned()
        {
            KillAllTweens();
            if (droneModel != null) droneModel.gameObject.SetActive(false);
            state = DroneState.Destroyed;
        }

        private void Update()
        {
            if (IsDestroyed || inPortalEntry) return;

            switch (state)
            {
                case DroneState.Idle:
                    TickIdleToAggressive();
                    break;

                case DroneState.Aggressive:
                    TryStartAttack();
                    break;

                // other states are timer/coroutine-driven
            }
        }

        // ================== Hover ==================
        private void StartHoverAnimation()
        {
            KillHover();

            // Set initial Y
            var p = transform.position;
            p.y = baseHoverHeight;
            transform.position = p;

            hoverTween = DOVirtual.Float(0f, 1f, 1f, _ =>
            {
                if (!hoverEnabled) return;
                float t = Time.time * hoverSpeed;
                float y = baseHoverHeight + Mathf.Sin(t) * hoverAmplitude;
                var pos = transform.position;
                pos.y = y;
                transform.position = pos;
            })
            .SetLoops(-1)
            .SetUpdate(UpdateType.Normal, true);
        }

        private void KillHover()
        {
            if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
            hoverTween = null;
        }

        // ================== Direct Aggro (No Wandering) ==================
        private void TickIdleToAggressive()
        {
            if (headTarget == null) return;

            // Brief delay after spawn before becoming aggressive
            float timeSinceSpawn = Time.time - spawnTime;
            if (timeSinceSpawn < initialAggroDelay) return;

            float dist = Vector3.Distance(transform.position, GetHeadPosition());
            float delta = (dist <= detectionRange)
                ? (aggroBuildPerSecond * Time.deltaTime)
                : (-aggroDecayPerSecond * Time.deltaTime);

            aggroValue = Mathf.Clamp01(aggroValue + delta);

            // Face the target while building aggro
            FaceTowards(GetHeadPosition());

            if (aggroValue >= 1f)
            {
                state = DroneState.Aggressive;
                Debug.Log($"{gameObject.name} became aggressive, targeting head position");
            }
        }

        private Vector3 GetHeadPosition()
        {
            if (headTarget == null) return transform.position + transform.forward;
            return headTarget.position + headOffset;
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desired,
                1f - Mathf.Exp(-8f * Time.deltaTime)
            );
        }

        // ================== Attack ==================
        private void TryStartAttack()
        {
            if (headTarget == null || state != DroneState.Aggressive) return;
            StopAllCoroutines();
            StartCoroutine(AttackDashSequence());
        }

        private IEnumerator AttackDashSequence()
        {
            state = DroneState.Telegraphing;

            float tele = telegraphDuration * _telegraphDuration_Internal;
            float t = 0f;
            
            // Telegraph while continuously tracking head
            while (t < tele)
            {
                t += Time.deltaTime;
                FaceTowards(GetHeadPosition());
                yield return null;
            }

            state = DroneState.Dashing;
            hoverEnabled = false;

            Vector3 startPos = transform.position;
            Vector3 headPos = GetHeadPosition();
            Vector3 dir = (headPos - startPos).normalized;

            Vector3 dashTarget = startPos + dir * (dashSpeed * _dashSpeed_Internal * dashTime);
            float elapsed = 0f;
            
            // Dash toward the head position
            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / dashTime);
                transform.position = Vector3.Lerp(startPos, dashTarget, a);
                
                // Keep facing the target during dash
                FaceTowards(headPos);
                yield return null;
            }

            // Despawn immediately after attack
            if (despawnAfterFirstAttack)
            {
                yield return new WaitForSeconds(0.05f);
                DespawnNow();
                yield break;
            }

            // Recovery phase (if not despawning)
            state = DroneState.Recovering;
            hoverEnabled = true;
            yield return new WaitForSeconds(recoverTime / Mathf.Max(0.2f, _attackCooldown_Internal));
            state = DroneState.Idle;
            aggroValue = Mathf.Clamp01(aggroValue - 0.35f);
        }

        // ================== Portal Entry ==================
        public void PlayPortalEntry(Transform portalPoint, float travelTime = 0.45f, float scaleTime = 0.25f)
        {
            if (portalPoint == null) 
            {
                Debug.LogWarning($"{gameObject.name}: PlayPortalEntry called with null portalPoint");
                return;
            }

            StopAllCoroutines();
            KillAllTweens();

            inPortalEntry = true;
            hoverEnabled = false;

            // Ensure drone is positioned exactly at portal point
            Vector3 portalPos = portalPoint.position;
            transform.position = portalPos;
            
            Debug.Log($"{gameObject.name} starting portal entry at position: {portalPos}");

            if (droneModel != null)
                droneModel.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            // Only scale animation, no movement - drone stays at portal position
            if (droneModel != null)
                seq.Append(droneModel.DOScale(Vector3.one, scaleTime).SetEase(Ease.OutBack));
            else
                seq.AppendInterval(scaleTime); // If no model, just wait

            seq.OnComplete(() =>
            {
                inPortalEntry = false;
                hoverEnabled = true;
                StartHoverAnimation();
                state = DroneState.Idle; // Start idle, will become aggressive quickly
                Debug.Log($"{gameObject.name} portal entry complete. Final position: {transform.position}");
            });
        }

        // ================== Utils ==================
        private void KillAllTweens()
        {
            if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
            hoverTween = null;
        }

        public void StunDrone(float duration)
        {
            if (IsDestroyed) return;
            StopAllCoroutines();
            state = DroneState.Stunned;
            DOVirtual.DelayedCall(duration, () =>
            {
                if (!IsDestroyed) state = DroneState.Idle;
            });
        }

        public void ApplyDamage(float amount)
        {
            if (IsDestroyed) return;
            hp -= Mathf.Abs(amount);
            if (hp <= 0f)
            {
                state = DroneState.Destroyed;
                _returnToPool?.Invoke(this);
            }
        }

        public void DespawnNow() => _returnToPool?.Invoke(this);

        /// <summary>
        /// Backward-compat: some scripts call DestroyDrone(). This will despawn the drone.
        /// </summary>
        public void DestroyDrone()
        {
            if (IsDestroyed) return;
            state = DroneState.Destroyed;
            _returnToPool?.Invoke(this);
        }
    }
}