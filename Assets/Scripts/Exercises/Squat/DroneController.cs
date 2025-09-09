// ... (usings و namespace و enumها و interface بدون تغییر)
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

    public class DroneController : MonoBehaviour, IPoolable
    {
        [Header("Identity")]
        public DroneType type = DroneType.Scout;
        [SerializeField] private bool despawnAfterFirstAttack = true;
        private Vector3 homePosition;
        public void SetHome(Vector3 pos) => homePosition = pos;

        [Header("References")]
        [SerializeField] private Transform droneModel;
        [SerializeField] public Transform player;
        [SerializeField] private SphereCollider attackCollider;

        [Header("Hover")]
        [SerializeField] private float baseHoverHeight = 1.4f;
        [SerializeField] private float hoverAmplitude = 0.15f;
        [SerializeField] private float hoverSpeed = 1.2f;

        [Header("Aggro / Attack")]
        [SerializeField] private float detectionRange = 8.0f; // Increased from 5.0f for better detection
        [SerializeField] private float aggroBuildPerSecond = 2.0f; // Increased from 1.5f for faster aggro
        [SerializeField] private float aggroDecayPerSecond = 0.5f; // Decreased from 0.8f for slower decay
        [SerializeField] private float telegraphDuration = 0.45f;
        [SerializeField] private float dashSpeed = 7.5f; 
        [SerializeField] private float dashTime = 0.34f;
        [SerializeField] private float recoverTime = 0.45f;
        [SerializeField] private float initialAggroDelay = 0.2f; // Reduced from 0.5f for faster response

        [Header("Attack Cooldowns")]
        [SerializeField] private float scoutAttackCooldown = 2.0f;
        [SerializeField] private float heavyAttackCooldown = 3.0f;
        [SerializeField] private float globalAttackDelay = 0.2f; // Reduced from 0.5f for faster attack availability

        [Header("Head Targeting")]
        [SerializeField] private Vector3 headOffset = new Vector3(0f, -0.05f, 0f); // ↓ کمی پایین‌تر از چشم

        [Header("HP")]
        [SerializeField] private float maxHP = 10f;

        [Header("Attack Damage")]
        [SerializeField] private float attackDamage = 1f;

        [SerializeField, Tooltip("0.15–0.25 recommended")]
        private float attackRadius = 0.20f; // ↓ کوچکتر شده

        // ======= Internal state =======
        public DroneState state { get; private set; } = DroneState.Idle;
        public bool IsDestroyed => state == DroneState.Destroyed;

        private float hp;
        private float aggroValue;
        private float spawnTime;
        private Transform headTarget;
        private bool hasDealtDamage = false;
        private float lastAttackTime = -1f;
        private bool canAttack = true;

        // ======= Tweens =======
        private Tween hoverTween;
        private bool hoverEnabled = true;
        private bool inPortalEntry = false;

        // ======= Pooling =======
        private System.Action<IPoolable> _returnToPool;

        // ======= Difficulty multipliers =======
        private float _dashSpeed_Internal = 1f;
        private float _attackCooldown_Internal = 1f;
        private float _telegraphDuration_Internal = 1f;
        private float _wanderSpeed_Internal = 1f; // Added missing field
        [SerializeField] private float hoverHeightOffset = 0;

        // ↓ بافر خیلی کم
        private const float DistanceFudge = 0.01f;

        private void Awake()
        {
            // if (audioSource == null)
            // {
            //     audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            //     audioSource.playOnAwake = false;
            //     audioSource.spatialBlend = 1f;
            // }

            if (attackCollider == null)
                attackCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();

            attackCollider.isTrigger = true;
            attackCollider.radius = attackRadius;
            attackCollider.center = Vector3.zero; // ✅ فیکس 1: مرکز جلو نباشد
            attackCollider.enabled = false;

            Debug.Log($"{gameObject.name}: Attack collider setup - radius:{attackCollider.radius}, center:{attackCollider.center}");
        }

        private void OnEnable()
        {
            inPortalEntry = false;
            hoverEnabled = true;
            hasDealtDamage = false;
            canAttack = false;
            KillAllTweens();
            StartHoverAnimation();
        }

        private void OnDisable()
        {
            KillAllTweens();
            if (attackCollider != null) attackCollider.enabled = false;
        }

        public void SetPool(System.Action<IPoolable> returnToPool) => _returnToPool = returnToPool;

        public void OnSpawned()
        {
            hp = maxHP;
            state = DroneState.Idle;
            aggroValue = 0f;
            inPortalEntry = false;
            hoverEnabled = true;
            spawnTime = Time.time;
            hasDealtDamage = false;
            lastAttackTime = -1f;
            canAttack = false;

            homePosition = transform.position;
            SetupHeadTarget();

            if (headTarget != null)
            {
                var pos = transform.position;
                pos.y = headTarget.position.y + hoverHeightOffset;
                transform.position = pos;
                baseHoverHeight = pos.y; // ✅ سنتر هاور هم‌تراز با سر
            }

            if (droneModel != null)
            {
                droneModel.gameObject.SetActive(true);
                droneModel.localScale = Vector3.one;
            }

            if (attackCollider != null) attackCollider.enabled = false;

            DOVirtual.DelayedCall(globalAttackDelay, () => {
                canAttack = true;
                Debug.Log($"{gameObject.name}: Can attack now! (after {globalAttackDelay}s delay)");
            });

            Debug.Log($"{gameObject.name} spawned at y={transform.position.y:F2}, head={(headTarget != null ? headTarget.name : "null")}, detectionRange={detectionRange}");
        }

        private void SetupHeadTarget()
        {
            if (player != null && player.GetComponent<Camera>() != null)
            {
                headTarget = player; 
                EnsurePlayerHasCollider(player.gameObject); 
                Debug.Log($"{gameObject.name}: Using assigned player camera: {player.name}");
                return;
            }

            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                headTarget = xrOrigin.Camera.transform; 
                player = headTarget;
                EnsurePlayerHasCollider(xrOrigin.Camera.gameObject); 
                Debug.Log($"{gameObject.name}: Found XR Origin camera: {headTarget.name}");
                return;
            }

            if (Camera.main != null)
            {
                headTarget = Camera.main.transform; 
                player = headTarget;
                EnsurePlayerHasCollider(Camera.main.gameObject); 
                Debug.Log($"{gameObject.name}: Using Camera.main: {headTarget.name}");
                return;
            }

            headTarget = player;
            if (player != null) 
            {
                EnsurePlayerHasCollider(player.gameObject);
                Debug.Log($"{gameObject.name}: Using fallback player: {player.name}");
            }
            else
            {
                Debug.LogError($"{gameObject.name}: No player target found! Drone will not attack.");
            }
        }

        private void EnsurePlayerHasCollider(GameObject playerObj)
        {
            if (playerObj == null) return;

            var xrOrigin = playerObj.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null) { Debug.Log($"{gameObject.name}: VR camera detected"); return; }

            var existing = playerObj.GetComponents<Collider>();
            bool ok = false; foreach (var c in existing) if (c.enabled && !c.isTrigger) { ok = true; break; }

            if (!ok)
            {
                var sc = playerObj.GetComponent<SphereCollider>() ?? playerObj.AddComponent<SphereCollider>();
                sc.isTrigger = false; sc.radius = 0.1f; sc.center = Vector3.zero;
            }
        }

        public void OnDespawned()
        {
            KillAllTweens();
            if (droneModel != null) droneModel.gameObject.SetActive(false);
            if (attackCollider != null) attackCollider.enabled = false;
            state = DroneState.Destroyed;
        }

        private void Update()
        {
            if (IsDestroyed || inPortalEntry) return;

            switch (state)
            {
                case DroneState.Idle:       TickIdleToAggressive(); break;
                case DroneState.Aggressive: TryStartAttack(); break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((state == DroneState.Dashing || state == DroneState.Recovering) && !hasDealtDamage && !IsDestroyed)
            {
                if (IsPlayerCollider(other)) { DealDamageToPlayer(); hasDealtDamage = true; }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if ((state == DroneState.Dashing || state == DroneState.Recovering) && !hasDealtDamage && !IsDestroyed)
            {
                if (IsPlayerCollider(other)) { DealDamageToPlayer(); hasDealtDamage = true; }
            }
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other.transform == headTarget) return true;
            if (other.GetComponent<Camera>() != null) return true;
            if (other.name.ToLower().Contains("controller")) return true;
            if (other.CompareTag("Player")) return true;
            return false;
        }

        private void DealDamageToPlayer()
        {
            CombatEvents.OnPlayerTakeDamage?.Invoke(attackDamage);
        }

        private void CheckDistanceBasedCollision()
        {
            if (headTarget == null || hasDealtDamage) return;

            // ✅ فیکس 2: همیشه از موقعیت مدل به‌عنوان مبدأ فاصله استفاده کن
            Vector3 origin = (droneModel != null) ? droneModel.position : transform.position;

            float dist   = Vector3.Distance(origin, GetHeadPosition());
            float radius = (attackCollider != null) ? attackCollider.radius : attackRadius;
            float hit    = radius + DistanceFudge; // ✅ فیکس 3: بافر کوچک

            if (dist <= hit) { DealDamageToPlayer(); hasDealtDamage = true; }
        }

        // ================== Hover ==================
        private void StartHoverAnimation()
        {
            KillHover();

            var p = transform.position; p.y = baseHoverHeight; transform.position = p;

            hoverTween = DOVirtual.Float(0f, 1f, 1f, _ =>
            {
                if (!hoverEnabled) return;
                float t = Time.time * hoverSpeed;
                float y = baseHoverHeight + Mathf.Sin(t) * hoverAmplitude;
                var pos = transform.position; pos.y = y; transform.position = pos;
            })
            .SetLoops(-1).SetUpdate(UpdateType.Normal, true);
        }

        private void KillHover()
        {
            if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
            hoverTween = null;
        }

        private void TickIdleToAggressive()
        {
            if (headTarget == null) 
            {
                Debug.LogWarning($"{gameObject.name}: No head target found!");
                return;
            }

            float timeSinceSpawn = Time.time - spawnTime;
            if (timeSinceSpawn < initialAggroDelay) return;

            float dist = Vector3.Distance(transform.position, GetHeadPosition());
            float delta = (dist <= detectionRange) ? (aggroBuildPerSecond * Time.deltaTime)
                                                   : (-aggroDecayPerSecond * Time.deltaTime);

            aggroValue = Mathf.Clamp01(aggroValue + delta);
            FaceTowards(GetHeadPosition());

            // Debug logging for aggro system
            if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second)
            {
                Debug.Log($"{gameObject.name}: Dist={dist:F2}, DetectionRange={detectionRange}, Aggro={aggroValue:F2}, State={state}");
            }

            if (aggroValue >= 1f) 
            {
                Debug.Log($"{gameObject.name}: Transitioning to Aggressive state!");
                state = DroneState.Aggressive;
            }
        }

        private Vector3 GetHeadPosition()
        {
            if (headTarget == null) return transform.position + transform.forward;
            return headTarget.position + headOffset;
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = (target - transform.position); dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        // ================== Attack with Cooldowns ==================
        private void TryStartAttack()
        {
            if (headTarget == null) 
            {
                Debug.LogWarning($"{gameObject.name}: Cannot attack - no head target!");
                return;
            }
            if (state != DroneState.Aggressive) 
            {
                Debug.LogWarning($"{gameObject.name}: Cannot attack - not in Aggressive state (current: {state})");
                return;
            }
            if (!canAttack) 
            {
                Debug.LogWarning($"{gameObject.name}: Cannot attack - canAttack is false");
                return;
            }

            float cooldown = (type == DroneType.Scout) ? scoutAttackCooldown : heavyAttackCooldown;
            cooldown *= _attackCooldown_Internal;

            if (lastAttackTime >= 0 && Time.time - lastAttackTime < cooldown) 
            {
                Debug.Log($"{gameObject.name}: Attack on cooldown. Time left: {cooldown - (Time.time - lastAttackTime):F2}s");
                return;
            }

            Debug.Log($"{gameObject.name}: Starting attack sequence!");
            lastAttackTime = Time.time;
            StopAllCoroutines();
            StartCoroutine(AttackDashSequence());
        }

        private IEnumerator AttackDashSequence()
        {
            state = DroneState.Telegraphing;
            hasDealtDamage = false;

            float tele = telegraphDuration * _telegraphDuration_Internal;
            float t = 0f;
            while (t < tele) { t += Time.deltaTime; FaceTowards(GetHeadPosition()); yield return null; }

            state = DroneState.Dashing;
            hoverEnabled = false;

            if (attackCollider != null) attackCollider.enabled = true;

            Vector3 startPos = transform.position;
            Vector3 dashTarget = GetHeadPosition(); // هدف لحظه‌ی شروع

            float elapsed = 0f;
            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / dashTime);
                transform.position = Vector3.Lerp(startPos, dashTarget, k);

                FaceTowards(GetHeadPosition());
                CheckDistanceBasedCollision(); // دَمج جدا از حرکت
                yield return null;
            }

            // ✅ فیکس 4: در پایان دَش دقیقاً روی dashTarget قرار بگیر
            transform.position = dashTarget;
            CheckDistanceBasedCollision();

            // پنجره‌ی کوتاه برای ثبت برخورد (بدون تلپورت دنبال کردن سر)
            float endWindow = 0.12f;
            float until = Time.time + endWindow;
            while (!hasDealtDamage && Time.time < until)
            {
                CheckDistanceBasedCollision();
                yield return new WaitForFixedUpdate();
            }

            if (attackCollider != null) attackCollider.enabled = false;

            if (despawnAfterFirstAttack || type == DroneType.Scout)
            {
                yield return new WaitForSeconds(0.2f);
                DespawnNow();
                yield break;
            }

            state = DroneState.Recovering;
            hoverEnabled = true;
            yield return new WaitForSeconds(recoverTime);
            state = DroneState.Idle;
            aggroValue = Mathf.Clamp01(aggroValue - 0.35f);
        }

        // ================== Portal Entry / Utils / Damage / Destroy (بدون تغییر معنادار) ==================
        public void PlayPortalEntry(Transform portalPoint, float travelTime = 0.45f, float scaleTime = 0.25f)
        {
            if (portalPoint == null) { Debug.LogWarning($"{gameObject.name}: PlayPortalEntry called with null portalPoint"); return; }
            StopAllCoroutines(); KillAllTweens();
            inPortalEntry = true; hoverEnabled = false;
            transform.position = portalPoint.position;
            if (droneModel != null) droneModel.localScale = Vector3.zero;
            Sequence seq = DOTween.Sequence();
            if (droneModel != null) seq.Append(droneModel.DOScale(Vector3.one, scaleTime).SetEase(Ease.OutBack));
            else seq.AppendInterval(scaleTime);
            seq.OnComplete(() => { inPortalEntry = false; hoverEnabled = true; StartHoverAnimation(); state = DroneState.Idle; });
        }

        private void KillAllTweens() { if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill(); hoverTween = null; }
        public void StunDrone(float duration){ if (IsDestroyed) return; StopAllCoroutines(); state = DroneState.Stunned; DOVirtual.DelayedCall(duration, ()=>{ if (!IsDestroyed) state = DroneState.Idle; }); }
        public void ApplyDamage(float amount){ if (IsDestroyed) return; hp -= Mathf.Abs(amount); if (hp <= 0f){ CombatEvents.OnDroneDestroyed?.Invoke(this); state = DroneState.Destroyed; _returnToPool?.Invoke(this);} }
        public void DespawnNow() => _returnToPool?.Invoke(this);
        public void DestroyDrone(){ if (IsDestroyed) return; CombatEvents.OnDroneDestroyed?.Invoke(this); state = DroneState.Destroyed; _returnToPool?.Invoke(this); }

        public bool CanAttackNow()
        {
            if (!canAttack) return false;
            float cd = (type == DroneType.Scout) ? scoutAttackCooldown : heavyAttackCooldown;
            cd *= _attackCooldown_Internal;
            return lastAttackTime < 0 || Time.time - lastAttackTime >= cd;
        }
    }
}
