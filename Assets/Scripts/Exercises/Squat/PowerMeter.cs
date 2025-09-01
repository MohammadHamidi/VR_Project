using System.Collections;
using UnityEngine;
using CombatSystem.Events;
using DG.Tweening;

namespace CombatSystem.Combat
{
    public class PowerMeter : MonoBehaviour
    {
        [Header("Power Settings")]
        [SerializeField] private float maxPower = 100f;
        [SerializeField] private float decayPerSecond = 2f;
        [SerializeField] private float perfectSquatPower = 12f;
        [SerializeField] private float validSquatPower = 6f;
        [SerializeField] private float hitPenalty = 15f;

        [Header("Overcharge Settings")]
        [SerializeField] private float overchargeThreshold = 100f;
        [SerializeField] private float overchargeDuration = 8f;
        [SerializeField] private float overchargeStartPower = 60f; // Power after overcharge ends

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem overchargeVFX;
        [SerializeField] private AudioClip overchargeStartSound;
        [SerializeField] private AudioClip overchargeEndSound;
        [SerializeField] private AudioClip powerGainSound;

        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Slider powerSlider;
        [SerializeField] private UnityEngine.UI.Image powerFillImage;
        [SerializeField] private Color normalColor = Color.blue;
        [SerializeField] private Color overchargeColor = Color.gold;

        // Properties
        public float CurrentPower { get; private set; }
        public float PowerPercentage => CurrentPower / maxPower;
        public bool IsOvercharged { get; private set; }
        public float OverchargeTimeRemaining { get; private set; }

        // Private fields
        private AudioSource audioSource;
        private Coroutine overchargeCoroutine;
        private Tween powerBarTween;

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
            InitializePowerMeter();
            SubscribeToEvents();
        }

        void Update()
        {
            if (!IsOvercharged)
            {
                UpdatePowerDecay();
            }
            else
            {
                UpdateOverchargeTimer();
            }
            
            UpdateUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializePowerMeter()
        {
            CurrentPower = 0f;
            IsOvercharged = false;
            OverchargeTimeRemaining = 0f;
            
            // Initialize UI
            if (powerSlider != null)
            {
                powerSlider.minValue = 0f;
                powerSlider.maxValue = maxPower;
                powerSlider.value = CurrentPower;
            }
            
            if (powerFillImage != null)
            {
                powerFillImage.color = normalColor;
            }
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnValidSquat += HandleValidSquat;
            CombatEvents.OnPlayerHit += HandlePlayerHit;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnValidSquat -= HandleValidSquat;
            CombatEvents.OnPlayerHit -= HandlePlayerHit;
        }

        private void UpdatePowerDecay()
        {
            if (CurrentPower > 0)
            {
                float previousPower = CurrentPower;
                CurrentPower = Mathf.Max(0f, CurrentPower - decayPerSecond * Time.deltaTime);
                
                if (CurrentPower != previousPower)
                {
                    CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);
                }
            }
        }

        private void UpdateOverchargeTimer()
        {
            OverchargeTimeRemaining -= Time.deltaTime;
            
            if (OverchargeTimeRemaining <= 0f)
            {
                EndOvercharge();
            }
        }

        private void UpdateUI()
        {
            if (powerSlider != null)
            {
                // Smooth slider animation
                if (powerBarTween != null) powerBarTween.Kill();
                powerBarTween = DOTween.To(() => powerSlider.value, x => powerSlider.value = x, CurrentPower, 0.3f);
            }

            if (powerFillImage != null)
            {
                Color targetColor = IsOvercharged ? overchargeColor : normalColor;
                
                if (IsOvercharged)
                {
                    // Pulsing effect during overcharge
                    float pulse = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                    targetColor = Color.Lerp(normalColor, overchargeColor, pulse);
                }
                
                powerFillImage.color = Color.Lerp(powerFillImage.color, targetColor, Time.deltaTime * 5f);
            }
        }

        private void HandleValidSquat(float depthNorm, float quality)
        {
            if (IsOvercharged) return; // No power gain during overcharge

            bool isPerfect = quality >= 85f;
            float powerGain = isPerfect ? perfectSquatPower : validSquatPower;
            
            AddPower(powerGain);
            
            // Play feedback
            if (audioSource && powerGainSound)
            {
                audioSource.pitch = isPerfect ? 1.2f : 1f;
                audioSource.PlayOneShot(powerGainSound);
            }

            Debug.Log($"Power gained: {powerGain} (Perfect: {isPerfect}, Quality: {quality:F1})");
        }

        private void HandlePlayerHit(Vector3 hitPosition)
        {
            if (IsOvercharged) return; // No penalty during overcharge

            ReducePower(hitPenalty);
            Debug.Log($"Power reduced by {hitPenalty} due to player hit");
        }

        public void AddPower(float amount)
        {
            if (IsOvercharged) return;

            float previousPower = CurrentPower;
            CurrentPower = Mathf.Min(maxPower, CurrentPower + amount);
            
            CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);

            // Check for overcharge threshold
            if (previousPower < overchargeThreshold && CurrentPower >= overchargeThreshold)
            {
                TriggerOvercharge();
            }
        }

        public void ReducePower(float amount)
        {
            if (IsOvercharged) return;

            CurrentPower = Mathf.Max(0f, CurrentPower - amount);
            CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);
        }

        private void TriggerOvercharge()
        {
            if (IsOvercharged) return;

            IsOvercharged = true;
            OverchargeTimeRemaining = overchargeDuration;
            
            // Visual and audio feedback
            if (overchargeVFX) overchargeVFX.Play();
            if (audioSource && overchargeStartSound) audioSource.PlayOneShot(overchargeStartSound);

            // Screen shake or other dramatic effect
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.5f, Vector3.one * 0.1f, 10, 90f);
            }

            // Notify other systems
            CombatEvents.OnOverchargeStateChanged?.Invoke(true);
            
            Debug.Log("OVERCHARGE ACTIVATED!");
        }

        private void EndOvercharge()
        {
            if (!IsOvercharged) return;

            IsOvercharged = false;
            OverchargeTimeRemaining = 0f;
            CurrentPower = overchargeStartPower; // Set power to specific amount after overcharge

            // Stop VFX
            if (overchargeVFX) overchargeVFX.Stop();
            if (audioSource && overchargeEndSound) audioSource.PlayOneShot(overchargeEndSound);

            // Notify other systems
            CombatEvents.OnOverchargeStateChanged?.Invoke(false);
            CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);
            
            Debug.Log("Overcharge ended");
        }

        // Public API for external systems
        public void ResetPower()
        {
            if (overchargeCoroutine != null)
            {
                StopCoroutine(overchargeCoroutine);
                overchargeCoroutine = null;
            }

            IsOvercharged = false;
            CurrentPower = 0f;
            OverchargeTimeRemaining = 0f;
            
            if (overchargeVFX) overchargeVFX.Stop();
            
            CombatEvents.OnOverchargeStateChanged?.Invoke(false);
            CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);
        }

        public void SetPower(float amount)
        {
            CurrentPower = Mathf.Clamp(amount, 0f, maxPower);
            CombatEvents.OnPowerMeterChanged?.Invoke(CurrentPower);
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddPower(float amount)
        {
            AddPower(amount);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTriggerOvercharge()
        {
            SetPower(overchargeThreshold);
        }

        void OnDrawGizmosSelected()
        {
            // Draw power meter visualization in scene view
            Vector3 pos = transform.position + Vector3.up * 2f;
            float width = 2f;
            float height = 0.2f;
            
            // Background
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(pos, new Vector3(width, height, 0.1f));
            
            // Power fill
            float fillPercent = CurrentPower / maxPower;
            Gizmos.color = IsOvercharged ? Color.yellow : Color.blue;
            Vector3 fillPos = pos - Vector3.right * width * 0.5f * (1f - fillPercent);
            Gizmos.DrawCube(fillPos, new Vector3(width * fillPercent, height, 0.12f));
            
            // Overcharge threshold line
            if (overchargeThreshold < maxPower)
            {
                float thresholdPercent = overchargeThreshold / maxPower;
                Vector3 thresholdPos = pos - Vector3.right * width * 0.5f * (1f - thresholdPercent);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(thresholdPos + Vector3.up * height * 0.6f, 
                               thresholdPos - Vector3.up * height * 0.6f);
            }
        }
    }
}
