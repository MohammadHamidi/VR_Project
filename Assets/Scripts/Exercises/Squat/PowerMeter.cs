using UnityEngine;
using UnityEngine.UI;
using CombatSystem.Events;
using System.Collections;

namespace CombatSystem.Combat
{
    public class PowerMeter : MonoBehaviour
    {
        [Header("Power Settings")]
        [SerializeField] private float maxPower = 100f;
        [SerializeField] private float powerPerSquat = 15f;
        [SerializeField] private float perfectSquatBonus = 10f;
        [SerializeField] private float powerDecayRate = 5f;
        [SerializeField] private float shockwaveCost = 50f;
        [SerializeField] private bool enablePowerDecay = true;
        [SerializeField] private float comboMultiplier = 0.1f;
        [SerializeField] private float overchargeThreshold = 90f; // EXPLICIT: Overcharge threshold

        [Header("UI References")]
        [SerializeField] private Slider powerSlider;
        [SerializeField] private Text powerText;
        [SerializeField] private Image powerFill;
        [SerializeField] private Button shockwaveButton;
        [SerializeField] private Text shockwaveButtonText;

        [Header("Visual Feedback")]
        [SerializeField] private Color lowPowerColor = Color.red;
        [SerializeField] private Color mediumPowerColor = Color.yellow;
        [SerializeField] private Color highPowerColor = Color.green;
        [SerializeField] private Color maxPowerColor = Color.cyan;
        [SerializeField] private ParticleSystem powerGainEffect;
        [SerializeField] private ParticleSystem maxPowerEffect;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip powerGainSound;
        [SerializeField] private AudioClip maxPowerSound;
        [SerializeField] private AudioClip shockwaveSound;

        [Header("Shockwave Integration")]
        [SerializeField] private ShockwaveEmitter shockwaveEmitter;

        // EXPLICIT PROPERTIES - These should fix compilation errors
        public float CurrentPower { get; private set; }
        public float PowerPercentage => CurrentPower / maxPower;
        public bool CanUseShockwave => CurrentPower >= shockwaveCost;
        public bool IsMaxPower => Mathf.Approximately(CurrentPower, maxPower);
        
        // EXPLICIT: IsOvercharged property that was missing
        public bool IsOvercharged 
        { 
            get { return CurrentPower >= overchargeThreshold; } 
        }

        private AudioSource audioSource;
        private bool wasMaxPower = false;
        private bool wasOvercharged = false;
        private int currentCombo = 0;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }

            if (shockwaveEmitter == null)
            {
                shockwaveEmitter = FindObjectOfType<ShockwaveEmitter>();
            }

            if (shockwaveButton != null && shockwaveButtonText == null)
            {
                shockwaveButtonText = shockwaveButton.GetComponentInChildren<Text>();
            }
        }

        private void Start()
        {
            SubscribeToEvents();
            ResetPower();
            UpdateUI();

            if (shockwaveButton != null)
            {
                shockwaveButton.onClick.AddListener(TryUseShockwave);
            }
        }

        private void Update()
        {
            if (enablePowerDecay && CurrentPower > 0)
            {
                float decay = powerDecayRate * Time.deltaTime;
                ModifyPower(-decay, false);
            }
            UpdateUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // EXPLICIT EVENT SUBSCRIPTIONS - Fixed to match your CombatEvents signatures
        private void SubscribeToEvents()
        {
            // Core events
            CombatEvents.OnValidSquat += HandleValidSquat;
            CombatEvents.OnPerfectSquat += HandlePerfectSquat;
            CombatEvents.OnShockwaveActivated += HandleShockwaveUsed;
            
            // FIXED: Using correct event signatures from your CombatEvents
            CombatEvents.OnComboChanged += HandleComboChanged_Float;  // Action<float>
            CombatEvents.OnPlayerDodge += HandlePlayerDodge;
            CombatEvents.OnDroneDestroyed += HandleDroneDestroyed_WithController;  // Action<DroneController>
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnValidSquat -= HandleValidSquat;
            CombatEvents.OnPerfectSquat -= HandlePerfectSquat;
            CombatEvents.OnShockwaveActivated -= HandleShockwaveUsed;
            CombatEvents.OnComboChanged -= HandleComboChanged_Float;
            CombatEvents.OnPlayerDodge -= HandlePlayerDodge;
            CombatEvents.OnDroneDestroyed -= HandleDroneDestroyed_WithController;
        }

        // EXPLICIT EVENT HANDLERS - Matching exact signatures from your CombatEvents
        private void HandleValidSquat(float depthNorm, float quality)
        {
            float basePowerGain = powerPerSquat * depthNorm * (quality / 100f);
            float comboBonus = basePowerGain * (currentCombo * comboMultiplier);
            float totalGain = basePowerGain + comboBonus;
            
            ModifyPower(totalGain, true);
            Debug.Log($"Power gained from squat: {totalGain:F1}");
        }

        private void HandlePerfectSquat(float quality)
        {
            float bonus = perfectSquatBonus;
            ModifyPower(bonus, true);
            Debug.Log($"Perfect squat bonus: {bonus:F1} power!");
        }

        private void HandleShockwaveUsed()
        {
            if (audioSource != null && shockwaveSound != null)
            {
                audioSource.PlayOneShot(shockwaveSound);
            }

            if (shockwaveEmitter != null)
            {
                shockwaveEmitter.TriggerShockwave();
            }
            else
            {
                CombatEvents.OnShockwaveTriggered?.Invoke(transform.position);
            }
        }

        // FIXED: Signature matches Action<float> from your CombatEvents
        private void HandleComboChanged_Float(float newCombo)
        {
            currentCombo = (int)newCombo;
        }

        private void HandlePlayerDodge()
        {
            ModifyPower(2f, false);
        }

        // FIXED: Signature matches Action<DroneController> from your CombatEvents
        private void HandleDroneDestroyed_WithController(CombatSystem.Drones.DroneController drone)
        {
            ModifyPower(3f, false);
        }

        private void ModifyPower(float amount, bool playFeedback = true)
        {
            float oldPower = CurrentPower;
            bool wasOverchargedBefore = IsOvercharged;
            
            CurrentPower = Mathf.Clamp(CurrentPower + amount, 0f, maxPower);

            // Check for overcharge state change
            bool isOverchargedNow = IsOvercharged;
            if (isOverchargedNow != wasOverchargedBefore)
            {
                CombatEvents.OnOverchargeStateChanged?.Invoke(isOverchargedNow);
                wasOvercharged = isOverchargedNow;
            }

            if (amount > 0)
            {
                CombatEvents.OnPowerGained?.Invoke(amount);
                if (playFeedback) PlayPowerGainFeedback();
            }
            else if (amount < 0)
            {
                CombatEvents.OnPowerSpent?.Invoke(Mathf.Abs(amount));
            }

            CombatEvents.OnPowerChanged?.Invoke(CurrentPower);

            if (!wasMaxPower && IsMaxPower)
            {
                PlayMaxPowerFeedback();
            }

            wasMaxPower = IsMaxPower;
        }

        private void PlayPowerGainFeedback()
        {
            if (audioSource != null && powerGainSound != null)
            {
                audioSource.pitch = 1f + (PowerPercentage * 0.5f);
                audioSource.PlayOneShot(powerGainSound);
            }

            if (powerGainEffect != null)
            {
                powerGainEffect.Play();
            }
        }

        private void PlayMaxPowerFeedback()
        {
            if (audioSource != null && maxPowerSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(maxPowerSound);
            }

            if (maxPowerEffect != null)
            {
                maxPowerEffect.Play();
            }

            Debug.Log("MAX POWER ACHIEVED!");
        }

        private void UpdateUI()
        {
            if (powerSlider != null)
            {
                powerSlider.value = PowerPercentage;
            }

            if (powerText != null)
            {
                powerText.text = $"Power: {CurrentPower:F0}/{maxPower:F0}";
                if (currentCombo > 0)
                {
                    powerText.text += $" (x{currentCombo + 1})";
                }
            }

            if (powerFill != null)
            {
                powerFill.color = GetPowerColor();
            }

            if (shockwaveButton != null)
            {
                shockwaveButton.interactable = CanUseShockwave;
                
                if (shockwaveButtonText != null)
                {
                    shockwaveButtonText.text = CanUseShockwave ? 
                        $"Shockwave ({shockwaveCost:F0})" : 
                        $"Need {(shockwaveCost - CurrentPower):F0} More";
                }
            }
        }

        private Color GetPowerColor()
        {
            float percentage = PowerPercentage;

            if (percentage >= 1f)
                return maxPowerColor;
            else if (percentage >= 0.75f)
                return Color.Lerp(highPowerColor, maxPowerColor, (percentage - 0.75f) * 4f);
            else if (percentage >= 0.5f)
                return Color.Lerp(mediumPowerColor, highPowerColor, (percentage - 0.5f) * 4f);
            else if (percentage >= 0.25f)
                return Color.Lerp(lowPowerColor, mediumPowerColor, (percentage - 0.25f) * 4f);
            else
                return lowPowerColor;
        }

        // Public API
        public void TryUseShockwave()
        {
            if (!CanUseShockwave)
            {
                Debug.Log($"Not enough power for shockwave! Need {shockwaveCost}, have {CurrentPower:F1}");
                return;
            }

            ModifyPower(-shockwaveCost, false);
            CombatEvents.OnShockwaveActivated?.Invoke();
            Debug.Log($"Shockwave activated! Power reduced by {shockwaveCost}");
        }

        public void AddPower(float amount)
        {
            if (amount > 0) ModifyPower(amount, true);
        }

        public void SpendPower(float amount)
        {
            if (amount > 0) ModifyPower(-amount, false);
        }

        public void ResetPower()
        {
            CurrentPower = 0f;
            wasMaxPower = false;
            wasOvercharged = false;
            currentCombo = 0;
            CombatEvents.OnPowerChanged?.Invoke(CurrentPower);
            UpdateUI();
            Debug.Log("Power meter reset");
        }

        public void SetMaxPower()
        {
            ModifyPower(maxPower - CurrentPower, true);
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddPower(float amount = 10f)
        {
            AddPower(amount);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugMaxPower()
        {
            SetMaxPower();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugUseShockwave()
        {
            TryUseShockwave();
        }
    }
}