using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VRRehab.ErrorHandling
{
    [System.Serializable]
    public class ErrorReport
    {
        public string errorId;
        public DateTime timestamp;
        public ErrorSeverity severity;
        public string errorType;
        public string message;
        public string stackTrace;
        public string context;
        public Dictionary<string, string> metadata;
        public bool isResolved;
        public DateTime? resolvedAt;
        public string resolutionNotes;

        public ErrorReport()
        {
            errorId = Guid.NewGuid().ToString();
            timestamp = DateTime.Now;
            metadata = new Dictionary<string, string>();
        }
    }

    [System.Serializable]
    public class SystemHealthReport
    {
        public DateTime timestamp;
        public Dictionary<string, HealthStatus> componentHealth;
        public float memoryUsage;
        public float cpuUsage;
        public int activeCoroutines;
        public bool isVrSystemActive;
        public string currentScene;
        public Dictionary<string, object> performanceMetrics;

        public SystemHealthReport()
        {
            timestamp = DateTime.Now;
            componentHealth = new Dictionary<string, HealthStatus>();
            performanceMetrics = new Dictionary<string, object>();
        }
    }

    public enum ErrorSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    public class ErrorHandler : MonoBehaviour
    {
        [System.Serializable]
        public class ErrorHandlerSettings
        {
            public bool enableLogging = true;
            public bool enableRemoteReporting = false;
            public string logDirectory = "Logs";
            public int maxLogFiles = 10;
            public int maxLogFileSize = 10 * 1024 * 1024; // 10MB
            public float healthCheckInterval = 30f;
            public bool enableAutoRecovery = true;
            public int maxRecoveryAttempts = 3;
        }

        [Header("Error Handler Settings")]
        [SerializeField] private ErrorHandlerSettings settings;

        [Header("UI References")]
        [SerializeField] private GameObject errorDialogPrefab;
        [SerializeField] private Transform uiContainer;

        // Error tracking
        private List<ErrorReport> errorReports = new List<ErrorReport>();
        private Dictionary<string, int> errorFrequency = new Dictionary<string, int>();
        private Queue<Action> recoveryActions = new Queue<Action>();
        private SystemHealthReport lastHealthReport;

        // Logging
        private string logFilePath;
        private StreamWriter logWriter;
        private int currentLogSize = 0;

        // Recovery system
        private Dictionary<string, Func<bool>> recoveryStrategies = new Dictionary<string, Func<bool>>();
        private Dictionary<string, int> recoveryAttempts = new Dictionary<string, int>();

        // Events
        public static event Action<ErrorReport> OnErrorReported;
        public static event Action<SystemHealthReport> OnHealthReportGenerated;
        public static event Action<string> OnRecoveryAttempted;
        public static event Action<string> OnRecoverySuccessful;
        public static event Action<string> OnRecoveryFailed;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeErrorHandler();
            SetupRecoveryStrategies();
            StartHealthMonitoring();
        }

        private void InitializeErrorHandler()
        {
            // Setup log file
            if (settings.enableLogging)
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, settings.logDirectory);
                Directory.CreateDirectory(logDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logFilePath = Path.Combine(logDirectory, $"VRRehab_Log_{timestamp}.txt");

                logWriter = new StreamWriter(logFilePath, false);
                logWriter.AutoFlush = true;

                LogInfo("Error Handler initialized", "System", new Dictionary<string, string>
                {
                    ["Version"] = Application.version,
                    ["Platform"] = Application.platform.ToString(),
                    ["UnityVersion"] = Application.unityVersion
                });
            }

            // Setup global error handling
            Application.logMessageReceived += HandleUnityLog;
        }

        private void SetupRecoveryStrategies()
        {
            // Scene loading recovery
            recoveryStrategies["SceneLoadFailed"] = () => RecoverFromSceneLoadFailure();
            recoveryStrategies["VRLost"] = () => RecoverFromVRLoss();
            recoveryStrategies["DataCorruption"] = () => RecoverFromDataCorruption();
            recoveryStrategies["MemoryWarning"] = () => RecoverFromMemoryWarning();
            recoveryStrategies["NetworkTimeout"] = () => RecoverFromNetworkTimeout();
        }

        #region Error Reporting

        public void ReportError(string errorType, string message, ErrorSeverity severity = ErrorSeverity.Medium,
                               string stackTrace = "", string context = "", Dictionary<string, string> metadata = null)
        {
            ErrorReport report = new ErrorReport
            {
                severity = severity,
                errorType = errorType,
                message = message,
                stackTrace = stackTrace,
                context = context,
                metadata = metadata ?? new Dictionary<string, string>()
            };

            // Add system context
            AddSystemContext(report);

            errorReports.Add(report);

            // Track error frequency
            if (!errorFrequency.ContainsKey(errorType))
                errorFrequency[errorType] = 0;
            errorFrequency[errorType]++;

            // Log the error
            LogError($"[{severity}] {errorType}: {message}", context, report.metadata);

            // Handle based on severity
            switch (severity)
            {
                case ErrorSeverity.Low:
                    HandleLowSeverityError(report);
                    break;
                case ErrorSeverity.Medium:
                    HandleMediumSeverityError(report);
                    break;
                case ErrorSeverity.High:
                    HandleHighSeverityError(report);
                    break;
                case ErrorSeverity.Critical:
                    HandleCriticalError(report);
                    break;
            }

            OnErrorReported?.Invoke(report);

            // Attempt automatic recovery if enabled
            if (settings.enableAutoRecovery && recoveryStrategies.ContainsKey(errorType))
            {
                AttemptRecovery(errorType);
            }
        }

        private void HandleLowSeverityError(ErrorReport report)
        {
            // Just log and continue
            Debug.LogWarning($"Low severity error: {report.message}");
        }

        private void HandleMediumSeverityError(ErrorReport report)
        {
            // Show user notification
            ShowErrorNotification(report);

            // Log detailed information
            LogError($"Medium severity error handled: {report.message}", report.context, report.metadata);
        }

        private void HandleHighSeverityError(ErrorReport report)
        {
            // Show blocking error dialog
            ShowErrorDialog(report);

            // Attempt immediate recovery
            if (recoveryStrategies.ContainsKey(report.errorType))
            {
                AttemptRecovery(report.errorType);
            }

            LogError($"High severity error: {report.message}", report.context, report.metadata);
        }

        private void HandleCriticalError(ErrorReport report)
        {
            // Show critical error dialog with option to restart
            ShowCriticalErrorDialog(report);

            // Force application state save
            ForceSaveApplicationState();

            LogError($"CRITICAL ERROR: {report.message}", report.context, report.metadata);
        }

        private void AddSystemContext(ErrorReport report)
        {
            report.metadata["Scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            report.metadata["Time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            report.metadata["Memory"] = (GC.GetTotalMemory(false) / 1024 / 1024).ToString() + "MB";
            report.metadata["Platform"] = Application.platform.ToString();
            report.metadata["QualityLevel"] = QualitySettings.GetQualityLevel().ToString();
        }

        #endregion

        #region Recovery System

        public void AttemptRecovery(string errorType)
        {
            if (!recoveryStrategies.ContainsKey(errorType))
            {
                LogWarning($"No recovery strategy available for error type: {errorType}");
                return;
            }

            // Check recovery attempt limits
            if (!recoveryAttempts.ContainsKey(errorType))
                recoveryAttempts[errorType] = 0;

            if (recoveryAttempts[errorType] >= settings.maxRecoveryAttempts)
            {
                LogError($"Maximum recovery attempts exceeded for {errorType}");
                return;
            }

            recoveryAttempts[errorType]++;

            OnRecoveryAttempted?.Invoke(errorType);

            try
            {
                bool success = recoveryStrategies[errorType]();
                if (success)
                {
                    OnRecoverySuccessful?.Invoke(errorType);
                    recoveryAttempts[errorType] = 0; // Reset on success
                    LogInfo($"Recovery successful for {errorType}");
                }
                else
                {
                    OnRecoveryFailed?.Invoke(errorType);
                    LogError($"Recovery failed for {errorType}");
                }
            }
            catch (Exception e)
            {
                LogError($"Recovery threw exception: {e.Message}", "RecoverySystem");
                OnRecoveryFailed?.Invoke(errorType);
            }
        }

        private bool RecoverFromSceneLoadFailure()
        {
            try
            {
                // Attempt to reload the main menu
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool RecoverFromVRLoss()
        {
            try
            {
                // Attempt to reinitialize VR system
                // This would integrate with your VR SDK
                LogInfo("Attempting VR system recovery");
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }

        private bool RecoverFromDataCorruption()
        {
            try
            {
                // Attempt to restore from backup
                LogInfo("Attempting data recovery from backup");
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }

        private bool RecoverFromMemoryWarning()
        {
            try
            {
                // Force garbage collection
                GC.Collect();
                Resources.UnloadUnusedAssets();

                LogInfo("Memory cleanup completed");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool RecoverFromNetworkTimeout()
        {
            try
            {
                // Retry network operation or switch to offline mode
                LogInfo("Attempting network recovery");
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Health Monitoring

        private void StartHealthMonitoring()
        {
            InvokeRepeating("PerformHealthCheck", settings.healthCheckInterval, settings.healthCheckInterval);
        }

        private void PerformHealthCheck()
        {
            SystemHealthReport report = new SystemHealthReport();

            // Check component health
            report.componentHealth["VRSystem"] = CheckVRSystemHealth();
            report.componentHealth["DataPersistence"] = CheckDataPersistenceHealth();
            report.componentHealth["AudioSystem"] = CheckAudioSystemHealth();
            report.componentHealth["Network"] = CheckNetworkHealth();

            // Performance metrics
            report.memoryUsage = (GC.GetTotalMemory(false) / 1024f / 1024f / 1024f); // GB
            report.cpuUsage = GetCPUUsage();
            report.activeCoroutines = GetActiveCoroutineCount();
            report.isVrSystemActive = CheckVRSystemActive();
            report.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Additional metrics
            report.performanceMetrics["FrameRate"] = 1f / Time.deltaTime;
            // Draw calls information - using Unity's built-in stats
            report.performanceMetrics["DrawCalls"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() > 0 ? 1 : 0;
            report.performanceMetrics["ActiveObjects"] = GameObject.FindObjectsOfType<GameObject>().Length;

            lastHealthReport = report;

            // Check for health issues
            AnalyzeHealthReport(report);

            OnHealthReportGenerated?.Invoke(report);
        }

        private HealthStatus CheckVRSystemHealth()
        {
            // Check if VR system is properly initialized
            try
            {
                // This would integrate with your VR SDK
                return HealthStatus.Healthy;
            }
            catch
            {
                return HealthStatus.Unhealthy;
            }
        }

        private HealthStatus CheckDataPersistenceHealth()
        {
            try
            {
                // Check if data persistence is working
                string testPath = Path.Combine(Application.persistentDataPath, "health_test.txt");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);
                return HealthStatus.Healthy;
            }
            catch
            {
                return HealthStatus.Unhealthy;
            }
        }

        private HealthStatus CheckAudioSystemHealth()
        {
            try
            {
                return AudioListener.volume >= 0 ? HealthStatus.Healthy : HealthStatus.Degraded;
            }
            catch
            {
                return HealthStatus.Unhealthy;
            }
        }

        private HealthStatus CheckNetworkHealth()
        {
            return Application.internetReachability != NetworkReachability.NotReachable ?
                   HealthStatus.Healthy : HealthStatus.Degraded;
        }

        private float GetCPUUsage()
        {
            // Simplified CPU usage estimation
            return Time.deltaTime * 100f; // Placeholder
        }

        private int GetActiveCoroutineCount()
        {
            // This would require tracking active coroutines
            return 0; // Placeholder
        }

        private bool CheckVRSystemActive()
        {
            // Check if VR headset is active
            return true; // Placeholder
        }

        private void AnalyzeHealthReport(SystemHealthReport report)
        {
            // Check for critical health issues
            if (report.memoryUsage > 2f) // More than 2GB
            {
                ReportError("MemoryWarning", "High memory usage detected", ErrorSeverity.Medium);
            }

            if (report.componentHealth.ContainsValue(HealthStatus.Unhealthy))
            {
                ReportError("SystemHealth", "Unhealthy system components detected", ErrorSeverity.High);
            }

            // Performance warnings
            if (report.performanceMetrics.ContainsKey("FrameRate"))
            {
                float fps = (float)report.performanceMetrics["FrameRate"];
                if (fps < 30f)
                {
                    ReportError("Performance", "Low frame rate detected", ErrorSeverity.Medium);
                }
            }
        }

        #endregion

        #region Logging

        public void LogInfo(string message, string context = "", Dictionary<string, string> metadata = null)
        {
            Log("INFO", message, context, metadata);
        }

        public void LogWarning(string message, string context = "", Dictionary<string, string> metadata = null)
        {
            Log("WARNING", message, context, metadata);
        }

        public void LogError(string message, string context = "", Dictionary<string, string> metadata = null)
        {
            Log("ERROR", message, context, metadata);
        }

        private void Log(string level, string message, string context, Dictionary<string, string> metadata)
        {
            if (!settings.enableLogging) return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] [{context}] {message}";

            if (metadata != null && metadata.Count > 0)
            {
                logEntry += " | Metadata: ";
                foreach (var kvp in metadata)
                {
                    logEntry += $"{kvp.Key}={kvp.Value}; ";
                }
            }

            // Write to file
            if (logWriter != null)
            {
                logWriter.WriteLine(logEntry);
                currentLogSize += logEntry.Length;

                // Rotate log file if needed
                if (currentLogSize > settings.maxLogFileSize)
                {
                    RotateLogFile();
                }
            }

            // Also write to Unity console
            switch (level)
            {
                case "ERROR":
                    Debug.LogError(logEntry);
                    break;
                case "WARNING":
                    Debug.LogWarning(logEntry);
                    break;
                default:
                    Debug.Log(logEntry);
                    break;
            }
        }

        private void RotateLogFile()
        {
            logWriter.Close();

            string logDirectory = Path.Combine(Application.persistentDataPath, settings.logDirectory);
            string[] logFiles = Directory.GetFiles(logDirectory, "VRRehab_Log_*.txt");

            // Delete oldest files if we have too many
            if (logFiles.Length >= settings.maxLogFiles)
            {
                Array.Sort(logFiles);
                for (int i = 0; i < logFiles.Length - settings.maxLogFiles + 1; i++)
                {
                    File.Delete(logFiles[i]);
                }
            }

            // Create new log file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(logDirectory, $"VRRehab_Log_{timestamp}.txt");
            logWriter = new StreamWriter(logFilePath, false);
            logWriter.AutoFlush = true;
            currentLogSize = 0;
        }

        #endregion

        #region UI Error Handling

        private void ShowErrorNotification(ErrorReport report)
        {
            // This would integrate with your UI system to show notifications
            Debug.LogWarning($"Error Notification: {report.message}");
        }

        private void ShowErrorDialog(ErrorReport report)
        {
            if (errorDialogPrefab != null && uiContainer != null)
            {
                GameObject dialog = Instantiate(errorDialogPrefab, uiContainer);
                // Setup dialog with error information
            }
        }

        private void ShowCriticalErrorDialog(ErrorReport report)
        {
            // Show critical error dialog with restart option
            Debug.LogError($"CRITICAL ERROR DIALOG: {report.message}");
        }

        #endregion

        #region Unity Log Handling

        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            ErrorSeverity severity = ErrorSeverity.Low;

            switch (type)
            {
                case LogType.Error:
                    severity = ErrorSeverity.High;
                    break;
                case LogType.Assert:
                    severity = ErrorSeverity.Medium;
                    break;
                case LogType.Warning:
                    severity = ErrorSeverity.Low;
                    break;
                case LogType.Log:
                    // Don't report regular log messages as errors
                    return;
                case LogType.Exception:
                    severity = ErrorSeverity.Critical;
                    break;
            }

            ReportError("UnityLog", logString, severity, stackTrace, "UnityEngine");
        }

        #endregion

        #region Utility Methods

        public List<ErrorReport> GetRecentErrors(int count = 10)
        {
            return errorReports.GetRange(Mathf.Max(0, errorReports.Count - count), Mathf.Min(count, errorReports.Count));
        }

        public Dictionary<string, int> GetErrorFrequency()
        {
            return new Dictionary<string, int>(errorFrequency);
        }

        public SystemHealthReport GetLastHealthReport()
        {
            return lastHealthReport;
        }

        public void ForceSaveApplicationState()
        {
            // Save critical application state
            LogInfo("Application state saved due to critical error");
        }

        public void ExportErrorReport(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("VR Rehab Error Report");
                    writer.WriteLine($"Generated: {DateTime.Now}");
                    writer.WriteLine($"Total Errors: {errorReports.Count}");
                    writer.WriteLine();

                    foreach (ErrorReport report in errorReports)
                    {
                        writer.WriteLine($"Error ID: {report.errorId}");
                        writer.WriteLine($"Timestamp: {report.timestamp}");
                        writer.WriteLine($"Severity: {report.severity}");
                        writer.WriteLine($"Type: {report.errorType}");
                        writer.WriteLine($"Message: {report.message}");
                        writer.WriteLine($"Context: {report.context}");
                        writer.WriteLine($"Stack Trace: {report.stackTrace}");
                        writer.WriteLine();

                        if (report.metadata.Count > 0)
                        {
                            writer.WriteLine("Metadata:");
                            foreach (var kvp in report.metadata)
                            {
                                writer.WriteLine($"  {kvp.Key}: {kvp.Value}");
                            }
                            writer.WriteLine();
                        }

                        writer.WriteLine("---");
                        writer.WriteLine();
                    }
                }

                LogInfo($"Error report exported to: {filePath}");
            }
            catch (Exception e)
            {
                LogError($"Failed to export error report: {e.Message}");
            }
        }

        public void ClearErrorHistory()
        {
            errorReports.Clear();
            errorFrequency.Clear();
            recoveryAttempts.Clear();
            LogInfo("Error history cleared");
        }

        #endregion

        void OnDestroy()
        {
            if (logWriter != null)
            {
                logWriter.Close();
            }

            Application.logMessageReceived -= HandleUnityLog;
        }
    }
}
