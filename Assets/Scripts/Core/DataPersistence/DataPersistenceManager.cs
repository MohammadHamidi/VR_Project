using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRRehab.DataPersistence;

namespace VRRehab.DataPersistence
{
    public class DataPersistenceManager : MonoBehaviour
    {
        [Header("Persistence Settings")]
        [SerializeField] private string saveFileName = "patient_profiles.json";
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        [SerializeField] private bool createBackupOnSave = true;
        [SerializeField] private int maxBackups = 5;

        [Header("Data Paths")]
        [SerializeField] private string dataDirectory = "PatientData";

        // Events
        public event Action<PatientProfile> OnProfileLoaded;
        public static event Action<PatientProfile> OnProfileSaved;
        public static event Action<string> OnProfileDeleted;
        public static event Action OnDataBackupCreated;

        // Current data
        private Dictionary<string, PatientProfile> patientProfiles = new Dictionary<string, PatientProfile>();
        private PatientProfile currentPatient;
        private string persistentDataPath;
        private string backupDirectory;

        // Auto-save
        private float lastAutoSaveTime;

        void Awake()
        {
            InitializePaths();
            LoadAllProfiles();
            StartAutoSaveTimer();
        }

        void Update()
        {
            HandleAutoSave();
        }

        private void InitializePaths()
        {
            persistentDataPath = Path.Combine(Application.persistentDataPath, dataDirectory);
            backupDirectory = Path.Combine(persistentDataPath, "Backups");

            // Create directories if they don't exist
            if (!Directory.Exists(persistentDataPath))
                Directory.CreateDirectory(persistentDataPath);

            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);
        }

        private void StartAutoSaveTimer()
        {
            lastAutoSaveTime = Time.time;
        }

        private void HandleAutoSave()
        {
            if (!autoSave) return;

            if (Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                SaveAllProfiles();
                lastAutoSaveTime = Time.time;
            }
        }

        #region Profile Management

        public void CreateNewProfile(string firstName, string lastName, DateTime dateOfBirth)
        {
            PatientProfile newProfile = new PatientProfile
            {
                firstName = firstName,
                lastName = lastName,
                dateOfBirth = dateOfBirth,
                rehabilitationStartDate = DateTime.Now
            };

            patientProfiles[newProfile.patientId] = newProfile;
            currentPatient = newProfile;

            SaveProfile(newProfile);
            Debug.Log($"Created new patient profile: {newProfile.fullName}");
        }

        public void LoadProfile(string patientId)
        {
            if (patientProfiles.TryGetValue(patientId, out PatientProfile profile))
            {
                currentPatient = profile;
                OnProfileLoaded?.Invoke(profile);
                Debug.Log($"Loaded patient profile: {profile.fullName}");
            }
            else
            {
                Debug.LogError($"Patient profile not found: {patientId}");
            }
        }

        public void SaveProfile(PatientProfile profile)
        {
            if (profile == null) return;

            profile.UpdateLastModified();

            // Create backup if enabled
            if (createBackupOnSave)
            {
                CreateBackup(profile);
            }

            try
            {
                string jsonData = JsonUtility.ToJson(profile, true);
                string filePath = GetProfileFilePath(profile.patientId);
                File.WriteAllText(filePath, jsonData);

                patientProfiles[profile.patientId] = profile;
                OnProfileSaved?.Invoke(profile);

                Debug.Log($"Saved patient profile: {profile.fullName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save patient profile: {e.Message}");
            }
        }

        public void DeleteProfile(string patientId)
        {
            if (patientProfiles.TryGetValue(patientId, out PatientProfile profile))
            {
                try
                {
                    string filePath = GetProfileFilePath(patientId);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    patientProfiles.Remove(patientId);

                    if (currentPatient?.patientId == patientId)
                    {
                        currentPatient = null;
                    }

                    OnProfileDeleted?.Invoke(patientId);
                    Debug.Log($"Deleted patient profile: {profile.fullName}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete patient profile: {e.Message}");
                }
            }
        }

        public List<PatientProfile> GetAllProfiles()
        {
            return new List<PatientProfile>(patientProfiles.Values);
        }

        public PatientProfile GetCurrentProfile()
        {
            return currentPatient;
        }

        public void SetCurrentProfile(PatientProfile profile)
        {
            currentPatient = profile;
        }

        #endregion

        #region Session Management

        public void StartNewSession(string primaryExercise)
        {
            if (currentPatient == null) return;

            SessionRecord newSession = new SessionRecord
            {
                primaryExercise = primaryExercise,
                sessionDate = DateTime.Now
            };

            currentPatient.sessionHistory.Add(newSession);
            SaveProfile(currentPatient);
        }

        public void EndCurrentSession(double averageScore, string notes = "")
        {
            if (currentPatient == null || currentPatient.sessionHistory.Count == 0) return;

            SessionRecord lastSession = currentPatient.sessionHistory[currentPatient.sessionHistory.Count - 1];
            lastSession.sessionDuration = DateTime.Now - lastSession.sessionDate;
            lastSession.averageScore = averageScore;
            lastSession.notes = notes;
            lastSession.completedSuccessfully = averageScore >= 0.6f; // Configurable threshold

            currentPatient.AddSession(lastSession);
            SaveProfile(currentPatient);
        }

        public void RecordExerciseResult(string exerciseName, int level, bool completed, double score)
        {
            if (currentPatient == null || currentPatient.sessionHistory.Count == 0) return;

            SessionRecord currentSession = currentPatient.sessionHistory[currentPatient.sessionHistory.Count - 1];

            ExerciseResult result = new ExerciseResult
            {
                exerciseName = exerciseName,
                levelAttempted = level,
                completed = completed,
                score = score,
                timeSpent = DateTime.Now - currentSession.sessionDate,
                attempts = 1 // Could be tracked more precisely
            };

            currentSession.exerciseResults.Add(result);
            SaveProfile(currentPatient);
        }

        #endregion

        #region Data Persistence

        private void LoadAllProfiles()
        {
            try
            {
                string[] files = Directory.GetFiles(persistentDataPath, "*.json");
                patientProfiles.Clear();

                foreach (string file in files)
                {
                    try
                    {
                        string jsonData = File.ReadAllText(file);
                        PatientProfile profile = JsonUtility.FromJson<PatientProfile>(jsonData);

                        if (profile != null && !string.IsNullOrEmpty(profile.patientId))
                        {
                            patientProfiles[profile.patientId] = profile;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load profile from {file}: {e.Message}");
                    }
                }

                Debug.Log($"Loaded {patientProfiles.Count} patient profiles");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load patient profiles: {e.Message}");
            }
        }

        private void SaveAllProfiles()
        {
            foreach (var profile in patientProfiles.Values)
            {
                SaveProfile(profile);
            }
        }

        private string GetProfileFilePath(string patientId)
        {
            return Path.Combine(persistentDataPath, $"{patientId}.json");
        }

        #endregion

        #region Backup System

        private void CreateBackup(PatientProfile profile)
        {
            try
            {
                string profileFileName = $"{profile.patientId}.json";
                string profilePath = GetProfileFilePath(profile.patientId);
                string backupPath = Path.Combine(backupDirectory, $"{profile.patientId}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                if (File.Exists(profilePath))
                {
                    File.Copy(profilePath, backupPath, true);
                    CleanupOldBackups(profile.patientId);
                    OnDataBackupCreated?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create backup: {e.Message}");
            }
        }

        private void CleanupOldBackups(string patientId)
        {
            try
            {
                string[] backupFiles = Directory.GetFiles(backupDirectory, $"{patientId}_*.json");

                if (backupFiles.Length > maxBackups)
                {
                    Array.Sort(backupFiles);
                    for (int i = 0; i < backupFiles.Length - maxBackups; i++)
                    {
                        File.Delete(backupFiles[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to cleanup backups: {e.Message}");
            }
        }

        public void RestoreFromBackup(string patientId, string backupFileName)
        {
            try
            {
                string backupPath = Path.Combine(backupDirectory, backupFileName);
                string profilePath = GetProfileFilePath(patientId);

                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, profilePath, true);
                    LoadAllProfiles();
                    Debug.Log($"Restored profile {patientId} from backup");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore from backup: {e.Message}");
            }
        }

        #endregion

        #region Utility Methods

        public List<string> GetBackupFiles(string patientId)
        {
            try
            {
                string[] files = Directory.GetFiles(backupDirectory, $"{patientId}_*.json");
                List<string> backupFiles = new List<string>();

                foreach (string file in files)
                {
                    backupFiles.Add(Path.GetFileName(file));
                }

                return backupFiles;
            }
            catch
            {
                return new List<string>();
            }
        }

        public Dictionary<string, object> GetStorageStatistics()
        {
            return new Dictionary<string, object>
            {
                ["TotalProfiles"] = patientProfiles.Count,
                ["DataDirectorySize"] = GetDirectorySize(persistentDataPath),
                ["BackupDirectorySize"] = GetDirectorySize(backupDirectory),
                ["LastSaveTime"] = lastAutoSaveTime
            };
        }

        private long GetDirectorySize(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return 0;

                long size = 0;
                string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);
                    size += info.Length;
                }

                return size;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        void OnApplicationQuit()
        {
            if (autoSave)
            {
                SaveAllProfiles();
            }
        }
    }
}
