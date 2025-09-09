using UnityEngine;
using CombatSystem.Events;
using CombatSystem.Drones;
using DG.Tweening;
using TMPro;

namespace CombatSystem.Combat
{
    public class CombatScoring : MonoBehaviour
    {
        [Header("Scoring Settings")]
        [SerializeField] private int dodgePoints = 50;
        [SerializeField] private int perfectSquatBonus = 25;
        [SerializeField] private int droneDestroyPoints = 100;
        [SerializeField] private float comboDecayTime = 5f;
        [SerializeField] private float comboMultiplier = 0.1f; // 10% per combo level

        [Header("Lives System")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private bool regenerateLives = false;
        [SerializeField] private float lifeRegenTime = 30f;

        [Header("Combo System")]
        [SerializeField] private int maxComboLevel = 10;
        [SerializeField] private Color comboTextColor = Color.yellow;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private UnityEngine.UI.Image[] lifeIcons;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip scoreSound;
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip perfectSound;
        [SerializeField] private AudioClip lifeGainSound;
        [SerializeField] private AudioClip lifeLostSound;

        // Properties
        public int CurrentScore { get; private set; }
        public int CurrentLives { get; private set; }
        public int ComboLevel { get; private set; }
        public float ComboTimeRemaining { get; private set; }
        public bool IsGameOver => CurrentLives <= 0;

        // Private fields
        private AudioSource audioSource;
        private float lastComboTime;
        private float lastLifeRegenTime;

        // Score tracking
        private int sessionDodges = 0;
        private int sessionPerfectSquats = 0;
        private int sessionDronesDestroyed = 0;
        private int highestCombo = 0;

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
            InitializeScoring();
            SubscribeToEvents();
            UpdateAllUI();
        }

        void Update()
        {
            UpdateComboDecay();
            UpdateLifeRegeneration();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeScoring()
        {
            CurrentScore = 0;
            CurrentLives = maxLives;
            ComboLevel = 0;
            ComboTimeRemaining = 0f;
            lastComboTime = Time.time;
            lastLifeRegenTime = Time.time;

            // Reset session tracking
            sessionDodges = 0;
            sessionPerfectSquats = 0;
            sessionDronesDestroyed = 0;
            highestCombo = 0;
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnPlayerDodge += HandlePlayerDodge;
            CombatEvents.OnPlayerHit += HandlePlayerHit;
            CombatEvents.OnPlayerTakeDamage += HandlePlayerTakeDamage; // Added missing subscription
            CombatEvents.OnValidSquat += HandleValidSquat;
            CombatEvents.OnDroneDestroyed += HandleDroneDestroyed;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnPlayerDodge -= HandlePlayerDodge;
            CombatEvents.OnPlayerHit -= HandlePlayerHit;
            CombatEvents.OnPlayerTakeDamage -= HandlePlayerTakeDamage; // Added missing unsubscription
            CombatEvents.OnValidSquat -= HandleValidSquat;
            CombatEvents.OnDroneDestroyed -= HandleDroneDestroyed;
        }

        private void UpdateComboDecay()
        {
            if (ComboLevel > 0)
            {
                ComboTimeRemaining = comboDecayTime - (Time.time - lastComboTime);
                
                if (ComboTimeRemaining <= 0f)
                {
                    ResetCombo();
                }
                
                UpdateComboUI();
            }
        }

        private void UpdateLifeRegeneration()
        {
            if (regenerateLives && CurrentLives < maxLives)
            {
                if (Time.time - lastLifeRegenTime >= lifeRegenTime)
                {
                    GainLife();
                    lastLifeRegenTime = Time.time;
                }
            }
        }

        private void HandlePlayerDodge()
        {
            sessionDodges++;
            AddScore(dodgePoints, "Dodge!");
            ExtendCombo();
        }

        private void HandlePlayerHit(Vector3 hitPosition)
        {
            LoseLife();
            ResetCombo();
        }

        private void HandlePlayerTakeDamage(float damageAmount)
        {
            LoseLife();
            ResetCombo();
            Debug.Log($"CombatScoring: Player took {damageAmount} damage, lives remaining: {CurrentLives}");
        }

        private void HandleValidSquat(float depthNorm, float quality)
        {
            bool isPerfect = quality >= 85f;
            
            if (isPerfect)
            {
                sessionPerfectSquats++;
                AddScore(perfectSquatBonus, "Perfect Squat!");
                ExtendCombo();
                
                // Play perfect feedback
                if (audioSource && perfectSound)
                    audioSource.PlayOneShot(perfectSound);
            }
            else
            {
                // Regular squat - small score but no combo extension
                AddScore(5, "Good Squat");
            }
        }

        private void HandleDroneDestroyed(DroneController drone)
        {
            sessionDronesDestroyed++;
            
            // Score based on drone type
            int points = drone.type == DroneType.Heavy ? droneDestroyPoints * 2 : droneDestroyPoints;
            string message = $"{drone.type} Destroyed!";
            
            AddScore(points, message);
            ExtendCombo();
        }

        private void AddScore(int basePoints, string reason)
        {
            // Apply combo multiplier
            float multiplier = 1f + (ComboLevel * comboMultiplier);
            int finalPoints = Mathf.RoundToInt(basePoints * multiplier);
            
            CurrentScore += finalPoints;
            CombatEvents.OnScoreChanged?.Invoke(CurrentScore);
            
            // Audio feedback
            if (audioSource && scoreSound)
                audioSource.PlayOneShot(scoreSound);
            
            // Visual feedback
            ShowScorePopup(finalPoints, reason);
            UpdateScoreUI();
            
            Debug.Log($"Score: +{finalPoints} ({reason}) - Combo x{multiplier:F1}");
        }

        private void ExtendCombo()
        {
            ComboLevel = Mathf.Min(ComboLevel + 1, maxComboLevel);
            lastComboTime = Time.time;
            ComboTimeRemaining = comboDecayTime;
            
            // Track highest combo
            if (ComboLevel > highestCombo)
                highestCombo = ComboLevel;
            
            CombatEvents.OnComboChanged?.Invoke(ComboLevel);
            
            // Play combo sound
            if (ComboLevel > 1 && audioSource && comboSound)
            {
                audioSource.pitch = 1f + (ComboLevel * 0.1f);
                audioSource.PlayOneShot(comboSound);
            }
            
            UpdateComboUI();
            Debug.Log($"Combo level: {ComboLevel}");
        }

        private void ResetCombo()
        {
            if (ComboLevel > 0)
            {
                ComboLevel = 0;
                ComboTimeRemaining = 0f;
                CombatEvents.OnComboChanged?.Invoke(ComboLevel);
                UpdateComboUI();
                Debug.Log("Combo reset");
            }
        }

        private void LoseLife()
        {
            if (CurrentLives > 0)
            {
                CurrentLives--;
                CombatEvents.OnLivesChanged?.Invoke(CurrentLives);
                
                // Audio feedback
                if (audioSource && lifeLostSound)
                    audioSource.PlayOneShot(lifeLostSound);
                
                // Visual feedback
                if (lifeIcons != null && CurrentLives < lifeIcons.Length)
                {
                    var icon = lifeIcons[CurrentLives];
                    if (icon != null)
                    {
                        icon.DOFade(0.3f, 0.3f);
                        icon.transform.DOShakeScale(0.5f, Vector3.one * 0.2f, 10, 90f);
                    }
                }
                
                UpdateLivesUI();
                
                // Check game over
                if (IsGameOver)
                {
                    HandleGameOver();
                }
                
                Debug.Log($"CombatScoring: Life lost! Lives remaining: {CurrentLives}");
            }
        }

        private void GainLife()
        {
            if (CurrentLives < maxLives)
            {
                CurrentLives++;
                CombatEvents.OnLivesChanged?.Invoke(CurrentLives);
                
                // Audio feedback
                if (audioSource && lifeGainSound)
                    audioSource.PlayOneShot(lifeGainSound);
                
                // Visual feedback
                if (lifeIcons != null && CurrentLives <= lifeIcons.Length)
                {
                    var icon = lifeIcons[CurrentLives - 1];
                    if (icon != null)
                    {
                        icon.DOFade(1f, 0.3f);
                        icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f);
                    }
                }
                
                UpdateLivesUI();
                Debug.Log($"Life gained! Lives: {CurrentLives}");
            }
        }

        private void HandleGameOver()
        {
            Debug.Log("Game Over!");
            
            // Show final stats
            LogSessionStats();
            
            // Could trigger game over UI or scene transition here
            // For now, just restart after delay
            StartCoroutine(RestartAfterDelay(3f));
        }

        private System.Collections.IEnumerator RestartAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            RestartGame();
        }

        private void ShowScorePopup(int points, string reason)
        {
            // This could be enhanced with a proper popup system
            if (scoreText != null)
            {
                // Animate score text
                scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1f);
            }
        }

        private void UpdateAllUI()
        {
            UpdateScoreUI();
            UpdateLivesUI();
            UpdateComboUI();
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {CurrentScore:N0}";
        }

        private void UpdateLivesUI()
        {
            if (livesText != null)
                livesText.text = $"Lives: {CurrentLives}";
            
            // Update life icons
            if (lifeIcons != null)
            {
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    if (lifeIcons[i] != null)
                    {
                        lifeIcons[i].color = i < CurrentLives ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                    }
                }
            }
        }

        private void UpdateComboUI()
        {
            if (comboText != null)
            {
                if (ComboLevel > 1)
                {
                    comboText.text = $"Combo x{ComboLevel}\n{ComboTimeRemaining:F1}s";
                    comboText.color = Color.Lerp(comboTextColor, Color.red, 1f - (ComboTimeRemaining / comboDecayTime));
                }
                else
                {
                    comboText.text = "";
                }
            }
        }

        private void LogSessionStats()
        {
            Debug.Log($"=== Session Stats ===");
            Debug.Log($"Final Score: {CurrentScore:N0}");
            Debug.Log($"Dodges: {sessionDodges}");
            Debug.Log($"Perfect Squats: {sessionPerfectSquats}");
            Debug.Log($"Drones Destroyed: {sessionDronesDestroyed}");
            Debug.Log($"Highest Combo: {highestCombo}");
        }

        // Public API
        public void RestartGame()
        {
            InitializeScoring();
            UpdateAllUI();
            Debug.Log("Game restarted");
        }

        public void AddBonusScore(int points, string reason = "Bonus")
        {
            AddScore(points, reason);
        }

        public void SetLives(int lives)
        {
            CurrentLives = Mathf.Clamp(lives, 0, maxLives);
            CombatEvents.OnLivesChanged?.Invoke(CurrentLives);
            UpdateLivesUI();
        }

        public SessionStats GetSessionStats()
        {
            return new SessionStats
            {
                finalScore = CurrentScore,
                dodges = sessionDodges,
                perfectSquats = sessionPerfectSquats,
                dronesDestroyed = sessionDronesDestroyed,
                highestCombo = highestCombo,
                livesRemaining = CurrentLives
            };
        }

        /// <summary>
        /// Gets wave success based on current session performance
        /// </summary>
        public bool GetWaveSuccess()
        {
            // Consider wave successful if player still has lives and has achieved some score
            return !IsGameOver && CurrentScore > 0;
        }

        /// <summary>
        /// Gets current wave score based on session performance
        /// </summary>
        public float GetWaveScore()
        {
            if (IsGameOver) return 0f;

            // Calculate score based on various factors
            float baseScore = CurrentScore;
            float lifeBonus = CurrentLives * 50f; // Bonus for remaining lives
            float comboBonus = ComboLevel * 25f; // Bonus for current combo
            float performanceMultiplier = 1f + (sessionPerfectSquats * 0.1f); // Bonus for perfect squats

            return (baseScore + lifeBonus + comboBonus) * performanceMultiplier;
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddScore(int points)
        {
            AddScore(points, "Debug");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLoseLife()
        {
            LoseLife();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugExtendCombo()
        {
            ExtendCombo();
        }
    }

    [System.Serializable]
    public struct SessionStats
    {
        public int finalScore;
        public int dodges;
        public int perfectSquats;
        public int dronesDestroyed;
        public int highestCombo;
        public int livesRemaining;
    }
}
