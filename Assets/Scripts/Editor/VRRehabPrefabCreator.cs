using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using UnityEditorInternal;

namespace VRRehab.EditorTools
{
    public class VRRehabPrefabCreator : EditorWindow
    {
        private const string PREFABS_PATH = "Assets/Prefabs";
        private const string SCENE_SETUP_PATH = "Assets/Scripts/SceneSetup";

        [MenuItem("VR Rehab/Create All Core Prefabs", false, 1)]
        public static void CreateAllCorePrefabs()
        {
            CreateXRTemplatePrefab();
            CreateCanvasPrefab();
            CreateBallPrefab();
            CreateTargetRingPrefab();
            CreateSpawnZonePrefab();
            CreateManagerPrefabs();

            AssetDatabase.Refresh();
            Debug.Log("All VR Rehab prefabs created successfully!");
        }

        [MenuItem("VR Rehab/Core Prefabs/XR Template", false, 10)]
        static void CreateXRTemplateMenu()
        {
            CreateXRTemplatePrefab();
        }

        [MenuItem("VR Rehab/Core Prefabs/Canvas", false, 11)]
        static void CreateCanvasMenu()
        {
            CreateCanvasPrefab();
        }

        [MenuItem("VR Rehab/Core Prefabs/Ball", false, 12)]
        static void CreateBallMenu()
        {
            CreateBallPrefab();
        }

        [MenuItem("VR Rehab/Core Prefabs/Target Ring", false, 13)]
        static void CreateTargetRingMenu()
        {
            CreateTargetRingPrefab();
        }

        [MenuItem("VR Rehab/Core Prefabs/Spawn Zone", false, 14)]
        static void CreateSpawnZoneMenu()
        {
            CreateSpawnZonePrefab();
        }

        [MenuItem("VR Rehab/Core Prefabs/Manager Prefabs", false, 15)]
        static void CreateManagersMenu()
        {
            CreateManagerPrefabs();
        }

        static void CreateXRTemplatePrefab()
        {
            // Create root GameObject
            GameObject xrTemplate = new GameObject("XR_Template");
            xrTemplate.transform.position = Vector3.zero;

            Debug.Log("Creating XR Template - This will work with or without XR Interaction Toolkit");

            // Create XR Origin (XR Rig)
            GameObject xrOrigin = new GameObject("XR Origin (XR Rig)");
            xrOrigin.transform.SetParent(xrTemplate.transform);

            // Add basic transform setup (XROrigin may not be available in all setups)
            // We'll use a simpler approach that works with most XR setups

            // Create Camera Offset
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOrigin.transform);
            cameraOffset.transform.localPosition = new Vector3(0, 1.1176f, 0);

            // Create Main Camera
            GameObject mainCamera = new GameObject("Main Camera");
            mainCamera.transform.SetParent(cameraOffset.transform);
            mainCamera.tag = "MainCamera";

            var camera = mainCamera.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.16f, 0.23f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.targetDisplay = 0;

            var audioListener = mainCamera.AddComponent<AudioListener>();

            // Add XR tracking if available
            try
            {
                var trackedPoseDriver = mainCamera.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                trackedPoseDriver.SetPoseSource(UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRDevice, UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.Head);
            }
            catch
            {
                Debug.Log("TrackedPoseDriver not available, camera will use basic setup");
            }



            // Create basic fallback components that work in any Unity setup
            CreateFallbackXRComponents(xrOrigin, cameraOffset);

            // Create XR Interaction Manager (optional)
            GameObject interactionManager = new GameObject("XR Interaction Manager");
            interactionManager.transform.SetParent(xrTemplate.transform);

            // Add XR Interaction Manager if available
            try
            {
                interactionManager.AddComponent<XRInteractionManager>();
                Debug.Log("XR Interaction Manager added");
            }
            catch
            {
                Debug.Log("XRInteractionManager not available, basic setup will be used");
            }

            // Create prefab
            EnsureDirectoryExists(PREFABS_PATH);
            string prefabPath = $"{PREFABS_PATH}/XR_Template.prefab";
            PrefabUtility.SaveAsPrefabAsset(xrTemplate, prefabPath);
            Object.DestroyImmediate(xrTemplate);

            Debug.Log($"XR_Template prefab created at {prefabPath}");
            Debug.Log("Note: Advanced XR features can be added later by installing XR Interaction Toolkit");
        }

        static void CreateFallbackXRComponents(GameObject xrOrigin, GameObject cameraOffset)
        {
            // Create basic controller placeholders
            GameObject leftController = new GameObject("Left Controller");
            leftController.transform.SetParent(cameraOffset.transform);
            leftController.transform.localPosition = new Vector3(-0.2f, 0, 0.1f);

            GameObject rightController = new GameObject("Right Controller");
            rightController.transform.SetParent(cameraOffset.transform);
            rightController.transform.localPosition = new Vector3(0.2f, 0, 0.1f);

            // Add basic colliders for interaction (can be replaced with XR components later)
            var leftCollider = leftController.AddComponent<SphereCollider>();
            leftCollider.radius = 0.05f;
            leftCollider.isTrigger = true;

            var rightCollider = rightController.AddComponent<SphereCollider>();
            rightCollider.radius = 0.05f;
            rightCollider.isTrigger = true;
        }

        static void CreateCanvasPrefab()
        {
            // Create Canvas GameObject
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null; // Will be set at runtime
            canvas.planeDistance = 100f;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(800, 600);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            var graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();

            // Add XR UI raycaster if available
            try
            {
                canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            catch
            {
                Debug.Log("TrackedDeviceGraphicRaycaster not available, using basic UI raycasting");
            }

            var rectTransform = canvasObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(2, 1);
            rectTransform.localPosition = new Vector3(0, 0, 0);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            // Create Main Panel
            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasObj.transform, false);

            var panelImage = mainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.125f, 0.125f, 0.125f, 0.9f);
            panelImage.raycastTarget = true;

            var panelRect = mainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 400);
            panelRect.anchoredPosition = new Vector2(0, 0.2f);

            // Create prefab
            EnsureDirectoryExists(PREFABS_PATH);
            string prefabPath = $"{PREFABS_PATH}/Canvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            Object.DestroyImmediate(canvasObj);

            Debug.Log($"Canvas prefab created at {prefabPath}");
        }

        static void CreateBallPrefab()
        {
            // Create Ball GameObject
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.tag = "Throwable";
            ball.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            ball.transform.position = new Vector3(0, 1.34f, 0);

            // Remove default collider and add our own
            Object.DestroyImmediate(ball.GetComponent<SphereCollider>());

            // Add required components
            var rigidbody = ball.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0.05f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var sphereCollider = ball.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.5f;
            sphereCollider.isTrigger = false;

            // Add XR Grab Interactable if available
            try
            {
                var grabInteractable = ball.AddComponent<XRGrabInteractable>();
                grabInteractable.interactionLayerMask = LayerMask.GetMask("Default");
                grabInteractable.colliders.Add(sphereCollider);
            }
            catch
            {
                Debug.Log("XRGrabInteractable not available, ball will use basic physics");
                // Add basic Rigidbody interaction
                var existingRigidbody = ball.GetComponent<Rigidbody>();
                if (existingRigidbody != null)
                {
                    existingRigidbody.useGravity = true;
                }
            }

            // Try to add HoverAndRelease script if it exists
            var hoverScript = ball.AddComponent(System.Type.GetType("HoverAndRelease, Assembly-CSharp"));
            if (hoverScript != null)
            {
                // Set default values if script exists
                var hoverAmplitudeField = hoverScript.GetType().GetField("hoverAmplitude");
                if (hoverAmplitudeField != null) hoverAmplitudeField.SetValue(hoverScript, 0.1f);

                var hoverDurationField = hoverScript.GetType().GetField("hoverDuration");
                if (hoverDurationField != null) hoverDurationField.SetValue(hoverScript, 1f);

                var rotationDurationField = hoverScript.GetType().GetField("rotationDuration");
                if (rotationDurationField != null) rotationDurationField.SetValue(hoverScript, 5f);
            }

            // Create material
            Material ballMaterial = new Material(Shader.Find("Standard"));
            ballMaterial.color = Color.red;
            ballMaterial.SetFloat("_Metallic", 0f);
            ballMaterial.SetFloat("_Glossiness", 0.5f);

            var renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = ballMaterial;
            }

            // Create prefab
            EnsureDirectoryExists(PREFABS_PATH);
            string prefabPath = $"{PREFABS_PATH}/Ball.prefab";
            PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
            Object.DestroyImmediate(ball);

            Debug.Log($"Ball prefab created at {prefabPath}");
        }

        static void CreateTargetRingPrefab()
        {
            // Create Target Ring GameObject
            GameObject targetRing = new GameObject("Target Ring");
            targetRing.transform.position = new Vector3(0, 1.62f, 1.3f);
            // Try to use TargetRing tag, or create it if it doesn't exist
            if (IsTagDefined("TargetRing"))
            {
                targetRing.tag = "TargetRing";
            }
            else
            {
                AddTag("TargetRing");
                targetRing.tag = "TargetRing";
                Debug.Log("Created TargetRing tag automatically");
            }

            // Also ensure UIPanel tag exists for UIManager
            if (!IsTagDefined("UIPanel"))
            {
                AddTag("UIPanel");
                Debug.Log("Created UIPanel tag automatically");
            }

            // Try to use existing ring mesh if available
            Mesh ringMesh = null;
            string[] ringFiles = Directory.GetFiles("Assets", "Ring.fbx", SearchOption.AllDirectories);
            if (ringFiles.Length > 0)
            {
                string ringPath = ringFiles[0].Replace("\\", "/");
                GameObject tempRing = AssetDatabase.LoadAssetAtPath<GameObject>(ringPath);
                if (tempRing != null)
                {
                    var meshFilter = tempRing.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        ringMesh = meshFilter.sharedMesh;
                    }
                }
            }

            // If no ring mesh found, create a simple torus-like shape using primitives
            if (ringMesh == null)
            {
                // Create a cylinder as a simple ring representation
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.transform.SetParent(targetRing.transform);
                cylinder.transform.localPosition = Vector3.zero;
                cylinder.transform.localScale = new Vector3(2f, 0.1f, 2f);

                // Remove the cylinder's collider since we'll add our own
                Object.DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
            }
            else
            {
                // Use the imported ring mesh
                var meshFilter = targetRing.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = ringMesh;

                var meshRenderer = targetRing.AddComponent<MeshRenderer>();
                Material ringMaterial = new Material(Shader.Find("Standard"));
                ringMaterial.color = Color.yellow;
                ringMaterial.SetFloat("_Metallic", 0.8f);
                ringMaterial.SetFloat("_Glossiness", 0.8f);
                meshRenderer.material = ringMaterial;
            }

            // Add TargetRing script if it exists
            var targetRingScript = targetRing.AddComponent(System.Type.GetType("TargetRing, Assembly-CSharp"));

            // Add collider
            var sphereCollider = targetRing.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 1.0f;
            sphereCollider.center = new Vector3(0, 0.33f, 0.16f);

            // Create prefab
            EnsureDirectoryExists(PREFABS_PATH);
            string prefabPath = $"{PREFABS_PATH}/Target Ring.prefab";
            PrefabUtility.SaveAsPrefabAsset(targetRing, prefabPath);
            Object.DestroyImmediate(targetRing);

            Debug.Log($"Target Ring prefab created at {prefabPath}");
        }

        static void CreateSpawnZonePrefab()
        {
            // Create Spawn Zone GameObject
            GameObject spawnZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnZone.name = "spawnzone";
            spawnZone.transform.localScale = new Vector3(2, 0.1f, 2);
            spawnZone.transform.position = Vector3.zero;

            // Remove default collider and add trigger collider
            Object.DestroyImmediate(spawnZone.GetComponent<BoxCollider>());

            var boxCollider = spawnZone.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2, 0.1f, 2);
            boxCollider.isTrigger = true;

            // Add BallRespawnZone script if it exists
            var respawnScript = spawnZone.AddComponent(System.Type.GetType("BallRespawnZone, Assembly-CSharp"));

            // Add spawn point child
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnZone.transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1, 0);

            // Make it semi-transparent
            var renderer = spawnZone.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material zoneMaterial = new Material(Shader.Find("Standard"));
                zoneMaterial.color = new Color(0, 1, 0, 0.3f);
                zoneMaterial.SetFloat("_Mode", 3); // Transparent mode
                zoneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                zoneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                zoneMaterial.SetInt("_ZWrite", 0);
                zoneMaterial.DisableKeyword("_ALPHATEST_ON");
                zoneMaterial.EnableKeyword("_ALPHABLEND_ON");
                zoneMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                zoneMaterial.renderQueue = 3000;
                renderer.material = zoneMaterial;
            }

            // Create prefab
            EnsureDirectoryExists(PREFABS_PATH);
            string prefabPath = $"{PREFABS_PATH}/spawnzone.prefab";
            PrefabUtility.SaveAsPrefabAsset(spawnZone, prefabPath);
            Object.DestroyImmediate(spawnZone);

            Debug.Log($"Spawn Zone prefab created at {prefabPath}");
        }

        static void CreateManagerPrefabs()
        {
            // Create Progression Manager
            GameObject progressionManager = new GameObject("ProgressionManager");
            var progressionScript = progressionManager.AddComponent(System.Type.GetType("ProgressionSystem, Assembly-CSharp"));
            if (progressionScript != null)
            {
                // Set default values
                var minSuccessRateField = progressionScript.GetType().GetField("minSuccessRateForAdvancement");
                if (minSuccessRateField != null) minSuccessRateField.SetValue(progressionScript, 0.7f);

                var minAttemptsField = progressionScript.GetType().GetField("minAttemptsForAdvancement");
                if (minAttemptsField != null) minAttemptsField.SetValue(progressionScript, 3);

                var masteryField = progressionScript.GetType().GetField("masteryThreshold");
                if (masteryField != null) masteryField.SetValue(progressionScript, 0.85f);
            }

            string progressionPath = $"{PREFABS_PATH}/ProgressionManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(progressionManager, progressionPath);
            Object.DestroyImmediate(progressionManager);
            Debug.Log($"ProgressionManager prefab created at {progressionPath}");

            // Create Data Manager
            GameObject dataManager = new GameObject("DataManager");
            var dataScript = dataManager.AddComponent(System.Type.GetType("DataPersistenceManager, Assembly-CSharp"));
            if (dataScript != null)
            {
                // Set default values
                var autoSaveField = dataScript.GetType().GetField("autoSave");
                if (autoSaveField != null) autoSaveField.SetValue(dataScript, true);

                var intervalField = dataScript.GetType().GetField("autoSaveInterval");
                if (intervalField != null) intervalField.SetValue(dataScript, 30f);
            }

            string dataPath = $"{PREFABS_PATH}/DataManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(dataManager, dataPath);
            Object.DestroyImmediate(dataManager);
            Debug.Log($"DataManager prefab created at {dataPath}");

            // Create Analytics Manager
            GameObject analyticsManager = new GameObject("AnalyticsManager");
            var analyticsScript = analyticsManager.AddComponent(System.Type.GetType("PerformanceAnalytics, Assembly-CSharp"));
            if (analyticsScript != null)
            {
                // Set default values
                var realTimeField = analyticsScript.GetType().GetField("enableRealTimeTracking");
                if (realTimeField != null) realTimeField.SetValue(analyticsScript, true);

                var intervalField = analyticsScript.GetType().GetField("dataCollectionInterval");
                if (intervalField != null) intervalField.SetValue(analyticsScript, 1f);
            }

            string analyticsPath = $"{PREFABS_PATH}/AnalyticsManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(analyticsManager, analyticsPath);
            Object.DestroyImmediate(analyticsManager);
            Debug.Log($"AnalyticsManager prefab created at {analyticsPath}");
        }

        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static bool IsTagDefined(string tagName)
        {
            try
            {
                // Try to create a temporary object and set the tag
                GameObject temp = new GameObject("temp");
                temp.tag = tagName;
                Object.DestroyImmediate(temp);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void AddTag(string tagName)
        {
            // Get the tag manager asset
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    return; // Tag already exists
                }
            }

            // Add new tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;

            // Save changes
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Tag '{tagName}' added to TagManager");
        }

        [MenuItem("VR Rehab/Tools/Create Scene Setup Objects", false, 50)]
        static void CreateSceneSetupObjects()
        {
            // Create a new scene with all setup objects
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create setup objects
            GameObject completeSetup = new GameObject("CompleteVRSetup");
            var setupScript = completeSetup.AddComponent(System.Type.GetType("CompleteVRSetup, Assembly-CSharp"));
            if (setupScript != null)
            {
                // Set scene type to ThrowingExercise as default
                var sceneTypeField = setupScript.GetType().GetField("sceneType");
                if (sceneTypeField != null)
                {
                    sceneTypeField.SetValue(setupScript, 2); // ThrowingExercise enum value
                }
            }

            // Save the scene
            EnsureDirectoryExists("Assets/Scenes");
            string scenePath = "Assets/Scenes/VRRehab_TestScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"Test scene created at {scenePath}");
        }

        [MenuItem("VR Rehab/Tools/Validate Prefabs", false, 51)]
        public static void ValidatePrefabs()
        {
            string[] prefabPaths = {
                $"{PREFABS_PATH}/XR_Template.prefab",
                $"{PREFABS_PATH}/Canvas.prefab",
                $"{PREFABS_PATH}/Ball.prefab",
                $"{PREFABS_PATH}/Target Ring.prefab",
                $"{PREFABS_PATH}/spawnzone.prefab",
                $"{PREFABS_PATH}/ProgressionManager.prefab",
                $"{PREFABS_PATH}/DataManager.prefab",
                $"{PREFABS_PATH}/AnalyticsManager.prefab"
            };

            int validCount = 0;
            foreach (string path in prefabPaths)
            {
                if (File.Exists(path))
                {
                    validCount++;
                    Debug.Log($"✓ {Path.GetFileName(path)} found");
                }
                else
                {
                    Debug.Log($"✗ {Path.GetFileName(path)} missing");
                }
            }

            Debug.Log($"Prefabs validation complete: {validCount}/{prefabPaths.Length} found");
        }
    }
}
