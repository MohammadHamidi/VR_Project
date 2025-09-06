// CombatSystem/Drones/DroneController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CombatSystem.Events;

namespace CombatSystem.Drones
{
    public enum DroneType { Scout, Heavy }
    public enum DroneState { Idle, Wandering, Aggressive, Telegraphing, Dashing, Recovering, Stunned, Destroyed }

    public interface IPoolable
    {
        void SetPool(System.Action<IPoolable> returnToPool);
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// رفتار پهپاد + ورودی پورتال با حرکت نرم.
    /// </summary>
    public class DroneController : MonoBehaviour, IPoolable
    {
        [Header("Identity")]
        public DroneType type = DroneType.Scout;
        [SerializeField] private bool wanderAroundHome = true;          // نزدیک محل اسپاون بچرخه تا وقتی آگرو بگیرد
        [SerializeField] private bool despawnAfterFirstAttack = true;    // بعد از اولین دَش برگرده به پول
        private Vector3 homePosition;                                    // محل اسپاون
        public void SetHome(Vector3 pos) => homePosition = pos;

        [Header("References")]
        [SerializeField] private Transform droneModel;    // بدنه بصری
        [SerializeField] private AudioSource audioSource; // در Awake ساخته می‌شود اگر نبود
        [SerializeField] public Transform player;        // هدف

        [Header("Hover")]
        [SerializeField] private float baseHoverHeight = 1.4f;
        [SerializeField] private float hoverAmplitude = 0.15f;
        [SerializeField] private float hoverSpeed = 1.2f;

        [Header("Wander (Range & Bias)")]
        [SerializeField] private float wanderRadius = 3.0f;
        [SerializeField] private float wanderForwardBias = 2.2f;

        [Header("Wander (Speed & Smoothness)")]
        [SerializeField] private float wanderMoveSpeed = 2.0f;          // حداکثر سرعت حرکت
        [SerializeField] private float wanderSmoothTime = 0.55f;        // زمان هموارسازی برای SmoothDamp
        [SerializeField] private float wanderTargetResponsiveness = 2.5f;// نرخ هموارسازی هدف (بزرگتر = تندتر)
        [SerializeField] private float wanderNoiseSpeed = 0.15f;        // سرعت تغییر نویز
        [SerializeField] private float wanderNoiseScale = 1.0f;         // شدت جابجایی نویزی داخل شعاع
        [SerializeField] private float minWanderDistance = 0.5f;        // فقط برای سازگاری با مقادیر قبلی

        [Header("Aggro / Attack")]
        [SerializeField] private float detectionRange = 5.0f;
        [SerializeField] private float aggroBuildPerSecond = 0.6f;
        [SerializeField] private float aggroDecayPerSecond = 0.8f;
        [SerializeField] private float telegraphDuration = 0.45f;
        [SerializeField] private float minWanderTime = 2.0f; // زمان حداقل قبل از تهاجمی شدن
        [SerializeField] private float dashSpeed = 7.5f;
        [SerializeField] private float dashTime = 0.34f;
        [SerializeField] private float recoverTime = 0.45f;

        [Header("HP")]
        [SerializeField] private float maxHP = 10f;

        // ======= Internal state =======
        public DroneState state { get; private set; } = DroneState.Idle;
        public bool IsDestroyed => state == DroneState.Destroyed;

        private float hp;
        private float aggroValue;
        private Vector3 wanderTarget;
        private float spawnTime; // Track when drone was spawned to enforce minimum wander time

        // Smooth wandering state
        private Vector3 wanderVelocity;         // for SmoothDamp
        private float noiseSeedX, noiseSeedZ;   // coherent Perlin seeds

        // ======= Tweens =======
        private Tween hoverTween;
        private Tween moveTween; // kept for compatibility with non-wander moves
        private bool hoverEnabled = true;
        private bool inPortalEntry = false;

        // ======= Pooling =======
        private System.Action<IPoolable> _returnToPool;

        // ======= Difficulty multipliers (set via reflection by spawner) =======
        private float _wanderSpeed_Internal = 1f;
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
            state = DroneState.Wandering; // شروع مستقیم با حالت پرسه‌زنی نرم
            aggroValue = 0f;
            inPortalEntry = false;
            hoverEnabled = true;
            spawnTime = Time.time;

            homePosition = transform.position; // 🟢 محل اسپاون را ذخیره کن

            if (droneModel != null)
            {
                droneModel.gameObject.SetActive(true);
                droneModel.localScale = Vector3.one;
            }

            // initialize smooth wander
            wanderTarget = transform.position;
            wanderVelocity = Vector3.zero;
            noiseSeedX = Random.Range(0f, 1000f);
            noiseSeedZ = Random.Range(0f, 1000f);
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
                case DroneState.Wandering:
                    TickWanderSmooth();
                    TickAggro();
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

            // set initial Y
            var p = transform.position;
            p.y = baseHoverHeight;
            transform.position = p;

            // Use a DOVirtual.Float driver (returns Tweener/Tween) — safe to SetLoops
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

        // ================== Wander (SMOOTH / PERLIN) ==================
        /// <summary>
        /// Smooth, subtle wandering using coherent Perlin noise + SmoothDamp (no tweens).
        /// Always keeps Y from hover, only steers on XZ.
        /// </summary>
        private void TickWanderSmooth()
        {
            // choose the anchor
            Vector3 anchor = wanderAroundHome
                ? homePosition
                : (player != null ? player.position : transform.position);

            // forward bias (flattened)
            Vector3 fwd = (player != null ? player.forward : transform.forward);
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;

            // coherent offset from Perlin noise (smooth over time)
            float t = Time.time * wanderNoiseSpeed;
            float nx = Mathf.PerlinNoise(noiseSeedX, t) * 2f - 1f;
            float nz = Mathf.PerlinNoise(noiseSeedZ, t) * 2f - 1f;

            Vector3 noiseOffset = new Vector3(nx, 0f, nz) * (wanderRadius * wanderNoiseScale);
            Vector3 desired = anchor + noiseOffset + fwd * wanderForwardBias;
            desired.y = baseHoverHeight;

            // low-pass filter the target itself (prevents "snapping" to new noise samples)
            float targetAlpha = 1f - Mathf.Exp(-wanderTargetResponsiveness * Time.deltaTime);
            wanderTarget = Vector3.Lerp(wanderTarget, desired, targetAlpha);

            // move smoothly toward the (filtered) target on XZ only
            Vector3 pos = transform.position;
            Vector3 targetXZ = new Vector3(wanderTarget.x, pos.y, wanderTarget.z);
            float maxSpeed = Mathf.Max(0.01f, wanderMoveSpeed * _wanderSpeed_Internal);

            transform.position = Vector3.SmoothDamp(
                pos, targetXZ, ref wanderVelocity, wanderSmoothTime, maxSpeed, Time.deltaTime
            );

            // gentle facing toward travel direction/target
            FaceTowards(targetXZ);
        }

        // ================== (Legacy) Tweened Move API kept for compatibility ==================
        private void MoveTo(Vector3 target, float speed)
        {
            // Kept for non-wander motions if needed; wandering no longer calls this.
            KillMove();
            hoverEnabled = false;

            float dist = Vector3.Distance(transform.position, target);
            float dur = dist / Mathf.Max(0.001f, speed);

            moveTween = transform.DOMove(target, dur)
                .SetEase(Ease.OutSine)
                .OnComplete(() => { hoverEnabled = true; });
        }

        private void KillMove()
        {
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
            moveTween = null;
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

        // ================== Aggro / Attack ==================
        private void TickAggro()
        {
            if (player == null) return;

            // Enforce minimum wander time before becoming aggressive
            float timeSinceSpawn = Time.time - spawnTime;
            if (timeSinceSpawn < minWanderTime) return;

            float dist = Vector3.Distance(transform.position, player.position);
            float delta = (dist <= detectionRange)
                ? (aggroBuildPerSecond * Time.deltaTime)
                : (-aggroDecayPerSecond * Time.deltaTime);

            aggroValue = Mathf.Clamp01(aggroValue + delta);

            if (aggroValue >= 1f && state == DroneState.Wandering)
            {
                state = DroneState.Aggressive;
                Debug.Log($"{gameObject.name} became aggressive after {timeSinceSpawn:F1}s wander time");
            }
            else if (aggroValue <= 0f && state == DroneState.Aggressive)
            {
                state = DroneState.Wandering;
            }
        }

        private void TryStartAttack()
        {
            if (player == null || state != DroneState.Aggressive) return;
            StopAllCoroutines();
            StartCoroutine(AttackDashSequence());
        }

        private IEnumerator AttackDashSequence()
        {
            state = DroneState.Telegraphing;

            float tele = telegraphDuration * _telegraphDuration_Internal;
            float t = 0f;
            while (t < tele)
            {
                t += Time.deltaTime;
                FaceTowards(player != null ? player.position : transform.position + transform.forward);
                yield return null;
            }

            state = DroneState.Dashing;
            hoverEnabled = false;
            KillMove();

            Vector3 startPos = transform.position;
            Vector3 dir = (player != null ? (player.position - transform.position) : transform.forward);
            dir.y = 0f; dir.Normalize();

            Vector3 dashTarget = startPos + dir * (dashSpeed * _dashSpeed_Internal * dashTime);
            float elapsed = 0f;
            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / dashTime);
                transform.position = Vector3.Lerp(startPos, dashTarget, a);
                FaceTowards(dashTarget);
                yield return null;
            }

            // 🟢 همین‌جا پهپاد را به پول برگردان (بدون توجه به برخورد)
            if (despawnAfterFirstAttack)
            {
                yield return new WaitForSeconds(0.05f); // کوچولو برای افکت
                DespawnNow();
                yield break;
            }

            // اگر خواستی قدیمی بماند:
            state = DroneState.Recovering;
            hoverEnabled = true;
            yield return new WaitForSeconds(recoverTime / Mathf.Max(0.2f, _attackCooldown_Internal));
            state = DroneState.Wandering;
            aggroValue = Mathf.Clamp01(aggroValue - 0.35f);
        }

        // ================== Portal Entry ==================
        public void PlayPortalEntry(Transform portalPoint, float travelTime = 0.45f, float scaleTime = 0.25f)
        {
            if (portalPoint == null) return;

            StopAllCoroutines();
            KillAllTweens();

            inPortalEntry = true;
            hoverEnabled = false;

            Vector3 finalPos = transform.position;
            Vector3 startPos = portalPoint.position;

            if (droneModel != null)
                droneModel.localScale = Vector3.zero;

            transform.position = startPos;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(finalPos, travelTime).SetEase(Ease.OutQuad));
            if (droneModel != null)
                seq.Join(droneModel.DOScale(Vector3.one, scaleTime).SetEase(Ease.OutBack));

            seq.OnComplete(() =>
            {
                inPortalEntry = false;
                hoverEnabled = true;
                StartHoverAnimation();
                state = DroneState.Wandering;
                Debug.Log($"{gameObject.name} portal entry complete, now wandering. Player: {(player != null ? "assigned" : "null")}");
            });
        }

        // ================== Utils ==================
        private void KillAllTweens()
        {
            if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
            hoverTween = moveTween = null;
        }

        public void StunDrone(float duration)
        {
            if (IsDestroyed) return;
            StopAllCoroutines();
            state = DroneState.Stunned;
            DOVirtual.DelayedCall(duration, () =>
            {
                if (!IsDestroyed) state = DroneState.Wandering;
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

        // ===== Compatibility with existing callers (e.g., ShockwaveEmitter) =====
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
