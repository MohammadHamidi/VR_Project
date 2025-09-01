using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using CombatSystem.Events;

namespace CombatSystem.Player
{
    public class SquatDodge : MonoBehaviour
    {
        public static SquatDodge Instance { get; private set; }

        [Header("Squat Detection")]
        [SerializeField] private Transform xrCamera;
        [SerializeField] private float targetDepth = 0.30f; // meters
        [SerializeField] private float validThreshold = 0.8f; // depthNorm
        [SerializeField] private float dwellMin = 0.3f;
        [SerializeField] private float dwellMax = 1.2f;
        [SerializeField] private float dodgeDuration = 0.5f;
        [SerializeField] private float cooldownDuration = 0.2f;

        [Header("Quality Assessment")]
        [SerializeField] private float perfectQualityThreshold = 85f;
        [SerializeField] private float smoothingFactor = 0.15f; // For EWMA smoothing
        [SerializeField] private float velocitySmoothing = 0.2f;

        [Header("Dodge Feedback")]
        [SerializeField] private AudioClip dodgeSound;
        [SerializeField] private AudioClip perfectSquatSound;
        [SerializeField] private ParticleSystem dodgeEffect;
        [SerializeField] private ParticleSystem perfectSquatEffect;

        public bool IsDodging { get; private set; }
        public float CurrentSquatDepth { get; private set; }
        public float CurrentDepthNorm { get; private set; }
        public bool IsOnCooldown { get; private set; }
        public float BaselineHeight { get; private set; }

        private AudioSource _audioSource;
        private Coroutine _dodgeCoroutine;
        private Coroutine _cooldownCoroutine;
        
        // Enhanced squat detection
        private float smoothedY;
        private float velocity;
        private bool inBottom;
        private float bottomEnterTime;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            if (xrCamera == null)
            {
                // Try to find XR Camera first
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    xrCamera = xrOrigin.Camera.transform;
                }
                else
                {
                    xrCamera = Camera.main?.transform;
                }
            }
        }

        void Start()
        {
            CalibrateStandingHeight();
        }

        void Update()
        {
            CheckSquatPosition();
        }

        private void CheckSquatPosition()
        {
            if (xrCamera == null) return;

            float y = xrCamera.position.y;
            smoothedY = Mathf.Lerp(smoothedY, y, smoothingFactor); // EWMA smoothing
            float depth = Mathf.Max(0f, BaselineHeight - smoothedY);
            float depthNorm = Mathf.Clamp01(depth / targetDepth);
            
            // Update velocity for stability calculation
            velocity = Mathf.Lerp(velocity, (y - smoothedY) / Time.deltaTime, velocitySmoothing);

            CurrentSquatDepth = depth;
            CurrentDepthNorm = depthNorm;

            CombatEvents.OnPlayerSquatDepthChanged?.Invoke(CurrentSquatDepth);

            // Bottom detection for quality squat assessment
            if (!inBottom && depthNorm >= validThreshold)
            {
                inBottom = true;
                bottomEnterTime = Time.time;
            }
            else if (inBottom && depthNorm < validThreshold - 0.1f) // Ascent with hysteresis
            {
                float dwell = Time.time - bottomEnterTime;
                if (dwell >= dwellMin && dwell <= dwellMax)
                {
                    // Calculate squat quality
                    float tempoScore = Mathf.Clamp01((0.7f - Mathf.Abs(dwell - 0.7f)) / 0.7f);
                    float stabilityScore = Mathf.Clamp01(1f - Mathf.Abs(velocity) * 0.02f);
                    float quality = 50f * depthNorm + 25f * tempoScore + 25f * stabilityScore;
                    
                    // Emit valid squat event
                    CombatEvents.OnValidSquat?.Invoke(depthNorm, quality);
                    
                    // Play feedback based on quality
                    PlaySquatFeedback(quality >= perfectQualityThreshold);
                    
                    Debug.Log($"Valid squat: Depth={depthNorm:F2}, Quality={quality:F1}, Dwell={dwell:F2}s");
                }
                inBottom = false;
            }

            // Trigger dodge if squatting deep enough (for laser dodging)
            if (CurrentSquatDepth >= targetDepth * validThreshold && !IsDodging && !IsOnCooldown)
            {
                TriggerDodge();
            }
        }

        private void TriggerDodge()
        {
            if (_dodgeCoroutine != null)
                StopCoroutine(_dodgeCoroutine);
            
            _dodgeCoroutine = StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            IsDodging = true;
            CombatEvents.OnPlayerDodge?.Invoke();
            PlayDodgeFeedback();

            yield return new WaitForSeconds(dodgeDuration);
            IsDodging = false;

            if (_cooldownCoroutine != null)
                StopCoroutine(_cooldownCoroutine);
            
            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            IsOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            IsOnCooldown = false;
        }

        private void PlayDodgeFeedback()
        {
            if (_audioSource != null && dodgeSound != null)
                _audioSource.PlayOneShot(dodgeSound);

            if (dodgeEffect != null)
                dodgeEffect.Play();
        }

        private void PlaySquatFeedback(bool isPerfect)
        {
            if (_audioSource != null)
            {
                AudioClip soundToPlay = isPerfect && perfectSquatSound != null ? perfectSquatSound : dodgeSound;
                if (soundToPlay != null)
                {
                    _audioSource.pitch = isPerfect ? 1.2f : 1f;
                    _audioSource.PlayOneShot(soundToPlay);
                }
            }

            if (isPerfect && perfectSquatEffect != null)
            {
                perfectSquatEffect.Play();
            }
            else if (dodgeEffect != null)
            {
                dodgeEffect.Play();
            }
        }

        public void CalibrateStandingHeight()
        {
            if (xrCamera != null)
            {
                BaselineHeight = xrCamera.position.y;
                smoothedY = BaselineHeight; // Initialize smoothed value
                Debug.Log($"Standing height calibrated to: {BaselineHeight:F2}m");
            }
        }

        public void RecalibrateStanding(float sampleSeconds = 1f)
        {
            StartCoroutine(CoRecalibrate(sampleSeconds));
        }

        private IEnumerator CoRecalibrate(float sampleTime)
        {
            if (xrCamera == null) yield break;

            float sum = 0f;
            int count = 0;
            float endTime = Time.time + sampleTime;

            while (Time.time < endTime)
            {
                sum += xrCamera.position.y;
                count++;
                yield return null;
            }

            if (count > 0)
            {
                BaselineHeight = sum / count;
                smoothedY = BaselineHeight;
                Debug.Log($"Standing height recalibrated to: {BaselineHeight:F2}m over {sampleTime}s");
            }
        }
    }
}