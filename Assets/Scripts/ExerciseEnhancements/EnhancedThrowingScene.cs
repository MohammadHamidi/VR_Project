using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRehab.Analytics;
using VRRehab.DataPersistence;
using VRRehab.UI;
using VRRehab.Tutorial;

namespace VRRehab.Exercises
{
    public class EnhancedThrowingScene : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private PerformanceAnalytics analytics;
        [SerializeField] private DataPersistenceManager dataManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TutorialManager tutorialManager;

        [Header("Exercise Objects")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform ballSpawnPoint;
        [SerializeField] private List<Transform> targetRingPositions;
        [SerializeField] private GameObject[] targetRings;

        [Header("UI Elements")]
        [SerializeField] private Canvas exerciseCanvas;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button restartButton;

        [Header("Exercise Settings")]
        [SerializeField] private float exerciseDuration = 60f; // seconds
        [SerializeField] private int targetScore = 10; // rings to hit
        [SerializeField] private float ballRespawnDelay = 1f;
        [SerializeField] private int currentLevel = 1;

        // Exercise state
        private bool isExerciseActive = false;
        private float exerciseTimer = 0f;
        private int currentScore = 0;
        private int ringsHit = 0;
        private GameObject currentBall;
        private List<GameObject> activeRings = new List<GameObject>();
        private Coroutine exerciseCoroutine;
        private Coroutine ballRespawnCoroutine;

        // Analytics data
        private Dictionary<string, float> detailedMetrics = new Dictionary<string, float>();
        private DateTime exerciseStartTime;
        private int ballsThrown = 0;
        private float averageThrowForce = 0f;

        // Events
        public static event Action<int> OnScoreChanged;
        public static event Action OnExerciseCompleted;
        public static event Action OnExerciseFailed;

        void Start()
        {
            InitializeExercise();
        }

        private void InitializeExercise()
        {
            // Find required systems if not assigned
            if (analytics == null) analytics = FindObjectOfType<PerformanceAnalytics>();
            if (dataManager == null) dataManager = FindObjectOfType<DataPersistenceManager>();
            if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
            if (tutorialManager == null) tutorialManager = FindObjectOfType<TutorialManager>();

            // Setup UI
            SetupUI();

            // Setup exercise environment
            SetupEnvironment();

            // Show welcome message
            if (uiManager != null)
            {
                uiManager.ShowSuccess("Throwing Exercise Ready! Hit the rings to score points.");
            }

            // Start tutorial if available
            if (tutorialManager != null)
            {
                tutorialManager.StartTutorialForExercise(VRRehab.SceneManagement.SceneTransitionManager.ExerciseType.Throwing);
            }
        }

        private void SetupUI()
        {
            if (exerciseCanvas != null)
                DontDestroyOnLoad(exerciseCanvas.gameObject);

            // Setup button listeners
            if (pauseButton != null)
                pauseButton.onClick.AddListener(TogglePause);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartExercise);

            // Initialize UI values
            UpdateScore(0);
            UpdateLevel(currentLevel);
            UpdateTimer(exerciseDuration);
            UpdateProgress(0f);
        }

        private void SetupEnvironment()
        {
            // Spawn target rings
            SpawnTargetRings();

            // Setup ball spawning
            if (ballSpawnPoint == null)
            {
                ballSpawnPoint = new GameObject("BallSpawnPoint").transform;
                ballSpawnPoint.position = new Vector3(0, 1.5f, 0);
            }

            // Spawn initial ball
            SpawnBall();
        }

        private void SpawnTargetRings()
        {
            activeRings.Clear();

            for (int i = 0; i < targetRingPositions.Count && i < targetRings.Length; i++)
            {
                if (targetRings[i] != null && targetRingPositions[i] != null)
                {
                    GameObject ring = Instantiate(targetRings[i], targetRingPositions[i].position, targetRingPositions[i].rotation);
                    ring.name = $"TargetRing_{i + 1}";
                    activeRings.Add(ring);

                    // Add ring hit detection
                    RingHitDetector detector = ring.AddComponent<RingHitDetector>();
                    detector.OnRingHit += OnRingHit;
                }
            }
        }

        private void SpawnBall()
        {
            if (ballPrefab != null && ballSpawnPoint != null)
            {
                currentBall = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity);
                currentBall.name = "ThrowableBall";

                // Add ball tracking
                BallTracker tracker = currentBall.AddComponent<BallTracker>();
                tracker.OnBallThrown += OnBallThrown;
            }
        }

        private void StartExercise()
        {
            if (isExerciseActive) return;

            isExerciseActive = true;
            exerciseTimer = exerciseDuration;
            exerciseStartTime = DateTime.Now;

            // Start exercise coroutine
            exerciseCoroutine = StartCoroutine(ExerciseTimer());

            // Initialize analytics
            InitializeAnalytics();

            Debug.Log("Throwing exercise started");
        }

        private void StopExercise()
        {
            if (!isExerciseActive) return;

            isExerciseActive = false;

            if (exerciseCoroutine != null)
            {
                StopCoroutine(exerciseCoroutine);
                exerciseCoroutine = null;
            }

            // Record final analytics
            RecordExerciseResults();

            Debug.Log("Throwing exercise stopped");
        }

        private void TogglePause()
        {
            if (isExerciseActive)
            {
                StopExercise();
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Exercise Paused", UIManager.NotificationData.NotificationType.Info);
                }
            }
            else
            {
                StartExercise();
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Exercise Resumed", UIManager.NotificationData.NotificationType.Info);
                }
            }
        }

        private void RestartExercise()
        {
            StopExercise();

            // Reset all values
            currentScore = 0;
            ringsHit = 0;
            ballsThrown = 0;
            averageThrowForce = 0f;

            // Reset UI
            UpdateScore(0);
            UpdateTimer(exerciseDuration);
            UpdateProgress(0f);

            // Reset environment
            ResetEnvironment();

            if (uiManager != null)
            {
                uiManager.ShowSuccess("Exercise restarted!");
            }

            StartExercise();
        }

        private void ResetEnvironment()
        {
            // Destroy current ball
            if (currentBall != null)
            {
                Destroy(currentBall);
            }

            // Reset rings (if needed)
            foreach (GameObject ring in activeRings)
            {
                if (ring != null)
                {
                    // Reset ring state
                    RingHitDetector detector = ring.GetComponent<RingHitDetector>();
                    if (detector != null)
                    {
                        detector.ResetRing();
                    }
                }
            }

            // Spawn new ball
            SpawnBall();
        }

        #region Event Handlers

        private void OnRingHit(GameObject ring, float hitForce)
        {
            if (!isExerciseActive) return;

            ringsHit++;
            currentScore += 10; // Base points for hitting ring
            int bonusPoints = Mathf.RoundToInt(hitForce * 5); // Bonus based on throw force
            currentScore += bonusPoints;

            UpdateScore(currentScore);
            UpdateProgress((float)ringsHit / targetScore);

            // Visual feedback
            if (uiManager != null)
            {
                uiManager.PlaySuccessFeedback();
            }

            // Check for completion
            if (ringsHit >= targetScore)
            {
                CompleteExercise(true);
            }

            // Analytics
            detailedMetrics["TotalRingsHit"] = ringsHit;
            detailedMetrics["AverageHitForce"] = hitForce;

            OnScoreChanged?.Invoke(currentScore);
        }

        private void OnBallThrown(float throwForce)
        {
            if (!isExerciseActive) return;

            ballsThrown++;
            averageThrowForce = ((averageThrowForce * (ballsThrown - 1)) + throwForce) / ballsThrown;

            // Schedule ball respawn
            if (ballRespawnCoroutine != null)
            {
                StopCoroutine(ballRespawnCoroutine);
            }
            ballRespawnCoroutine = StartCoroutine(RespawnBallDelayed());

            // Analytics
            detailedMetrics["BallsThrown"] = ballsThrown;
            detailedMetrics["AverageThrowForce"] = averageThrowForce;
        }

        #endregion

        #region UI Updates

        private void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        private void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {level}";
            }
        }

        private void UpdateTimer(float timeRemaining)
        {
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }

        #endregion

        #region Exercise Logic

        private IEnumerator ExerciseTimer()
        {
            while (exerciseTimer > 0 && isExerciseActive)
            {
                exerciseTimer -= Time.deltaTime;
                UpdateTimer(exerciseTimer);

                // Update analytics every second
                if (analytics != null && Mathf.FloorToInt(exerciseTimer) % 1 == 0)
                {
                    UpdateLiveAnalytics();
                }

                yield return null;
            }

            if (isExerciseActive)
            {
                // Time's up
                CompleteExercise(ringsHit >= targetScore / 2); // Pass if hit at least half the targets
            }
        }

        private IEnumerator RespawnBallDelayed()
        {
            yield return new WaitForSeconds(ballRespawnDelay);
            SpawnBall();
        }

        private void CompleteExercise(bool success)
        {
            StopExercise();

            TimeSpan exerciseDuration = DateTime.Now - exerciseStartTime;
            float finalScore = currentScore;

            // Record final results
            RecordExerciseResults();

            if (success)
            {
                if (uiManager != null)
                {
                    uiManager.ShowAchievement("Throwing Master", $"Completed level {currentLevel} with {currentScore} points!");
                }
                OnExerciseCompleted?.Invoke();
            }
            else
            {
                if (uiManager != null)
                {
                    uiManager.ShowWarning("Exercise completed. Try again to improve your score!");
                }
                OnExerciseFailed?.Invoke();
            }

            // Show results
            ShowResults(success, finalScore, exerciseDuration);
        }

        private void ShowResults(bool success, float score, TimeSpan duration)
        {
            string resultMessage = success ?
                $"🎉 Exercise Complete!\nScore: {score}\nTime: {duration.TotalSeconds:F1}s\nRings Hit: {ringsHit}" :
                $"Exercise Finished\nScore: {score}\nTime: {duration.TotalSeconds:F1}s\nRings Hit: {ringsHit}";

            if (uiManager != null)
            {
                uiManager.ShowNotification(resultMessage,
                    success ? UIManager.NotificationData.NotificationType.Success : UIManager.NotificationData.NotificationType.Info,
                    5f);
            }
        }

        #endregion

        #region Analytics Integration

        private void InitializeAnalytics()
        {
            detailedMetrics.Clear();
            detailedMetrics["Level"] = currentLevel;
            detailedMetrics["TargetScore"] = targetScore;
            detailedMetrics["ExerciseDuration"] = exerciseDuration;
        }

        private void UpdateLiveAnalytics()
        {
            if (analytics != null)
            {
                analytics.RecordDataPoint("Score", currentScore, "Throwing", currentLevel);
                analytics.RecordDataPoint("Accuracy", (float)ringsHit / Mathf.Max(ballsThrown, 1), "Throwing", currentLevel);
                analytics.RecordDataPoint("TimeRemaining", exerciseTimer, "Throwing", currentLevel);
            }
        }

        private void RecordExerciseResults()
        {
            if (analytics == null) return;

            // Calculate final metrics
            detailedMetrics["FinalScore"] = currentScore;
            detailedMetrics["RingsHit"] = ringsHit;
            detailedMetrics["Accuracy"] = ballsThrown > 0 ? (float)ringsHit / ballsThrown : 0f;
            detailedMetrics["CompletionRate"] = (float)ringsHit / targetScore;
            detailedMetrics["TimeSpent"] = exerciseDuration - exerciseTimer;
            detailedMetrics["BallsThrown"] = ballsThrown;

            // Record exercise completion
            TimeSpan duration = DateTime.Now - exerciseStartTime;
            analytics.RecordExerciseResult("Throwing", currentLevel, currentScore, duration, detailedMetrics);

            Debug.Log($"Exercise results recorded: Score={currentScore}, Rings={ringsHit}/{targetScore}, Accuracy={detailedMetrics["Accuracy"]:F2}");
        }

        #endregion

        #region Utility Methods

        public bool IsExerciseActive()
        {
            return isExerciseActive;
        }

        public int GetCurrentScore()
        {
            return currentScore;
        }

        public float GetTimeRemaining()
        {
            return exerciseTimer;
        }

        public void SetDifficulty(int level)
        {
            currentLevel = level;
            UpdateLevel(level);

            // Adjust parameters based on level
            targetScore = 5 + (level * 2); // Increases with level
            exerciseDuration = 60f - (level * 5); // Decreases with level
            exerciseDuration = Mathf.Max(exerciseDuration, 30f); // Minimum 30 seconds

            UpdateTimer(exerciseDuration);
        }

        public void SetTargetScore(int score)
        {
            targetScore = score;
            detailedMetrics["TargetScore"] = targetScore;
        }

        public void SetExerciseDuration(float duration)
        {
            exerciseDuration = duration;
            detailedMetrics["ExerciseDuration"] = exerciseDuration;
        }

        #endregion

        void OnDestroy()
        {
            // Clean up coroutines
            if (exerciseCoroutine != null)
            {
                StopCoroutine(exerciseCoroutine);
            }

            if (ballRespawnCoroutine != null)
            {
                StopCoroutine(ballRespawnCoroutine);
            }
        }
    }

    // Helper component for ring hit detection
    public class RingHitDetector : MonoBehaviour
    {
        public event Action<GameObject, float> OnRingHit;

        private bool hasBeenHit = false;
        private Renderer ringRenderer;
        private Color originalColor;

        void Start()
        {
            ringRenderer = GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                originalColor = ringRenderer.material.color;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasBeenHit || !other.CompareTag("ThrowableBall")) return;

            hasBeenHit = true;

            // Calculate hit force (simplified)
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            float hitForce = ballRb != null ? ballRb.velocity.magnitude : 1f;

            // Visual feedback
            if (ringRenderer != null)
            {
                StartCoroutine(HitEffect());
            }

            // Trigger event
            OnRingHit?.Invoke(gameObject, hitForce);

            Debug.Log($"Ring {gameObject.name} hit with force: {hitForce}");
        }

        private IEnumerator HitEffect()
        {
            if (ringRenderer != null)
            {
                ringRenderer.material.color = Color.green;
                yield return new WaitForSeconds(0.5f);
                ringRenderer.material.color = originalColor;
            }
        }

        public void ResetRing()
        {
            hasBeenHit = false;
            if (ringRenderer != null)
            {
                ringRenderer.material.color = originalColor;
            }
        }
    }

    // Helper component for ball tracking
    public class BallTracker : MonoBehaviour
    {
        public event Action<float> OnBallThrown;

        private bool hasBeenThrown = false;
        private Vector3 initialPosition;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;
        }

        void FixedUpdate()
        {
            if (!hasBeenThrown && rb != null)
            {
                // Check if ball has been thrown (significant movement)
                if (Vector3.Distance(transform.position, initialPosition) > 0.5f && rb.velocity.magnitude > 1f)
                {
                    hasBeenThrown = true;
                    OnBallThrown?.Invoke(rb.velocity.magnitude);
                }
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // Ball hit something, could respawn after delay
            if (hasBeenThrown && collision.gameObject.CompareTag("Ground"))
            {
                // Could trigger respawn here
            }
        }
    }
}
