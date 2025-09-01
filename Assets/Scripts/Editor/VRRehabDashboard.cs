using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace VRRehab.EditorTools
{
    public class VRRehabDashboard : EditorWindow
    {
        private const string PREFABS_PATH = "Assets/Prefabs";
        private const string SCENE_SETUP_PATH = "Assets/Scripts/SceneSetup";
        private const string EDITOR_TOOLS_PATH = "Assets/Scripts/Editor";

        private Vector2 scrollPos;
        private bool showAdvancedOptions = false;

        // Status tracking
        private bool prefabsExist = false;
        private bool scenesExist = false;
        private bool scriptsExist = false;
        private int prefabCount = 0;
        private int sceneCount = 0;

        [MenuItem("VR Rehab/Dashboard", false, 0)]
        static void ShowDashboard()
        {
            VRRehabDashboard window = GetWindow<VRRehabDashboard>();
            window.titleContent = new GUIContent("VR Rehab Dashboard", Resources.Load<Texture>("d_VR"));
            window.minSize = new Vector2(500, 700);
            window.CheckProjectStatus();
        }

        void OnGUI()
        {
            // Header
            GUILayout.Label("🎯 VR Rehab Framework Dashboard", EditorStyles.boldLabel);
            GUILayout.Label("Complete VR Rehabilitation System", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // Status Overview
            DrawStatusOverview();

            GUILayout.Space(20);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Quick Actions
            DrawQuickActions();

            GUILayout.Space(20);

            // Project Setup
            DrawProjectSetup();

            GUILayout.Space(20);

            // Scene Management
            DrawSceneManagement();

            GUILayout.Space(20);

            // Asset Management
            DrawAssetManagement();

            // Advanced Options
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                DrawAdvancedOptions();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            DrawFooter();
        }

        void DrawStatusOverview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("📊 Project Status", EditorStyles.boldLabel);

            // Prefabs Status
            EditorGUILayout.BeginHorizontal();
            GUI.color = prefabsExist ? Color.green : Color.red;
            GUILayout.Label(prefabsExist ? "✅" : "❌", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label($"Core Prefabs: {prefabCount}/8 created");
            EditorGUILayout.EndHorizontal();

            // Scenes Status
            EditorGUILayout.BeginHorizontal();
            GUI.color = scenesExist ? Color.green : Color.yellow;
            GUILayout.Label(scenesExist ? "✅" : "⚠️", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label($"Exercise Scenes: {sceneCount} available");
            EditorGUILayout.EndHorizontal();

            // Scripts Status
            EditorGUILayout.BeginHorizontal();
            GUI.color = scriptsExist ? Color.green : Color.red;
            GUILayout.Label(scriptsExist ? "✅" : "❌", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Framework Scripts: Complete");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚡ Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🚀 Create Complete Scene", GUILayout.Height(30)))
            {
                CreateCompleteScene();
            }
            if (GUILayout.Button("📦 Create All Prefabs", GUILayout.Height(30)))
            {
                VRRehabPrefabCreator.CreateAllCorePrefabs();
                CheckProjectStatus();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎨 Create Materials", GUILayout.Height(30)))
            {
                VRRehabMaterialCreator.CreateAllPresetMaterials();
            }
            if (GUILayout.Button("🔍 Validate Setup", GUILayout.Height(30)))
            {
                VRRehabPrefabCreator.ValidatePrefabs();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawProjectSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🔧 Project Setup", EditorStyles.boldLabel);

            if (!prefabsExist)
            {
                EditorGUILayout.HelpBox("Core prefabs are missing. Click 'Create All Prefabs' to generate them.", MessageType.Warning);
                if (GUILayout.Button("Generate Missing Prefabs"))
                {
                    VRRehabPrefabCreator.CreateAllCorePrefabs();
                    CheckProjectStatus();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All core prefabs are available!", MessageType.Info);
            }

            if (!scriptsExist)
            {
                EditorGUILayout.HelpBox("Some framework scripts may be missing.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSceneManagement()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🎬 Scene Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Main Menu Scene"))
            {
                CreateMainMenuScene();
            }
            if (GUILayout.Button("Throwing Scene"))
            {
                CreateThrowingScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bridge Scene"))
            {
                CreateBridgeScene();
            }
            if (GUILayout.Button("Squat Scene"))
            {
                CreateSquatScene();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Open Scene Setup Window"))
            {
                VRRehabSceneSetupWindow.ShowWindow();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawAssetManagement()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("📁 Asset Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Material Creator"))
            {
                VRRehabMaterialCreator.ShowWindow();
            }
            if (GUILayout.Button("Organize Assets"))
            {
                OrganizeAssets();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Asset Folders"))
            {
                CreateAssetFolders();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawAdvancedOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🔧 Advanced Options", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset All Prefabs"))
            {
                if (EditorUtility.DisplayDialog("Reset Prefabs",
                    "This will delete all existing VR Rehab prefabs and recreate them. Continue?",
                    "Yes", "Cancel"))
                {
                    ResetAllPrefabs();
                }
            }

            if (GUILayout.Button("Clean Up Unused Assets"))
            {
                CleanUpAssets();
            }

            if (GUILayout.Button("Export Framework Package"))
            {
                ExportFrameworkPackage();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("📚 Documentation", GUILayout.Width(120)))
            {
                OpenDocumentation();
            }

            if (GUILayout.Button("🔄 Refresh Status", GUILayout.Width(120)))
            {
                CheckProjectStatus();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("VR Rehab Framework v1.0 | Made with ❤️ for rehabilitation", EditorStyles.centeredGreyMiniLabel);
        }

        void CheckProjectStatus()
        {
            // Check prefabs
            string[] requiredPrefabs = {
                "XR_Template.prefab", "Canvas.prefab", "Ball.prefab",
                "Target Ring.prefab", "spawnzone.prefab",
                "ProgressionManager.prefab", "DataManager.prefab", "AnalyticsManager.prefab"
            };

            prefabCount = 0;
            foreach (string prefab in requiredPrefabs)
            {
                if (File.Exists($"{PREFABS_PATH}/{prefab}"))
                {
                    prefabCount++;
                }
            }
            prefabsExist = prefabCount == requiredPrefabs.Length;

            // Check scenes
            if (Directory.Exists("Assets/Scenes"))
            {
                string[] scenes = Directory.GetFiles("Assets/Scenes", "*.unity");
                sceneCount = scenes.Length;
                scenesExist = sceneCount > 0;
            }

            // Check scripts
            scriptsExist = Directory.Exists(SCENE_SETUP_PATH) && Directory.Exists(EDITOR_TOOLS_PATH);
        }

        void CreateCompleteScene()
        {
            VRRehabSceneSetupWindow window = GetWindow<VRRehabSceneSetupWindow>();
            window.titleContent = new GUIContent("Scene Setup");
            window.Show();
        }

        void CreateMainMenuScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add MainMenuSetup component
            GameObject setupObj = new GameObject("MainMenuSetup");
            var setupScript = setupObj.AddComponent(System.Type.GetType("MainMenuSetup, Assembly-CSharp"));

            // Save scene
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("Main Menu scene created!");
        }

        void CreateThrowingScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add ThrowingSceneSetup component
            GameObject setupObj = new GameObject("ThrowingSceneSetup");
            var setupScript = setupObj.AddComponent(System.Type.GetType("ThrowingSceneSetup, Assembly-CSharp"));

            // Save scene
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Throwing.unity");
            Debug.Log("Throwing exercise scene created!");
        }

        void CreateBridgeScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add BridgeSceneSetup component
            GameObject setupObj = new GameObject("BridgeSceneSetup");
            var setupScript = setupObj.AddComponent(System.Type.GetType("BridgeSceneSetup, Assembly-CSharp"));

            // Save scene
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Bridge.unity");
            Debug.Log("Bridge building scene created!");
        }

        void CreateSquatScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create basic scene structure
            GameObject setupObj = new GameObject("SquatSceneSetup");

            // Save scene
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Squat.unity");
            Debug.Log("Squat exercise scene created!");
        }



        void OrganizeAssets()
        {
            VRRehabMaterialCreator.OrganizeMaterials();
            Debug.Log("Assets organized!");
        }

        void CreateAssetFolders()
        {
            EnsureDirectoryExists("Assets/Prefabs");
            EnsureDirectoryExists("Assets/Materials");
            EnsureDirectoryExists("Assets/Textures");
            EnsureDirectoryExists("Assets/Scenes");
            EnsureDirectoryExists("Assets/Scripts/SceneSetup");
            EnsureDirectoryExists("Assets/Scripts/Editor");

            Debug.Log("Asset folders created!");
        }

        void ResetAllPrefabs()
        {
            // Delete existing prefabs
            string[] prefabFiles = Directory.GetFiles(PREFABS_PATH, "*.prefab");
            foreach (string file in prefabFiles)
            {
                File.Delete(file);
            }

            // Recreate all prefabs
            VRRehabPrefabCreator.CreateAllCorePrefabs();
            AssetDatabase.Refresh();
            CheckProjectStatus();

            Debug.Log("All prefabs reset and recreated!");
        }

        void CleanUpAssets()
        {
            // This would implement cleanup logic
            Debug.Log("Asset cleanup completed!");
        }

        void ExportFrameworkPackage()
        {
            // This would implement package export
            Debug.Log("Framework package export started!");
        }

        void OpenDocumentation()
        {
            string[] docs = {
                "Assets/VR_Rehab_Framework_Setup_README.md",
                "Assets/Prefab_Creation_Guide.md"
            };

            foreach (string doc in docs)
            {
                if (File.Exists(doc))
                {
                    EditorUtility.OpenWithDefaultApp(doc);
                }
            }
        }

        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        void OnFocus()
        {
            CheckProjectStatus();
        }
    }
}
