using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace VRRehab.EditorTools
{
    public class VRRehabSceneSetupWindow : EditorWindow
    {
        private const string PREFABS_PATH = "Assets/Prefabs";
        private const string SCENE_SETUP_PATH = "Assets/Scripts/SceneSetup";

        // Prefab references
        private GameObject xrTemplatePrefab;
        private GameObject canvasPrefab;
        private GameObject ballPrefab;
        private GameObject targetRingPrefab;
        private GameObject spawnZonePrefab;
        private GameObject progressionManagerPrefab;
        private GameObject dataManagerPrefab;
        private GameObject analyticsManagerPrefab;

        // Scene setup options
        private bool createXRTemplate = true;
        private bool createCanvas = true;
        private bool createBallSpawner = true;
        private bool createTargetRings = true;
        private bool createManagers = true;
        private int numberOfRings = 5;
        private float ringSpacing = 2f;
        private Vector3 spawnPosition = Vector3.zero;
        private string sceneName = "VRRehab_Scene";

        // Exercise type selection
        private string[] exerciseTypes = { "MainMenu", "ThrowingExercise", "BridgeExercise", "SquatExercise" };
        private int selectedExerciseType = 1; // Default to ThrowingExercise

        [MenuItem("VR Rehab/Scene Setup Window", false, 0)]
        public static void ShowWindow()
        {
            VRRehabSceneSetupWindow window = GetWindow<VRRehabSceneSetupWindow>();
            window.titleContent = new GUIContent("VR Rehab Scene Setup");
            window.minSize = new Vector2(400, 600);
        }

        void OnGUI()
        {
            GUILayout.Label("VR Rehab Scene Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Prefab Assignment Section
            GUILayout.Label("Prefab Assignments", EditorStyles.boldLabel);
            xrTemplatePrefab = (GameObject)EditorGUILayout.ObjectField("XR Template", xrTemplatePrefab, typeof(GameObject), false);
            canvasPrefab = (GameObject)EditorGUILayout.ObjectField("Canvas", canvasPrefab, typeof(GameObject), false);
            ballPrefab = (GameObject)EditorGUILayout.ObjectField("Ball", ballPrefab, typeof(GameObject), false);
            targetRingPrefab = (GameObject)EditorGUILayout.ObjectField("Target Ring", targetRingPrefab, typeof(GameObject), false);
            spawnZonePrefab = (GameObject)EditorGUILayout.ObjectField("Spawn Zone", spawnZonePrefab, typeof(GameObject), false);
            progressionManagerPrefab = (GameObject)EditorGUILayout.ObjectField("Progression Manager", progressionManagerPrefab, typeof(GameObject), false);
            dataManagerPrefab = (GameObject)EditorGUILayout.ObjectField("Data Manager", dataManagerPrefab, typeof(GameObject), false);
            analyticsManagerPrefab = (GameObject)EditorGUILayout.ObjectField("Analytics Manager", analyticsManagerPrefab, typeof(GameObject), false);

            GUILayout.Space(20);

            // Auto-load prefabs button
            if (GUILayout.Button("Auto-Load Existing Prefabs"))
            {
                AutoLoadExistingPrefabs();
            }

            GUILayout.Space(20);

            // Scene Configuration
            GUILayout.Label("Scene Configuration", EditorStyles.boldLabel);
            sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
            selectedExerciseType = EditorGUILayout.Popup("Exercise Type", selectedExerciseType, exerciseTypes);

            // Exercise-specific settings
            if (selectedExerciseType == 1) // ThrowingExercise
            {
                createBallSpawner = EditorGUILayout.Toggle("Create Ball Spawner", createBallSpawner);
                createTargetRings = EditorGUILayout.Toggle("Create Target Rings", createTargetRings);

                if (createTargetRings)
                {
                    numberOfRings = EditorGUILayout.IntSlider("Number of Rings", numberOfRings, 1, 10);
                    ringSpacing = EditorGUILayout.Slider("Ring Spacing", ringSpacing, 1f, 5f);
                }

                if (createBallSpawner)
                {
                    spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);
                }
            }
            else if (selectedExerciseType == 2) // BridgeExercise
            {
                EditorGUILayout.HelpBox("Bridge exercise will create ground plane and bridge builder automatically.", MessageType.Info);
            }

            GUILayout.Space(20);

            // Create Options
            GUILayout.Label("Create Options", EditorStyles.boldLabel);
            createXRTemplate = EditorGUILayout.Toggle("XR Template", createXRTemplate);
            createCanvas = EditorGUILayout.Toggle("UI Canvas", createCanvas);
            createManagers = EditorGUILayout.Toggle("Manager Objects", createManagers);

            GUILayout.Space(20);

            // Action Buttons
            if (GUILayout.Button("Create New Scene", GUILayout.Height(40)))
            {
                CreateCompleteScene();
            }

            if (GUILayout.Button("Add Setup to Current Scene", GUILayout.Height(30)))
            {
                AddSetupToCurrentScene();
            }

            if (GUILayout.Button("Validate Current Scene", GUILayout.Height(30)))
            {
                ValidateCurrentScene();
            }

            GUILayout.Space(20);

            // Quick Actions
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Create All Missing Prefabs"))
            {
                VRRehabPrefabCreator.CreateAllCorePrefabs();
                AutoLoadExistingPrefabs();
            }

            if (GUILayout.Button("Open Prefab Creation Guide"))
            {
                string guidePath = "Assets/Prefab_Creation_Guide.md";
                if (File.Exists(guidePath))
                {
                    EditorUtility.OpenWithDefaultApp(guidePath);
                }
                else
                {
                    Debug.LogWarning("Prefab creation guide not found at: " + guidePath);
                }
            }
        }

        void AutoLoadExistingPrefabs()
        {
            // Try to load existing prefabs automatically
            string[] prefabNames = {
                "XR_Template", "Canvas", "Ball", "Target Ring", "spawnzone",
                "ProgressionManager", "DataManager", "AnalyticsManager"
            };

            foreach (string prefabName in prefabNames)
            {
                string[] guids = AssetDatabase.FindAssets(prefabName + " t:prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    // Assign to the appropriate field
                    switch (prefabName)
                    {
                        case "XR_Template": xrTemplatePrefab = prefab; break;
                        case "Canvas": canvasPrefab = prefab; break;
                        case "Ball": ballPrefab = prefab; break;
                        case "Target Ring": targetRingPrefab = prefab; break;
                        case "spawnzone": spawnZonePrefab = prefab; break;
                        case "ProgressionManager": progressionManagerPrefab = prefab; break;
                        case "DataManager": dataManagerPrefab = prefab; break;
                        case "AnalyticsManager": analyticsManagerPrefab = prefab; break;
                    }
                }
            }

            Debug.Log("Auto-loaded existing prefabs");
        }

        void CreateCompleteScene()
        {
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create the setup objects
            AddSetupToCurrentScene();

            // Save the scene
            EnsureDirectoryExists("Assets/Scenes");
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"Complete VR Rehab scene created: {scenePath}");
        }

        void AddSetupToCurrentScene()
        {
            // Create CompleteVRSetup object
            GameObject setupObj = new GameObject("CompleteVRSetup");
            var setupScript = setupObj.AddComponent(System.Type.GetType("CompleteVRSetup, Assembly-CSharp"));

            if (setupScript != null)
            {
                // Set exercise type
                var sceneTypeField = setupScript.GetType().GetField("sceneType");
                if (sceneTypeField != null)
                {
                    sceneTypeField.SetValue(setupScript, selectedExerciseType);
                }

                // Assign prefabs
                AssignPrefabsToSetupScript(setupScript);
            }

            // Create additional setup objects based on exercise type
            if (selectedExerciseType == 1) // ThrowingExercise
            {
                CreateThrowingExerciseSetup();
            }
            else if (selectedExerciseType == 2) // BridgeExercise
            {
                CreateBridgeExerciseSetup();
            }

            Debug.Log("Setup objects added to current scene");
        }

        void AssignPrefabsToSetupScript(System.Object setupScript)
        {
            // Assign XR Template
            if (xrTemplatePrefab != null)
            {
                var xrField = setupScript.GetType().GetField("xrTemplatePrefab");
                if (xrField != null) xrField.SetValue(setupScript, xrTemplatePrefab);
            }

            // Assign Canvas
            if (canvasPrefab != null)
            {
                var canvasField = setupScript.GetType().GetField("canvasPrefab");
                if (canvasField != null) canvasField.SetValue(setupScript, canvasPrefab);
            }

            // Assign Ball
            if (ballPrefab != null)
            {
                var ballField = setupScript.GetType().GetField("ballPrefab");
                if (ballField != null) ballField.SetValue(setupScript, ballPrefab);
            }

            // Assign Target Ring
            if (targetRingPrefab != null)
            {
                var ringField = setupScript.GetType().GetField("targetRingPrefab");
                if (ringField != null) ringField.SetValue(setupScript, targetRingPrefab);
            }

            // Assign Spawn Zone
            if (spawnZonePrefab != null)
            {
                var zoneField = setupScript.GetType().GetField("spawnZonePrefab");
                if (zoneField != null) zoneField.SetValue(setupScript, spawnZonePrefab);
            }
        }

        void CreateThrowingExerciseSetup()
        {
            if (createBallSpawner && spawnZonePrefab != null)
            {
                GameObject spawnZone = PrefabUtility.InstantiatePrefab(spawnZonePrefab) as GameObject;
                if (spawnZone != null)
                {
                    spawnZone.transform.position = spawnPosition;
                    spawnZone.name = "BallSpawnZone";
                }
            }

            if (createTargetRings && targetRingPrefab != null)
            {
                for (int i = 0; i < numberOfRings; i++)
                {
                    GameObject ring = PrefabUtility.InstantiatePrefab(targetRingPrefab) as GameObject;
                    if (ring != null)
                    {
                        Vector3 position = new Vector3(
                            (i - (numberOfRings - 1) / 2f) * ringSpacing,
                            1.5f,
                            3f
                        );
                        ring.transform.position = position;
                        ring.name = $"TargetRing_{i + 1}";
                    }
                }
            }
        }

        void CreateBridgeExerciseSetup()
        {
            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Add material if available
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material groundMaterial = new Material(Shader.Find("Standard"));
                groundMaterial.color = new Color(0.8f, 0.8f, 0.8f);
                renderer.material = groundMaterial;
            }
        }

        void ValidateCurrentScene()
        {
            int issues = 0;

            // Check for required components
            if (GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                Debug.LogWarning("❌ XR Interaction Manager not found");
                issues++;
            }
            else
            {
                Debug.Log("✅ XR Interaction Manager found");
            }

            if (GameObject.FindObjectOfType<Canvas>() == null)
            {
                Debug.LogWarning("❌ Canvas not found");
                issues++;
            }
            else
            {
                Debug.Log("✅ Canvas found");
            }

            if (GameObject.FindGameObjectWithTag("Throwable") == null)
            {
                Debug.LogWarning("❌ Throwable object not found");
                issues++;
            }
            else
            {
                Debug.Log("✅ Throwable object found");
            }

            if (GameObject.FindGameObjectsWithTag("TargetRing").Length == 0)
            {
                Debug.LogWarning("❌ Target rings not found");
                issues++;
            }
            else
            {
                Debug.Log($"✅ {GameObject.FindGameObjectsWithTag("TargetRing").Length} target rings found");
            }

            if (issues == 0)
            {
                Debug.Log("🎉 Scene validation passed! All required components found.");
            }
            else
            {
                Debug.Log($"⚠️ Scene validation found {issues} issues. Check console for details.");
            }
        }

        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
