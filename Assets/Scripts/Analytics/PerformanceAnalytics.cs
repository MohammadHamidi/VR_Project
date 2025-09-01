using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRRehab.DataPersistence;
using Random = Unity.Mathematics.Random;

namespace VRRehab.Analytics
{
    [System.Serializable]
    public class AnalyticsDataPoint
    {
        public DateTime timestamp;
        public string metricName;
        public float value;
        public string exerciseName;
        public int level;
        public string metadata;
    }

    [System.Serializable]
    public class PerformanceTrend
    {
        public string metricName;
        public List<float> values;
        public List<DateTime> timestamps;
        public float average;
        public float trend; // Positive = improving, negative = declining
        public float volatility; // How consistent the performance is
    }

    [System.Serializable]
    public class ExerciseAnalytics
    {
        public string exerciseName;
        public int totalSessions;
        public TimeSpan totalTimeSpent;
        public double averageScore;
        public float bestScore;
        public double successRate;
        public int currentLevel;
        public int maxLevelReached;
        public List<PerformanceTrend> trends;
        public Dictionary<string, float> skillBreakdown;
    }

    [System.Serializable]
    public class PatientAnalytics
    {
        public string patientId;
        public DateTime analysisDate;
        public TimeSpan totalRehabTime;
        public int totalSessions;
        public float overallProgress;
        public List<ExerciseAnalytics> exerciseAnalytics;
        public Dictionary<string, object> keyInsights;
        public List<string> recommendations;
        public Dictionary<string, PerformanceTrend> globalTrends;
    }

    public class PerformanceAnalytics : MonoBehaviour
    {
        [Header("Analytics Settings")]
        [SerializeField] private bool enableRealTimeTracking = true;
        [SerializeField] private float dataCollectionInterval = 1f; // seconds
        [SerializeField] private int maxDataPoints = 1000;
        [SerializeField] private bool enablePredictiveAnalytics = true;

        [Header("Trend Analysis")]
        [SerializeField] private int trendWindowSize = 10; // Number of data points for trend calculation
        [SerializeField] private float trendThreshold = 0.1f; // Minimum change to be considered a trend

        // Data storage
        private List<AnalyticsDataPoint> analyticsData = new List<AnalyticsDataPoint>();
        private Dictionary<string, Queue<AnalyticsDataPoint>> metricBuffers = new Dictionary<string, Queue<AnalyticsDataPoint>>();
        private DataPersistenceManager dataManager;
        private PatientProfile currentPatient;

        // Real-time tracking
        private Coroutine dataCollectionCoroutine;
        private Dictionary<string, float> realTimeMetrics = new Dictionary<string, float>();

        // Events
        public static event Action<PatientAnalytics> OnAnalyticsGenerated;
        public static event Action<string, float> OnMetricUpdated;
        public static event Action<string, PerformanceTrend> OnTrendDetected;

        void Awake()
        {
            dataManager = FindObjectOfType<DataPersistenceManager>();
            if (dataManager != null)
            {
                dataManager.OnProfileLoaded += OnProfileLoaded;
            }

            InitializeMetricBuffers();
            if (enableRealTimeTracking)
            {
                StartDataCollection();
            }
        }

        private void InitializeMetricBuffers()
        {
            string[] metrics = {
                "Accuracy", "Speed", "Consistency", "Endurance",
                "RangeOfMotion", "Balance", "Coordination", "Strength"
            };

            foreach (string metric in metrics)
            {
                metricBuffers[metric] = new Queue<AnalyticsDataPoint>();
            }
        }

        #region Data Collection

        public void RecordDataPoint(string metricName, float value, string exerciseName = "", int level = 1, string metadata = "")
        {
            AnalyticsDataPoint dataPoint = new AnalyticsDataPoint
            {
                timestamp = DateTime.Now,
                metricName = metricName,
                value = value,
                exerciseName = exerciseName,
                level = level,
                metadata = metadata
            };

            // Add to main data list
            analyticsData.Add(dataPoint);
            if (analyticsData.Count > maxDataPoints)
            {
                analyticsData.RemoveAt(0); // Remove oldest
            }

            // Add to metric buffer for trend analysis
            if (metricBuffers.ContainsKey(metricName))
            {
                metricBuffers[metricName].Enqueue(dataPoint);
                if (metricBuffers[metricName].Count > trendWindowSize)
                {
                    metricBuffers[metricName].Dequeue();
                }
            }

            // Update real-time metrics
            realTimeMetrics[metricName] = value;

            OnMetricUpdated?.Invoke(metricName, value);

            // Check for trends
            AnalyzeTrends(metricName);
        }

        public void RecordExerciseResult(string exerciseName, int level, float score, TimeSpan duration, Dictionary<string, float> detailedMetrics = null)
        {
            // Record main score
            RecordDataPoint("Score", score, exerciseName, level, $"Duration:{duration.TotalSeconds}");

            // Record detailed metrics if provided
            if (detailedMetrics != null)
            {
                foreach (var metric in detailedMetrics)
                {
                    RecordDataPoint(metric.Key, metric.Value, exerciseName, level);
                }
            }

            // Calculate derived metrics
            float accuracy = CalculateAccuracy(exerciseName, level, score);
            float consistency = CalculateConsistency(exerciseName);
            float improvement = CalculateImprovement(exerciseName);

            RecordDataPoint("Accuracy", accuracy, exerciseName, level);
            RecordDataPoint("Consistency", consistency, exerciseName, level);
            RecordDataPoint("Improvement", improvement, exerciseName, level);
        }

        private float CalculateAccuracy(string exerciseName, int level, float score)
        {
            // Exercise-specific accuracy calculation
            switch (exerciseName)
            {
                case "Throwing":
                    return Mathf.Clamp01(score / 1.0f); // Normalize to 0-1
                case "BridgeBuilding":
                    return Mathf.Clamp01(score / 100f); // Assume 100 is perfect
                case "SquatDodge":
                    return Mathf.Clamp01(score / 50f); // Assume 50 is perfect
                default:
                    return Mathf.Clamp01(score);
            }
        }

        private float CalculateConsistency(string exerciseName)
        {
            if (!metricBuffers.ContainsKey("Score")) return 0f;

            var scoreData = metricBuffers["Score"].Where(dp => dp.exerciseName == exerciseName).ToList();
            if (scoreData.Count < 2) return 1f; // Perfect consistency with limited data

            float mean = scoreData.Average(dp => dp.value);
            float variance = scoreData.Sum(dp => Mathf.Pow(dp.value - mean, 2)) / scoreData.Count;
            float standardDeviation = Mathf.Sqrt(variance);

            // Return consistency as 1 - (normalized standard deviation)
            return Mathf.Clamp01(1f - (standardDeviation / mean));
        }

        private float CalculateImprovement(string exerciseName)
        {
            if (!metricBuffers.ContainsKey("Score")) return 0f;

            var scoreData = metricBuffers["Score"].Where(dp => dp.exerciseName == exerciseName)
                                                   .OrderBy(dp => dp.timestamp)
                                                   .ToList();

            if (scoreData.Count < trendWindowSize) return 0f;

            // Compare recent performance to earlier performance
            int halfPoint = scoreData.Count / 2;
            float earlyAverage = scoreData.Take(halfPoint).Average(dp => dp.value);
            float recentAverage = scoreData.Skip(halfPoint).Average(dp => dp.value);

            return recentAverage - earlyAverage;
        }

        #endregion

        #region Trend Analysis

        private void AnalyzeTrends(string metricName)
        {
            if (!metricBuffers.ContainsKey(metricName) || metricBuffers[metricName].Count < trendWindowSize)
                return;

            PerformanceTrend trend = CalculateTrend(metricName);
            if (Mathf.Abs(trend.trend) >= trendThreshold)
            {
                OnTrendDetected?.Invoke(metricName, trend);
            }
        }

        private PerformanceTrend CalculateTrend(string metricName)
        {
            var dataPoints = metricBuffers[metricName].ToList();

            PerformanceTrend trend = new PerformanceTrend
            {
                metricName = metricName,
                values = dataPoints.Select(dp => dp.value).ToList(),
                timestamps = dataPoints.Select(dp => dp.timestamp).ToList(),
                average = dataPoints.Average(dp => dp.value)
            };

            // Calculate linear trend using simple linear regression
            int n = dataPoints.Count;
            if (n > 1)
            {
                float sumX = 0, sumY = trend.average * n, sumXY = 0, sumXX = 0;

                for (int i = 0; i < n; i++)
                {
                    float x = i; // Use index as time
                    float y = dataPoints[i].value;
                    sumX += x;
                    sumXY += x * y;
                    sumXX += x * x;
                }

                float slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
                trend.trend = slope;

                // Calculate volatility (standard deviation)
                float variance = dataPoints.Sum(dp => Mathf.Pow(dp.value - trend.average, 2)) / n;
                trend.volatility = Mathf.Sqrt(variance);
            }

            return trend;
        }

        #endregion

        #region Analytics Generation

        public PatientAnalytics GeneratePatientAnalytics()
        {
            if (currentPatient == null) return null;

            PatientAnalytics analytics = new PatientAnalytics
            {
                patientId = currentPatient.patientId,
                analysisDate = DateTime.Now,
                totalRehabTime = currentPatient.GetRehabilitationDuration(),
                totalSessions = currentPatient.totalSessionsCompleted,
                overallProgress = (float)currentPatient.GetAverageSessionScore(),
                exerciseAnalytics = new List<ExerciseAnalytics>(),
                keyInsights = new Dictionary<string, object>(),
                recommendations = new List<string>(),
                globalTrends = new Dictionary<string, PerformanceTrend>()
            };

            // Generate exercise-specific analytics
            foreach (var exerciseProgress in currentPatient.exerciseProgress)
            {
                ExerciseAnalytics exerciseAnalytics = GenerateExerciseAnalytics(exerciseProgress);
                analytics.exerciseAnalytics.Add(exerciseAnalytics);
            }

            // Generate global trends
            foreach (var metricBuffer in metricBuffers)
            {
                if (metricBuffer.Value.Count >= trendWindowSize)
                {
                    analytics.globalTrends[metricBuffer.Key] = CalculateTrend(metricBuffer.Key);
                }
            }

            // Generate insights and recommendations
            GenerateInsightsAndRecommendations(analytics);

            OnAnalyticsGenerated?.Invoke(analytics);
            return analytics;
        }

        private ExerciseAnalytics GenerateExerciseAnalytics(VRRehab.DataPersistence.ExerciseProgressData progress)
        {
            var exerciseData = analyticsData.Where(dp => dp.exerciseName == progress.exerciseName).ToList();

            ExerciseAnalytics analytics = new ExerciseAnalytics
            {
                exerciseName = progress.exerciseName,
                totalSessions = progress.totalAttempts,
                averageScore = progress.averageScore,
                bestScore = exerciseData.Any() ? exerciseData.Max(dp => dp.value) : 0f,
                successRate = progress.successRate,
                currentLevel = progress.currentLevel,
                maxLevelReached = progress.maxLevelReached,
                trends = new List<PerformanceTrend>(),
                skillBreakdown = new Dictionary<string, float>()
            };

            // Calculate time spent (approximation)
            analytics.totalTimeSpent = TimeSpan.FromMinutes(exerciseData.Count * 5); // Assume 5 minutes per session

            // Generate trends for this exercise
            foreach (var metricBuffer in metricBuffers)
            {
                var exerciseMetricData = metricBuffer.Value.Where(dp => dp.exerciseName == progress.exerciseName).ToList();
                if (exerciseMetricData.Count >= trendWindowSize)
                {
                    // Temporarily replace buffer data for calculation
                    var originalData = metricBuffer.Value.ToList();
                    metricBuffer.Value.Clear();
                    foreach (var dp in exerciseMetricData)
                    {
                        metricBuffer.Value.Enqueue(dp);
                    }

                    analytics.trends.Add(CalculateTrend(metricBuffer.Key));

                    // Restore original data
                    metricBuffer.Value.Clear();
                    foreach (var dp in originalData)
                    {
                        metricBuffer.Value.Enqueue(dp);
                    }
                }
            }

            // Generate skill breakdown
            analytics.skillBreakdown = GenerateSkillBreakdown(progress.exerciseName);

            return analytics;
        }

        private Dictionary<string, float> GenerateSkillBreakdown(string exerciseName)
        {
            var breakdown = new Dictionary<string, float>();

            // This would be customized based on the specific exercise
            switch (exerciseName)
            {
                case "Throwing":
                    breakdown["Accuracy"] = GetAverageMetricForExercise("Accuracy", exerciseName);
                    breakdown["Power"] = GetAverageMetricForExercise("Strength", exerciseName);
                    breakdown["Timing"] = GetAverageMetricForExercise("Speed", exerciseName);
                    break;
                case "BridgeBuilding":
                    breakdown["Planning"] = GetAverageMetricForExercise("Coordination", exerciseName);
                    breakdown["Stability"] = GetAverageMetricForExercise("Balance", exerciseName);
                    breakdown["Precision"] = GetAverageMetricForExercise("Accuracy", exerciseName);
                    break;
                case "SquatDodge":
                    breakdown["Reflexes"] = GetAverageMetricForExercise("Speed", exerciseName);
                    breakdown["Balance"] = GetAverageMetricForExercise("Balance", exerciseName);
                    breakdown["Endurance"] = GetAverageMetricForExercise("Endurance", exerciseName);
                    break;
            }

            return breakdown;
        }

        private float GetAverageMetricForExercise(string metricName, string exerciseName)
        {
            if (!metricBuffers.ContainsKey(metricName)) return 0f;

            var exerciseData = metricBuffers[metricName].Where(dp => dp.exerciseName == exerciseName).ToList();
            return exerciseData.Any() ? exerciseData.Average(dp => dp.value) : 0f;
        }

        private void GenerateInsightsAndRecommendations(PatientAnalytics analytics)
        {
            // Generate key insights
            analytics.keyInsights["MostImprovedExercise"] = GetMostImprovedExercise(analytics);
            analytics.keyInsights["MostConsistentExercise"] = GetMostConsistentExercise(analytics);
            analytics.keyInsights["RecommendedFocusArea"] = GetRecommendedFocusArea(analytics);
            analytics.keyInsights["WeeklyProgress"] = CalculateWeeklyProgress();
            analytics.keyInsights["PredictedRecoveryTime"] = EstimateRecoveryTime(analytics);

            // Generate recommendations
            analytics.recommendations.AddRange(GeneratePersonalizedRecommendations(analytics));
        }

        private string GetMostImprovedExercise(PatientAnalytics analytics)
        {
            ExerciseAnalytics bestImprovement = null;
            float bestTrend = float.MinValue;

            foreach (var exercise in analytics.exerciseAnalytics)
            {
                var improvementTrend = exercise.trends.FirstOrDefault(t => t.metricName == "Improvement");
                if (improvementTrend != null && improvementTrend.trend > bestTrend)
                {
                    bestImprovement = exercise;
                    bestTrend = improvementTrend.trend;
                }
            }

            return bestImprovement?.exerciseName ?? "None";
        }

        private string GetMostConsistentExercise(PatientAnalytics analytics)
        {
            ExerciseAnalytics mostConsistent = null;
            float bestConsistency = float.MinValue;

            foreach (var exercise in analytics.exerciseAnalytics)
            {
                var consistencyTrend = exercise.trends.FirstOrDefault(t => t.metricName == "Consistency");
                if (consistencyTrend != null && consistencyTrend.average > bestConsistency)
                {
                    mostConsistent = exercise;
                    bestConsistency = consistencyTrend.average;
                }
            }

            return mostConsistent?.exerciseName ?? "None";
        }

        private string GetRecommendedFocusArea(PatientAnalytics analytics)
        {
            // Find exercise with lowest performance
            ExerciseAnalytics weakestExercise = null;
            float lowestScore = float.MaxValue;

            foreach (var exercise in analytics.exerciseAnalytics)
            {
                if (exercise.averageScore < lowestScore)
                {
                    weakestExercise = exercise;
                    lowestScore = (float)exercise.averageScore;
                }
            }

            return weakestExercise?.exerciseName ?? "General Practice";
        }

        private float CalculateWeeklyProgress()
        {
            // Calculate progress over the last 7 days
            var recentData = analyticsData.Where(dp => (DateTime.Now - dp.timestamp).TotalDays <= 7).ToList();

            if (recentData.Count < 2) return 0f;

            var earlyWeek = recentData.Where(dp => (DateTime.Now - dp.timestamp).TotalDays > 3.5).ToList();
            var lateWeek = recentData.Where(dp => (DateTime.Now - dp.timestamp).TotalDays <= 3.5).ToList();

            if (earlyWeek.Any() && lateWeek.Any())
            {
                float earlyAverage = earlyWeek.Average(dp => dp.value);
                float lateAverage = lateWeek.Average(dp => dp.value);
                return lateAverage - earlyAverage;
            }

            return 0f;
        }

        private TimeSpan EstimateRecoveryTime(PatientAnalytics analytics)
        {
            if (analytics.overallProgress >= 1f) return TimeSpan.Zero;

            // Simple estimation based on current progress and trends
            float progressRate = CalculateWeeklyProgress();
            if (progressRate <= 0) return TimeSpan.MaxValue; // No progress

            float remainingProgress = 1f - analytics.overallProgress;
            float weeksRemaining = remainingProgress / progressRate;

            return TimeSpan.FromDays(weeksRemaining * 7);
        }

        private List<string> GeneratePersonalizedRecommendations(PatientAnalytics analytics)
        {
            List<string> recommendations = new List<string>();

            // Analyze each exercise
            foreach (var exercise in analytics.exerciseAnalytics)
            {
                if (exercise.successRate < 0.7f)
                {
                    recommendations.Add($"Focus on improving {exercise.exerciseName} accuracy through more practice.");
                }

                if (exercise.trends.Any(t => t.volatility > 0.3f))
                {
                    recommendations.Add($"Work on consistency in {exercise.exerciseName} - performance varies significantly.");
                }

                if (exercise.currentLevel == 1 && exercise.totalSessions > 5)
                {
                    recommendations.Add($"Consider advancing to higher difficulty levels in {exercise.exerciseName}.");
                }
            }

            // General recommendations
            if (analytics.totalSessions < 5)
            {
                recommendations.Add("Continue with regular sessions to establish baseline performance.");
            }

            if (analytics.overallProgress < 0.5f)
            {
                recommendations.Add("Focus on fundamental movements before advancing to complex exercises.");
            }

            return recommendations;
        }

        #endregion

        #region Real-time Tracking

        private void StartDataCollection()
        {
            if (dataCollectionCoroutine != null)
                StopCoroutine(dataCollectionCoroutine);

            dataCollectionCoroutine = StartCoroutine(DataCollectionRoutine());
        }

        private IEnumerator DataCollectionRoutine()
        {
            while (true)
            {
                // Collect real-time performance metrics
                CollectRealTimeMetrics();
                yield return new WaitForSeconds(dataCollectionInterval);
            }
        }

        private void CollectRealTimeMetrics()
        {
            // This would integrate with your VR tracking systems
            // For now, we'll simulate some metrics

            // Example: Track head movement smoothness
            if (Camera.main != null)
            {
                float headMovement = CalculateHeadMovement();
                RecordDataPoint("HeadStability", headMovement);
            }

            // Example: Track hand coordination
            float handCoordination = CalculateHandCoordination();
            RecordDataPoint("HandCoordination", handCoordination);
        }

        private float CalculateHeadMovement()
        {
            // Simplified head movement calculation
            // In a real implementation, you'd track head position over time
            return UnityEngine.Random.Range(0.5f, 1.0f); // Simulated value
        }

        private float CalculateHandCoordination()
        {
            // Simplified hand coordination calculation
            // In a real implementation, you'd track hand positions and movements
            return UnityEngine.Random.Range(0.3f, 0.8f); // Simulated value
        }

        #endregion

        #region Event Handlers

        private void OnProfileLoaded(PatientProfile profile)
        {
            currentPatient = profile;

            // Load existing analytics data for this patient
            // In a real implementation, you'd load from persistent storage
        }

        #endregion

        #region Utility Methods

        public Dictionary<string, float> GetCurrentMetrics()
        {
            return new Dictionary<string, float>(realTimeMetrics);
        }

        public List<AnalyticsDataPoint> GetDataPointsForMetric(string metricName, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = analyticsData.Where(dp => dp.metricName == metricName);

            if (startDate.HasValue)
                query = query.Where(dp => dp.timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(dp => dp.timestamp <= endDate.Value);

            return query.OrderBy(dp => dp.timestamp).ToList();
        }

        public Dictionary<string, object> GetAnalyticsSummary()
        {
            return new Dictionary<string, object>
            {
                ["TotalDataPoints"] = analyticsData.Count,
                ["ActiveMetrics"] = realTimeMetrics.Count,
                ["DateRange"] = analyticsData.Any() ?
                    $"{analyticsData.Min(dp => dp.timestamp):yyyy-MM-dd} to {analyticsData.Max(dp => dp.timestamp):yyyy-MM-dd}" :
                    "No data",
                ["MostTrackedMetric"] = GetMostTrackedMetric()
            };
        }

        private string GetMostTrackedMetric()
        {
            return analyticsData.GroupBy(dp => dp.metricName)
                               .OrderByDescending(g => g.Count())
                               .FirstOrDefault()?.Key ?? "None";
        }

        #endregion

        void OnDestroy()
        {
            if (dataManager != null)
            {
                dataManager.OnProfileLoaded -= OnProfileLoaded;
            }

            if (dataCollectionCoroutine != null)
            {
                StopCoroutine(dataCollectionCoroutine);
            }
        }
    }
}
