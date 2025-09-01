using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressionIntegratedStageManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private ObjectStageManager stageManager;
    [SerializeField] private ProgressionSystem progressionSystem;
    [SerializeField] private LevelGenerator levelGenerator;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    [Header("Progression Settings")]
    [SerializeField] private string exerciseName = "Throwing";
    [SerializeField] private float successScoreThreshold = 0.6f;

    private bool levelCompleted = false;
    private float levelScore = 0f;
    private int ringsHit = 0;
    private int totalRings = 0;

    void Start()
    {
        if (stageManager == null)
            stageManager = GetComponent<ObjectStageManager>();

        if (progressionSystem == null)
            progressionSystem = FindObjectOfType<ProgressionSystem>();

        if (levelGenerator == null)
            levelGenerator = FindObjectOfType<LevelGenerator>();

        InitializeUI();
        SetupEventListeners();
        UpdateUI();
    }

    private void InitializeUI()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(AdvanceToNextLevel);
            nextLevelButton.gameObject.SetActive(false);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryLevel);
            retryButton.gameObject.SetActive(false);
        }
    }

    private void SetupEventListeners()
    {
        if (stageManager != null)
        {
            // Hook into stage manager events (you'll need to add these to ObjectStageManager)
            // stageManager.OnStageCompleted += OnStageCompleted;
            // stageManager.OnTargetHit += OnTargetHit;
            // stageManager.OnTimeUp += OnTimeUp;
        }

        if (progressionSystem != null)
        {
            ProgressionSystem.OnLevelAdvanced += OnLevelAdvanced;
            ProgressionSystem.OnExerciseMastered += OnExerciseMastered;
        }
    }

    private void OnTargetHit(TargetRing ring)
    {
        ringsHit++;
        UpdateUI();
    }

    private void OnStageCompleted()
    {
        levelCompleted = true;
        CalculateScore();
        RecordProgress();
        ShowCompletionUI();
    }

    private void OnTimeUp()
    {
        levelCompleted = false;
        CalculateScore();
        RecordProgress();
        ShowCompletionUI();
    }

    private void CalculateScore()
    {
        if (totalRings == 0) return;

        float accuracy = (float)ringsHit / totalRings;
        float timeBonus = levelCompleted ? 1.0f : 0.5f; // Bonus for completing within time
        levelScore = (accuracy + timeBonus) / 2f;

        levelScore = Mathf.Clamp01(levelScore);
    }

    private void RecordProgress()
    {
        bool success = levelScore >= successScoreThreshold;

        if (progressionSystem != null)
        {
            progressionSystem.RecordExerciseResult(exerciseName, success, levelScore);
        }

        Debug.Log($"Level completed: {levelCompleted}, Rings hit: {ringsHit}/{totalRings}, Score: {levelScore:F2}, Success: {success}");
    }

    private void ShowCompletionUI()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(levelCompleted);
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(!levelCompleted);
        }

        UpdateUI();
    }

    private void AdvanceToNextLevel()
    {
        if (levelGenerator != null)
        {
            levelGenerator.AdvanceToNextLevel();
            ResetLevelState();
            UpdateUI();
        }
    }

    private void RetryLevel()
    {
        if (levelGenerator != null)
        {
            levelGenerator.RegenerateLevel();
            ResetLevelState();
            UpdateUI();
        }
    }

    private void ResetLevelState()
    {
        levelCompleted = false;
        levelScore = 0f;
        ringsHit = 0;
        totalRings = 0;

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        // Update level text
        if (levelText != null && levelGenerator != null)
        {
            int currentLevel = levelGenerator.GetCurrentLevelNumber();
            int totalLevels = levelGenerator.GetTotalLevels();
            levelText.text = $"Level {currentLevel}/{totalLevels}";
        }

        // Update progress text
        if (progressText != null)
        {
            if (totalRings > 0)
            {
                progressText.text = $"Rings: {ringsHit}/{totalRings}";
            }
            else
            {
                progressText.text = "Get ready...";
            }
        }

        // Update progress bar
        if (progressBar != null && totalRings > 0)
        {
            progressBar.value = (float)ringsHit / totalRings;
        }
    }

    private void OnLevelAdvanced(string exercise, int newLevel)
    {
        if (exercise == exerciseName)
        {
            Debug.Log($"Congratulations! Advanced to level {newLevel} in {exercise}!");
            // Could show celebration UI here
        }
    }

    private void OnExerciseMastered(string exercise)
    {
        if (exercise == exerciseName)
        {
            Debug.Log($"Mastered {exercise}! Excellent progress!");
            // Could show mastery achievement UI here
        }
    }

    // Public methods for external integration
    public void SetTotalRings(int count)
    {
        totalRings = count;
        UpdateUI();
    }

    public void NotifyTargetHit()
    {
        ringsHit++;
        UpdateUI();
    }

    public void NotifyStageCompleted()
    {
        OnStageCompleted();
    }

    public void NotifyTimeUp()
    {
        OnTimeUp();
    }

    void OnDestroy()
    {
        if (progressionSystem != null)
        {
            ProgressionSystem.OnLevelAdvanced -= OnLevelAdvanced;
            ProgressionSystem.OnExerciseMastered -= OnExerciseMastered;
        }
    }
}
