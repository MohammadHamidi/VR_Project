using System.Collections;
using UnityEngine;
using CombatSystem.Events;
using CombatSystem.Combat;
using CombatSystem.Player;
using CombatSystem.Spawning;
using VRRehab.SceneManagement;

namespace CombatSystem
{
    public class SquatGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private float sessionDuration = 90f;
        [SerializeField] private bool enableTimer = true;
        [SerializeField] private float calibrationTime = 5f;
        [SerializeField] private bool autoRestartOnGameOver = true;
        [SerializeField] private float gameOverDelay = 5f;

        [Header("Component References")]
        [SerializeField] private SquatDodge squatDodge;
        [SerializeField] private PowerMeter powerMeter;
        [SerializeField] private ShockwaveEmitter shockwaveEmitter;
        [SerializeField] private DroneSpawner droneSpawner;
        [SerializeField] private CombatScoring combatScoring;

        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Text timerText;
        [SerializeField] private UnityEngine.UI.Text statusText;
        [SerializeField] private UnityEngine.UI.Text instructionText;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button calibrateButton;
        [SerializeField] private Canvas gameOverCanvas;

        [Header("Audio")]
        [SerializeField] private AudioClip gameStartSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip countdownSound;

        // Game state
        public enum GameState
        {
            Initializing,
            Calibrating,
            Ready,
            Playing,
            GameOver,
            Paused
        }

        public GameState CurrentState { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsSessionActive => CurrentState == GameState.Playing;

        private AudioSource audioSource;
        private Coroutine gameTimerCoroutine;
        private SceneTransitionManager sceneManager;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0f; // 2D UI sound
            
            sceneManager = FindObjectOfType<SceneTransitionManager>();
        }

        void Start()
        {
            InitializeGame();
        }

        void Update()
        {
            UpdateTimer();
            UpdateUI();
        }

        void OnDestroy()
        {
            CombatEvents.ClearAllEvents();
        }

        private void InitializeGame()
        {
            // Find components if not assigned
            if (squatDodge == null) squatDodge = FindObjectOfType<SquatDodge>();
            if (powerMeter == null) powerMeter = FindObjectOfType<PowerMeter>();
            if (shockwaveEmitter == null) shockwaveEmitter = FindObjectOfType<ShockwaveEmitter>();
            if (droneSpawner == null) droneSpawner = FindObjectOfType<DroneSpawner>();
            if (combatScoring == null) combatScoring = FindObjectOfType<CombatScoring>();

            // Setup UI button events
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            if (calibrateButton != null)
                calibrateButton.onClick.AddListener(StartCalibration);

            // Subscribe to game events
            CombatEvents.OnLivesChanged += HandleLivesChanged;

            // Initialize game state
            ChangeState(GameState.Initializing);
            
            // Start calibration after brief delay
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(1f);
            StartCalibration();
        }

        private void ChangeState(GameState newState)
        {
            GameState previousState = CurrentState;
            CurrentState = newState;
            
            OnStateChanged(previousState, newState);
            UpdateStatusText();
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Initializing:
                    if (gameOverCanvas) gameOverCanvas.gameObject.SetActive(false);
                    break;

                case GameState.Calibrating:
                    UpdateInstructionText("Stand upright and remain still for calibration...");
                    break;

                case GameState.Ready:
                    UpdateInstructionText("Get ready! Game will start soon...");
                    break;

                case GameState.Playing:
                    UpdateInstructionText("Squat to dodge drone lasers! Build power meter for shockwaves!");
                    StartGameSession();
                    break;

                case GameState.GameOver:
                    EndGameSession();
                    break;

                case GameState.Paused:
                    UpdateInstructionText("Game Paused");
                    break;
            }
        }

        public void StartCalibration()
        {
            if (CurrentState == GameState.Playing) return;

            ChangeState(GameState.Calibrating);
            StartCoroutine(CalibrationSequence());
        }

        private IEnumerator CalibrationSequence()
        {
            // Calibrate squat detection
            if (squatDodge != null)
            {
                UpdateStatusText("Calibrating height...");
                squatDodge.RecalibrateStanding(calibrationTime);
                yield return new WaitForSeconds(calibrationTime);
            }

            // Ready countdown
            for (int i = 3; i > 0; i--)
            {
                UpdateStatusText($"Starting in {i}...");
                if (audioSource && countdownSound)
                    audioSource.PlayOneShot(countdownSound);
                yield return new WaitForSeconds(1f);
            }

            // Start game
            ChangeState(GameState.Playing);
        }

        private void StartGameSession()
        {
            // Reset all systems
            if (powerMeter != null) powerMeter.ResetPower();
            if (combatScoring != null) combatScoring.RestartGame();
            
            // Initialize timer
            TimeRemaining = sessionDuration;
            
            // Start drone spawning
            if (droneSpawner != null)
            {
                droneSpawner.RestartWaves();
            }

            // Start game timer
            if (enableTimer)
            {
                if (gameTimerCoroutine != null)
                    StopCoroutine(gameTimerCoroutine);
                gameTimerCoroutine = StartCoroutine(GameTimerCoroutine());
            }

            // Play start sound
            if (audioSource && gameStartSound)
                audioSource.PlayOneShot(gameStartSound);

            Debug.Log("Squat game session started!");
        }

        private IEnumerator GameTimerCoroutine()
        {
            while (TimeRemaining > 0 && CurrentState == GameState.Playing)
            {
                TimeRemaining -= Time.deltaTime;
                yield return null;
            }

            if (CurrentState == GameState.Playing)
            {
                // Time's up - end session
                ChangeState(GameState.GameOver);
            }
        }

        private void EndGameSession()
        {
            // Stop spawning
            if (droneSpawner != null)
                droneSpawner.StopAllWaves();

            // Stop timer
            if (gameTimerCoroutine != null)
            {
                StopCoroutine(gameTimerCoroutine);
                gameTimerCoroutine = null;
            }

            // Show results
            ShowGameOverScreen();

            // Play game over sound
            if (audioSource && gameOverSound)
                audioSource.PlayOneShot(gameOverSound);

            // Auto restart if enabled
            if (autoRestartOnGameOver)
            {
                StartCoroutine(AutoRestartCoroutine());
            }

            Debug.Log("Squat game session ended!");
        }

        private IEnumerator AutoRestartCoroutine()
        {
            yield return new WaitForSeconds(gameOverDelay);
            
            if (CurrentState == GameState.GameOver)
            {
                RestartGame();
            }
        }

        private void ShowGameOverScreen()
        {
            if (gameOverCanvas != null)
            {
                gameOverCanvas.gameObject.SetActive(true);
                
                // Show final stats
                if (combatScoring != null)
                {
                    var stats = combatScoring.GetSessionStats();
                    var gameOverText = gameOverCanvas.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (gameOverText != null)
                    {
                        gameOverText.text = $"Session Complete!\n\n" +
                                          $"Final Score: {stats.finalScore:N0}\n" +
                                          $"Dodges: {stats.dodges}\n" +
                                          $"Perfect Squats: {stats.perfectSquats}\n" +
                                          $"Drones Destroyed: {stats.dronesDestroyed}\n" +
                                          $"Highest Combo: {stats.highestCombo}\n" +
                                          $"Lives Remaining: {stats.livesRemaining}";
                    }
                }
            }
        }

        private void HandleLivesChanged(int livesRemaining)
        {
            if (livesRemaining <= 0 && CurrentState == GameState.Playing)
            {
                ChangeState(GameState.GameOver);
            }
        }

        private void UpdateTimer()
        {
            if (CurrentState == GameState.Playing && enableTimer && TimeRemaining > 0)
            {
                // Warning effects when time is low
                if (TimeRemaining <= 10f && timerText != null)
                {
                    // Flash red when time is critically low
                    float flash = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
                    timerText.color = Color.Lerp(Color.white, Color.red, flash);
                }
            }
        }

        private void UpdateUI()
        {
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (timerText != null)
            {
                if (enableTimer && CurrentState == GameState.Playing)
                {
                    int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
                    timerText.text = $"Time: {minutes:00}:{seconds:00}";
                }
                else
                {
                    timerText.text = "";
                }
            }
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                switch (CurrentState)
                {
                    case GameState.Initializing:
                        statusText.text = "Initializing...";
                        break;
                    case GameState.Calibrating:
                        statusText.text = "Calibrating";
                        break;
                    case GameState.Ready:
                        statusText.text = "Ready";
                        break;
                    case GameState.Playing:
                        statusText.text = "Playing";
                        break;
                    case GameState.GameOver:
                        statusText.text = "Game Over";
                        break;
                    case GameState.Paused:
                        statusText.text = "Paused";
                        break;
                }
            }
        }

        private void UpdateStatusText(string customMessage)
        {
            if (statusText != null)
            {
                statusText.text = customMessage;
            }
        }

        private void UpdateInstructionText(string message)
        {
            if (instructionText != null)
                instructionText.text = message;
        }

        // Public API
        public void RestartGame()
        {
            if (gameOverCanvas) gameOverCanvas.gameObject.SetActive(false);
            StartCalibration();
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f; // Reset time scale
            
            if (sceneManager != null)
            {
                sceneManager.LoadScene("MainMenu"); // Adjust scene name as needed
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Load first scene
            }
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugEndGame()
        {
            ChangeState(GameState.GameOver);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddTime(float seconds)
        {
            TimeRemaining += seconds;
        }

        void OnDrawGizmosSelected()
        {
            // Draw game area bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 10f);
        }
    }
}
