using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRRehab.DataPersistence
{
    [System.Serializable]
    public class PatientProfile
    {
        // Basic Information
        public string patientId;
        public string firstName;
        public string lastName;
        public string fullName => $"{firstName} {lastName}";
        public DateTime dateOfBirth;
        public string gender;
        public string medicalRecordNumber;

        // Contact Information
        public string email;
        public string phoneNumber;
        public string emergencyContactName;
        public string emergencyContactPhone;

        // Medical Information
        public List<string> diagnoses = new List<string>();
        public List<string> medications = new List<string>();
        public string primaryCarePhysician;
        public string therapistName;

        // Rehabilitation Information
        public DateTime rehabilitationStartDate;
        public string rehabilitationGoal;
        public string currentCondition;
        public int sessionsPerWeek;
        public int sessionDurationMinutes;

        // Preferences
        public bool enableVoiceGuidance = true;
        public bool enableHapticFeedback = true;
        public bool enableProgressNotifications = true;
        public string preferredHand = "Right"; // Right, Left, Ambidextrous

        // Session Tracking
        public DateTime lastSessionDate;
        public int totalSessionsCompleted;
        public int consecutiveDaysActive;
        public List<SessionRecord> sessionHistory = new List<SessionRecord>();

        // Exercise Progress (this will be populated from ProgressionSystem)
        public List<ExerciseProgressData> exerciseProgress = new List<ExerciseProgressData>();

        // Achievements and Milestones
        public List<string> achievements = new List<string>();
        public List<MilestoneRecord> milestones = new List<MilestoneRecord>();

        // System Metadata
        public DateTime profileCreatedDate;
        public DateTime lastModifiedDate;
        public string profileVersion = "1.0";
        public bool isActive = true;

        // Constructor
        public PatientProfile()
        {
            patientId = Guid.NewGuid().ToString();
            profileCreatedDate = DateTime.Now;
            lastModifiedDate = DateTime.Now;
            rehabilitationStartDate = DateTime.Now;
        }

        // Methods
        public void UpdateLastModified()
        {
            lastModifiedDate = DateTime.Now;
        }

        public void AddSession(SessionRecord session)
        {
            sessionHistory.Add(session);
            lastSessionDate = session.sessionDate;
            totalSessionsCompleted++;
            UpdateLastModified();
        }

        public void AddAchievement(string achievement)
        {
            if (!achievements.Contains(achievement))
            {
                achievements.Add(achievement);
                UpdateLastModified();
            }
        }

        public void AddMilestone(MilestoneRecord milestone)
        {
            milestones.Add(milestone);
            UpdateLastModified();
        }

        public int GetAge()
        {
            return DateTime.Now.Year - dateOfBirth.Year -
                   (DateTime.Now.DayOfYear < dateOfBirth.DayOfYear ? 1 : 0);
        }

        public TimeSpan GetRehabilitationDuration()
        {
            return DateTime.Now - rehabilitationStartDate;
        }

        public double GetAverageSessionScore()
        {
            if (sessionHistory.Count == 0) return 0;

            double totalScore = 0;
            int validSessions = 0;

            foreach (var session in sessionHistory)
            {
                if (session.averageScore >= 0)
                {
                    totalScore += session.averageScore;
                    validSessions++;
                }
            }

            return validSessions > 0 ? totalScore / validSessions : 0;
        }

        public Dictionary<string, object> GetProfileSummary()
        {
            return new Dictionary<string, object>
            {
                ["FullName"] = fullName,
                ["Age"] = GetAge(),
                ["TotalSessions"] = totalSessionsCompleted,
                ["RehabDuration"] = GetRehabilitationDuration().Days,
                ["LastSession"] = lastSessionDate,
                ["AverageScore"] = GetAverageSessionScore(),
                ["Achievements"] = achievements.Count,
                ["ActiveExercises"] = exerciseProgress.FindAll(e => e.isUnlocked).Count
            };
        }
    }

    [System.Serializable]
    public class SessionRecord
    {
        public string sessionId;
        public DateTime sessionDate;
        public TimeSpan sessionDuration;
        public string primaryExercise;
        public List<ExerciseResult> exerciseResults = new List<ExerciseResult>();
        public double averageScore;
        public string notes;
        public bool completedSuccessfully;

        public SessionRecord()
        {
            sessionId = Guid.NewGuid().ToString();
            sessionDate = DateTime.Now;
        }
    }

    [System.Serializable]
    public class ExerciseResult
    {
        public string exerciseName;
        public int levelAttempted;
        public bool completed;
        public double score;
        public TimeSpan timeSpent;
        public int attempts;
        public string performanceNotes;
    }

    [System.Serializable]
    public class ExerciseProgressData
    {
        public string exerciseName;
        public int currentLevel;
        public int maxLevelReached;
        public double successRate;
        public int totalAttempts;
        public int successfulAttempts;
        public double averageScore;
        public DateTime lastPlayed;
        public bool isUnlocked;
        public bool isMastered;
    }

    [System.Serializable]
    public class MilestoneRecord
    {
        public string milestoneId;
        public string milestoneName;
        public string description;
        public DateTime achievedDate;
        public string exerciseName;
        public int levelAchieved;
    }
}
