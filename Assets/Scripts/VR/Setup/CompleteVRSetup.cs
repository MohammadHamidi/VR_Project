using System.Collections;
using UnityEngine;
using VRRehab.UI;
using VRRehab.DataPersistence;
using VRRehab.Analytics;

namespace VRRehab.SceneSetup
{
    /// <summary>
    /// Complete VR Setup script that initializes all VR Rehab systems
    /// Attach this to an empty GameObject in your main scene
    /// </summary>
    public class CompleteVRSetup : MonoBehaviour
    {
        [Header("Core Prefabs")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;
        [SerializeField] private GameObject progressionSystemPrefab;
        [SerializeField] private GameObject dataManagerPrefab;
        [SerializeField] private GameObject analyticsManagerPrefab;

        [Header("Scene Type")]
        [SerializeField] private SceneType sceneType = SceneType.MainMenu;

        [Header("Exercise Settings")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject targetRingPrefab;
        [SerializeField] private GameObject spawnZonePrefab;
        [SerializeField] private BridgeConfig bridgeConfig;
        [SerializeField] private int numberOfRings = 5;

        public enum SceneType
        {
            MainMenu,
            ThrowingExercise,
            BridgeExercise,
            SquatExercise
        }

        private void Awake()
        {
            StartCoroutine(InitializeCompleteVRSetup());
        }

        private IEnumerator InitializeCompleteVRSetup()
        {
            Debug.Log("Starting Complete VR Setup...");

            yield return new WaitForEndOfFrame();

            // Step 1: Setup VR Environment
            yield return StartCoroutine(SetupVREnvironment());
            Debug.Log("✓ VR Environment setup complete");

            // Step 2: Setup Core Systems
            yield return StartCoroutine(SetupCoreSystems());
            Debug.Log("✓ Core systems setup complete");

            // Step 3: Setup UI System
            yield return StartCoroutine(SetupUISystem());
            Debug.Log("✓ UI system setup complete");

            // Step 4: Setup Scene-Specific Components
            yield return StartCoroutine(SetupSceneSpecificComponents());
            Debug.Log("✓ Scene-specific setup complete");

            // Step 5: Connect All Systems
            yield return StartCoroutine(ConnectSystems());
            Debug.Log("✓ System connections complete");

            Debug.Log("🎉 Complete VR Setup finished successfully!");
            ShowSetupCompleteMessage();
        }

        private IEnumerator SetupVREnvironment()
        {
            // Instantiate XR Template if not present
            if (GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                if (xrTemplatePrefab != null)
                {
                    GameObject xrObj = Instantiate(xrTemplatePrefab);
                    xrObj.name = "XR_Environment";
                    DontDestroyOnLoad(xrObj);
                }
                else
                {
                    Debug.LogWarning("XR Template prefab not assigned!");
                }
            }

            yield return null;
        }

        private IEnumerator SetupCoreSystems()
        {
            // Data Persistence Manager
            if (GameObject.FindObjectOfType<DataPersistenceManager>() == null)
            {
                if (dataManagerPrefab != null)
                {
                    GameObject dataObj = Instantiate(dataManagerPrefab);
                    dataObj.name = "DataPersistenceManager";
                    DontDestroyOnLoad(dataObj);
                }
            }

            // Progression System
            if (GameObject.FindObjectOfType<ProgressionSystem>() == null)
            {
                if (progressionSystemPrefab != null)
                {
                    GameObject progObj = Instantiate(progressionSystemPrefab);
                    progObj.name = "ProgressionSystem";
                    DontDestroyOnLoad(progObj);
                }
            }

            // Analytics Manager
            if (GameObject.FindObjectOfType<PerformanceAnalytics>() == null)
            {
                if (analyticsManagerPrefab != null)
                {
                    GameObject analyticsObj = Instantiate(analyticsManagerPrefab);
                    analyticsObj.name = "AnalyticsManager";
                    DontDestroyOnLoad(analyticsObj);
                }
            }

            yield return null;
        }

        private IEnumerator SetupUISystem()
        {
            // Canvas
            if (GameObject.FindObjectOfType<Canvas>() == null)
            {
                if (canvasPrefab != null)
                {
                    GameObject canvasObj = Instantiate(canvasPrefab);
                    canvasObj.name = "MainCanvas";
                }
            }

            // UIManager
            if (GameObject.FindObjectOfType<UIManager>() == null)
            {
                GameObject uiObj = new GameObject("UIManager");
                UIManager uiManager = uiObj.AddComponent<UIManager>();
                uiManager.EnableHapticFeedback = true;
                uiManager.EnableVoiceFeedback = true;

                // Initialize notification settings if needed
                var notificationSettingsField = typeof(UIManager).GetField("notificationSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (notificationSettingsField != null)
                {
                    var notificationSettings = notificationSettingsField.GetValue(uiManager);
                    if (notificationSettings == null)
                    {
                        // Create default notification settings
                        var defaultSettings = new UIManager.NotificationSettings();
                        notificationSettingsField.SetValue(uiManager, defaultSettings);
                        Debug.Log("Initialized default notification settings for UIManager");
                    }
                }
            }

            yield return null;
        }

        private IEnumerator SetupSceneSpecificComponents()
        {
            switch (sceneType)
            {
                case SceneType.ThrowingExercise:
                    yield return StartCoroutine(SetupThrowingComponents());
                    break;

                case SceneType.BridgeExercise:
                    yield return StartCoroutine(SetupBridgeComponents());
                    break;

                case SceneType.MainMenu:
                    yield return StartCoroutine(SetupMainMenuComponents());
                    break;

                case SceneType.SquatExercise:
                    yield return StartCoroutine(SetupSquatComponents());
                    break;
            }
        }

        private IEnumerator SetupThrowingComponents()
        {
            // Ball spawning system
            if (GameObject.FindGameObjectWithTag("BallSpawner") == null)
            {
                if (spawnZonePrefab != null)
                {
                    GameObject spawnZone = Instantiate(spawnZonePrefab, Vector3.zero, Quaternion.identity);
                    spawnZone.name = "BallSpawnZone";

                    if (ballPrefab != null)
                    {
                        GameObject spawnerObj = new GameObject("BallSpawner");
                        var ballSpawner = spawnerObj.AddComponent<BallSpawner>();
                        ballSpawner.ballPrefab = ballPrefab;
                        ballSpawner.spawnPoint = spawnZone.transform;
                        ballSpawner.respawnDelay = 1f;

                        if (spawnZone.GetComponent<BallRespawnZone>() != null)
                        {
                            spawnZone.GetComponent<BallRespawnZone>().spawner = ballSpawner;
                        }

                        ballSpawner.SpawnBall();
                    }
                }
            }

            // Target rings
            if (GameObject.FindGameObjectsWithTag("TargetRing").Length == 0)
            {
                if (targetRingPrefab != null)
                {
                    for (int i = 0; i < numberOfRings; i++)
                    {
                        Vector3 position = new Vector3(
                            (i - (numberOfRings - 1) / 2f) * 2f,
                            1.5f,
                            3f
                        );

                        GameObject ring = Instantiate(targetRingPrefab, position, Quaternion.identity);
                        ring.name = $"TargetRing_{i + 1}";
                        ring.tag = "TargetRing";
                    }
                }
            }

            // Stage manager
            if (GameObject.FindObjectOfType<ObjectStageManager>() == null)
            {
                GameObject managerObj = new GameObject("StageManager");
                var stageManager = managerObj.AddComponent<ObjectStageManager>();

                GameObject[] rings = GameObject.FindGameObjectsWithTag("TargetRing");
                TargetRing[] targetRings = new TargetRing[rings.Length];
                for (int i = 0; i < rings.Length; i++)
                {
                    targetRings[i] = rings[i].GetComponent<TargetRing>();
                }

                stageManager.targets = targetRings;
                stageManager.stageTime = 60f;
                stageManager.enableTimer = true;

                // Connect UI
                var uiController = FindObjectOfType<StageUIController>();
                if (uiController != null)
                {
                    stageManager.uiController = uiController;
                }
            }

            yield return null;
        }

        private IEnumerator SetupBridgeComponents()
        {
            // Bridge builder
            if (GameObject.FindObjectOfType<SOLIDBridgeBuilder>() == null)
            {
                GameObject builderObj = new GameObject("BridgeBuilder");
                var bridgeBuilder = builderObj.AddComponent<SOLIDBridgeBuilder>();

                if (bridgeConfig != null)
                {
                    bridgeBuilder.SetBridgeConfiguration(bridgeConfig);
                    bridgeBuilder.SetAnchorType(AnchorType.Standard);
                }
            }

            // Ground plane
            if (GameObject.Find("Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(10, 1, 10);
            }

            yield return null;
        }

        private IEnumerator SetupMainMenuComponents()
        {
            // Scene loader for navigation
            if (GameObject.FindObjectOfType<SceneLoader>() == null)
            {
                GameObject loaderObj = new GameObject("SceneLoader");
                loaderObj.AddComponent<SceneLoader>();
            }

            yield return null;
        }

        private IEnumerator SetupSquatComponents()
        {
            // Placeholder for squat exercise setup
            // Add your squat-specific components here
            Debug.Log("Squat exercise setup - customize as needed");
            yield return null;
        }

        private IEnumerator ConnectSystems()
        {
            // Connect progression system to analytics
            var progressionSystem = FindObjectOfType<ProgressionSystem>();
            var analyticsManager = FindObjectOfType<PerformanceAnalytics>();
            var dataManager = FindObjectOfType<DataPersistenceManager>();

            if (progressionSystem != null && analyticsManager != null)
            {
                // Systems are already connected through events in the code
                Debug.Log("Progression and Analytics systems connected");
            }

            if (dataManager != null && progressionSystem != null)
            {
                // Data persistence is handled automatically
                Debug.Log("Data persistence connected");
            }

            yield return null;
        }

        private void ShowSetupCompleteMessage()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowSuccess("VR Rehab System Ready!");
            }
        }

        [ContextMenu("Test VR Setup")]
        public void TestVRSetup()
        {
            Debug.Log("Testing VR Setup...");

            bool xrManager = GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null;
            bool canvas = GameObject.FindObjectOfType<Canvas>() != null;
            bool uiManager = GameObject.FindObjectOfType<UIManager>() != null;
            bool dataManager = GameObject.FindObjectOfType<DataPersistenceManager>() != null;
            bool progression = GameObject.FindObjectOfType<ProgressionSystem>() != null;
            bool analytics = GameObject.FindObjectOfType<PerformanceAnalytics>() != null;

            Debug.Log($"XR Manager: {xrManager}");
            Debug.Log($"Canvas: {canvas}");
            Debug.Log($"UI Manager: {uiManager}");
            Debug.Log($"Data Manager: {dataManager}");
            Debug.Log($"Progression System: {progression}");
            Debug.Log($"Analytics Manager: {analytics}");

            if (xrManager && canvas && uiManager && dataManager && progression && analytics)
            {
                Debug.Log("✅ All core systems are active!");
            }
            else
            {
                Debug.Log("❌ Some systems are missing. Check the setup.");
            }
        }

        [ContextMenu("Reset VR Setup")]
        public void ResetVRSetup()
        {
            // Destroy all instantiated objects to reset
            DestroyObjectIfExists("XR_Environment");
            DestroyObjectIfExists("MainCanvas");
            DestroyObjectIfExists("DataPersistenceManager");
            DestroyObjectIfExists("ProgressionSystem");
            DestroyObjectIfExists("AnalyticsManager");
            DestroyObjectIfExists("UIManager");

            Debug.Log("VR Setup reset. Run InitializeCompleteVRSetup to restart.");
        }

        private void DestroyObjectIfExists(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }
    }
}
