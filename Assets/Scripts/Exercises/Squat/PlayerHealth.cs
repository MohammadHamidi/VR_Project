using UnityEngine;
using CombatSystem.Events;
using System.Collections;

namespace CombatSystem.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilityTime = 1.5f;
        [SerializeField] private bool enableInvulnerabilityFlash = true;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip takeDamageSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Visual Feedback")]
        [SerializeField] private CanvasGroup damageOverlay; // Red screen overlay
        [SerializeField] private float flashDuration = 0.3f;

        // Properties
        public int CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        private AudioSource audioSource;
        private float invulnerabilityTimer;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        private void Start()
        {
            CurrentHealth = maxHealth;
            
            // CRITICAL: Subscribe to damage events from drones
            CombatEvents.OnPlayerTakeDamage += HandleTakeDamage;
            
            // Initialize UI
            CombatEvents.OnLivesChanged?.Invoke(CurrentHealth);
            
            Debug.Log($"PlayerHealth initialized with {CurrentHealth} health");
        }

        private void Update()
        {
            // Handle invulnerability timer
            if (IsInvulnerable)
            {
                invulnerabilityTimer -= Time.deltaTime;
                if (invulnerabilityTimer <= 0)
                {
                    IsInvulnerable = false;
                    Debug.Log("Invulnerability ended");
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            CombatEvents.OnPlayerTakeDamage -= HandleTakeDamage;
        }

        private void HandleTakeDamage(float damage)
        {
            // Ignore damage if invulnerable or dead
            if (IsInvulnerable || !IsAlive) 
            {
                Debug.Log($"Damage ignored - Invulnerable: {IsInvulnerable}, Alive: {IsAlive}");
                return;
            }

            // Apply damage
            int damageAmount = Mathf.RoundToInt(damage);
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);

            // Start invulnerability period
            IsInvulnerable = true;
            invulnerabilityTimer = invulnerabilityTime;

            // Play feedback
            PlayDamageFeedback();

            // Update UI
            CombatEvents.OnLivesChanged?.Invoke(CurrentHealth);

            Debug.Log($"Player took {damageAmount} damage. Health: {CurrentHealth}/{maxHealth}");

            // Check if dead
            if (CurrentHealth <= 0)
            {
                HandleDeath();
            }
        }

        private void PlayDamageFeedback()
        {
            // Play damage sound
            if (audioSource != null && takeDamageSound != null)
            {
                audioSource.PlayOneShot(takeDamageSound);
            }

            // Flash damage overlay
            if (damageOverlay != null)
            {
                StartCoroutine(FlashDamageOverlay());
            }
        }

        private IEnumerator FlashDamageOverlay()
        {
            if (damageOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.8f, 0f, elapsed / flashDuration);
                damageOverlay.alpha = alpha;
                yield return null;
            }
            damageOverlay.alpha = 0f;
        }

        private void HandleDeath()
        {
            Debug.Log("Player died!");

            // Play death sound
            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            // Fire death event
            CombatEvents.OnPlayerDeath?.Invoke();
        }

        // Public methods
        public void RestoreHealth(int amount)
        {
            if (!IsAlive) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            CombatEvents.OnLivesChanged?.Invoke(CurrentHealth);
            
            Debug.Log($"Health restored by {amount}. Current health: {CurrentHealth}/{maxHealth}");
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            IsInvulnerable = false;
            invulnerabilityTimer = 0f;
            
            CombatEvents.OnLivesChanged?.Invoke(CurrentHealth);
            Debug.Log("Health reset to maximum");
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTakeDamage(float damage = 1f)
        {
            HandleTakeDamage(damage);
        }
    }
}