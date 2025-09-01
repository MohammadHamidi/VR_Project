using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressionSystem : MonoBehaviour
{
    [System.Serializable]
    public class ExerciseProgress
    {
        public string exerciseName;
        public int currentLevel = 1;
        public int maxLevelReached = 1;
        public float successRate = 0f;
        public int attempts = 0;
        public int successfulAttempts = 0;
        public float averageScore = 0f;
        public DateTime lastPlayed;
        public bool isUnlocked = true;
    }

    [System.Serializable]
    public class PatientProgress
    {
        public string patientId;
        public string patientName;
        public List<ExerciseProgress> exercises = new List<ExerciseProgress>();
        public DateTime lastSession;
        public int totalSessions = 0;
        public float overallProgress = 0f;
    }

    [Header("Progression Settings")]
    [Tooltip("Minimum success rate required to advance to next level")]
    [Range(0.5f, 1.0f)]
    public float minSuccessRateForAdvancement = 0.7f;

    [Tooltip("Minimum attempts required before allowing level advancement")]
    [Range(3, 10)]
    public int minAttemptsForAdvancement = 3;

    [Tooltip("Success rate threshold for considering an exercise 'mastered'")]
    [Range(0.8f, 1.0f)]
    public float masteryThreshold = 0.85f;

    [Header("Adaptive Difficulty")]
    [Tooltip("Enable automatic difficulty adjustment based on performance")]
    public bool enableAdaptiveDifficulty = true;

    [Tooltip("How quickly difficulty adjusts (0 = slow, 1 = fast)")]
    [Range(0.1f, 1.0f)]
    public float adaptationSpeed = 0.3f;

    // Current patient progress
    private PatientProgress currentPatient;

    // Events
    public static event Action<string, int> OnLevelAdvanced;
    public static event Action<string> OnExerciseMastered;
    public static event Action<string, int> OnNewExerciseUnlocked;

    void Awake()
    {
        // Initialize with default patient if none exists
        if (currentPatient == null)
        {
            CreateNewPatient("DefaultPatient");
        }
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public void CreateNewPatient(string patientName)
    {
        currentPatient = new PatientProgress
        {
            patientId = Guid.NewGuid().ToString(),
            patientName = patientName,
            exercises = new List<ExerciseProgress>(),
            lastSession = DateTime.Now,
            totalSessions = 0
        };

        // Initialize with basic exercises
        InitializeBasicExercises();
    }

    /// <summary>
    /// Initializes basic exercises for new patients
    /// </summary>
    private void InitializeBasicExercises()
    {
        string[] basicExercises = { "Throwing", "BridgeBuilding", "SquatDodge" };

        foreach (string exercise in basicExercises)
        {
            ExerciseProgress progress = new ExerciseProgress
            {
                exerciseName = exercise,
                currentLevel = 1,
                maxLevelReached = 1,
                successRate = 0f,
                attempts = 0,
                successfulAttempts = 0,
                averageScore = 0f,
                lastPlayed = DateTime.Now,
                isUnlocked = true
            };

            currentPatient.exercises.Add(progress);
        }
    }

    /// <summary>
    /// Records the result of an exercise attempt
    /// </summary>
    public void RecordExerciseResult(string exerciseName, bool success, float score)
    {
        ExerciseProgress progress = GetExerciseProgress(exerciseName);
        if (progress == null) return;

        progress.attempts++;
        progress.lastPlayed = DateTime.Now;

        if (success)
        {
            progress.successfulAttempts++;
        }

        // Update success rate
        progress.successRate = (float)progress.successfulAttempts / progress.attempts;

        // Update average score (weighted average)
        progress.averageScore = (progress.averageScore * (progress.attempts - 1) + score) / progress.attempts;

        // Check for level advancement
        CheckLevelAdvancement(progress);

        // Update overall progress
        UpdateOverallProgress();

        Debug.Log($"Exercise {exerciseName}: Success rate = {progress.successRate:F2}, Average score = {progress.averageScore:F2}");
    }

    /// <summary>
    /// Checks if the patient should advance to the next level
    /// </summary>
    private void CheckLevelAdvancement(ExerciseProgress progress)
    {
        if (progress.attempts < minAttemptsForAdvancement) return;

        bool shouldAdvance = progress.successRate >= minSuccessRateForAdvancement &&
                           progress.averageScore >= 0.6f; // Minimum score threshold

        if (shouldAdvance && progress.currentLevel < GetMaxLevelForExercise(progress.exerciseName))
        {
            int oldLevel = progress.currentLevel;
            progress.currentLevel++;
            progress.maxLevelReached = Mathf.Max(progress.maxLevelReached, progress.currentLevel);

            OnLevelAdvanced?.Invoke(progress.exerciseName, progress.currentLevel);
            Debug.Log($"Advanced {progress.exerciseName} from level {oldLevel} to {progress.currentLevel}");
        }

        // Check for mastery
        if (progress.successRate >= masteryThreshold && progress.averageScore >= 0.8f)
        {
            OnExerciseMastered?.Invoke(progress.exerciseName);
            Debug.Log($"Mastered exercise: {progress.exerciseName}");
        }
    }

    /// <summary>
    /// Gets the maximum level available for an exercise
    /// </summary>
    private int GetMaxLevelForExercise(string exerciseName)
    {
        switch (exerciseName)
        {
            case "Throwing":
                return 5; // We created 5 throwing levels
            case "BridgeBuilding":
                return 3; // Could expand this later
            case "SquatDodge":
                return 3; // Could expand this later
            default:
                return 1;
        }
    }

    /// <summary>
    /// Gets the progress for a specific exercise
    /// </summary>
    public ExerciseProgress GetExerciseProgress(string exerciseName)
    {
        return currentPatient.exercises.Find(e => e.exerciseName == exerciseName);
    }

    /// <summary>
    /// Gets all exercise progress
    /// </summary>
    public List<ExerciseProgress> GetAllExerciseProgress()
    {
        return currentPatient.exercises;
    }

    /// <summary>
    /// Updates the overall patient progress
    /// </summary>
    private void UpdateOverallProgress()
    {
        if (currentPatient.exercises.Count == 0) return;

        float totalProgress = 0f;
        foreach (var exercise in currentPatient.exercises)
        {
            // Calculate progress as a combination of level and success rate
            float levelProgress = (float)exercise.currentLevel / GetMaxLevelForExercise(exercise.exerciseName);
            float performanceProgress = exercise.successRate;
            totalProgress += (levelProgress + performanceProgress) / 2f;
        }

        currentPatient.overallProgress = totalProgress / currentPatient.exercises.Count;
    }

    /// <summary>
    /// Gets recommended next exercise based on current progress
    /// </summary>
    public string GetRecommendedExercise()
    {
        ExerciseProgress weakestExercise = null;
        float lowestScore = float.MaxValue;

        foreach (var exercise in currentPatient.exercises)
        {
            if (!exercise.isUnlocked) continue;

            float score = exercise.successRate * 0.7f + (exercise.averageScore * 0.3f);
            if (score < lowestScore)
            {
                lowestScore = score;
                weakestExercise = exercise;
            }
        }

        return weakestExercise?.exerciseName ?? currentPatient.exercises[0].exerciseName;
    }

    /// <summary>
    /// Gets recommended difficulty level for an exercise
    /// </summary>
    public int GetRecommendedLevel(string exerciseName)
    {
        ExerciseProgress progress = GetExerciseProgress(exerciseName);
        if (progress == null) return 1;

        if (!enableAdaptiveDifficulty)
        {
            return progress.currentLevel;
        }

        // Adaptive difficulty based on recent performance
        if (progress.successRate > 0.8f)
        {
            return Mathf.Min(progress.currentLevel + 1, GetMaxLevelForExercise(exerciseName));
        }
        else if (progress.successRate < 0.6f)
        {
            return Mathf.Max(progress.currentLevel - 1, 1);
        }

        return progress.currentLevel;
    }

    /// <summary>
    /// Resets progress for an exercise (useful for rehabilitation setbacks)
    /// </summary>
    public void ResetExerciseProgress(string exerciseName)
    {
        ExerciseProgress progress = GetExerciseProgress(exerciseName);
        if (progress != null)
        {
            progress.currentLevel = 1;
            progress.successRate = 0f;
            progress.attempts = 0;
            progress.successfulAttempts = 0;
            progress.averageScore = 0f;

            Debug.Log($"Reset progress for exercise: {exerciseName}");
        }
    }

    /// <summary>
    /// Gets overall patient statistics
    /// </summary>
    public Dictionary<string, object> GetPatientStatistics()
    {
        var stats = new Dictionary<string, object>
        {
            ["PatientName"] = currentPatient.patientName,
            ["TotalSessions"] = currentPatient.totalSessions,
            ["OverallProgress"] = currentPatient.overallProgress,
            ["LastSession"] = currentPatient.lastSession,
            ["ExercisesCompleted"] = currentPatient.exercises.FindAll(e => e.successRate >= masteryThreshold).Count,
            ["TotalExercises"] = currentPatient.exercises.Count
        };

        return stats;
    }

    /// <summary>
    /// Saves patient progress (placeholder for persistence implementation)
    /// </summary>
    public void SaveProgress()
    {
        // TODO: Implement actual saving to file/database
        Debug.Log("Progress saved for patient: " + currentPatient.patientName);
    }

    /// <summary>
    /// Loads patient progress (placeholder for persistence implementation)
    /// </summary>
    public void LoadProgress(string patientId)
    {
        // TODO: Implement actual loading from file/database
        Debug.Log("Progress loaded for patient: " + patientId);
    }
}
