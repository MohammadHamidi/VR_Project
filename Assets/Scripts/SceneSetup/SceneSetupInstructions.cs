// Scene Setup Instructions for VR Rehab Framework
// ================================================

/*
This file contains instructions for setting up scenes in the VR Rehab Framework.

1. MAIN MENU SCENE SETUP
=========================

Create a new scene called "MainMenu" and add these components:

a) Add to Hierarchy:
   - Create Empty GameObject "SceneSetup"
   - Add Component: MainMenuSetup
   - Assign prefabs in inspector:
     * XR Template Prefab: XR_Template
     * Canvas Prefab: Canvas

b) Add to Hierarchy:
   - Create Empty GameObject "SceneLoader"
   - Add Component: SceneLoader
   - Set showLoadingScreen = true

2. THROWING EXERCISE SCENE SETUP
=================================

Create a new scene called "Throwing" and add these components:

a) Add to Hierarchy:
   - Create Empty GameObject "SceneSetup"
   - Add Component: ThrowingSceneSetup
   - Assign prefabs in inspector:
     * XR Template Prefab: XR_Template
     * Canvas Prefab: Canvas
     * Ball Prefab: Ball
     * Target Ring Prefab: Target Ring
     * Spawn Zone Prefab: spawnzone

b) Configure settings:
   - Number of Rings: 5
   - Ring Spacing: 2.0
   - Ring Height: 1.5
   - Ring Distance: 3.0

3. BRIDGE BUILDING SCENE SETUP
===============================

Create a new scene called "Bridge" and add these components:

a) Add to Hierarchy:
   - Create Empty GameObject "SceneSetup"
   - Add Component: BridgeSceneSetup
   - Assign prefabs in inspector:
     * XR Template Prefab: XR_Template
     * Canvas Prefab: Canvas

b) Create Bridge Configuration:
   - Create new BridgeConfiguration ScriptableObject
   - Set properties:
     * Number of Planks: 8
     * Total Bridge Length: 12
     * Plank Width: 1.5
     * Create Platforms: true

c) Add to Hierarchy:
   - Create Empty GameObject "Ground"
   - Add Component: Mesh Filter (Plane)
   - Add Component: Mesh Renderer
   - Scale: (10, 1, 10)
   - Position: (0, 0, 0)

4. GENERAL SCENE SETUP
=======================

For any scene, you can also use the PrefabInstantiator:

a) Add to Hierarchy:
   - Create Empty GameObject "PrefabInstantiator"
   - Add Component: PrefabInstantiator
   - Configure in inspector:
     * Set Instantiate On Awake: true
     * Assign all core system prefabs
     * Add custom prefabs to the list

5. REQUIRED PREFABS
===================

Make sure these prefabs exist in Assets/Prefabs/:

- XR_Template.prefab (VR setup)
- Canvas.prefab (UI canvas)
- Ball.prefab (throwing ball)
- Target Ring.prefab (target for throwing)
- spawnzone.prefab (ball spawn zone)

6. MANAGER PREFABS
===================

Create these manager prefabs:

a) ProgressionManager.prefab:
   - Add Component: ProgressionSystem

b) DataManager.prefab:
   - Add Component: DataPersistenceManager

c) AnalyticsManager.prefab:
   - Add Component: PerformanceAnalytics

7. SCENE LOADING
================

Add scene loading between exercises:

- From MainMenu to exercises
- From exercises back to MainMenu
- Between different exercise types

Use the SceneLoader component for smooth transitions with loading screens.

8. TESTING
==========

Test each scene by:
1. Opening the scene in Unity
2. Entering Play mode
3. Verifying VR components load
4. Testing exercise mechanics
5. Checking UI interactions

9. BUILD SETTINGS
=================

Add all scenes to Build Settings:
- MainMenu (index 0)
- Throwing
- Bridge
- Squat

Set MainMenu as the first scene to load.
*/

using UnityEngine;
using VRRehab.UI;
using VRRehab.DataPersistence;
using VRRehab.Analytics;

namespace VRRehab.SceneSetup
{
    public class SceneSetupInstructions : MonoBehaviour
    {
        // This script serves as documentation
        // Attach it to any GameObject in your scenes for reference

        [Header("Scene Setup Status")]
        [SerializeField] private bool mainMenuSetup = false;
        [SerializeField] private bool throwingSceneSetup = false;
        [SerializeField] private bool bridgeSceneSetup = false;
        [SerializeField] private bool vrEnvironmentSetup = false;
        [SerializeField] private bool uiSystemSetup = false;

        [Header("Required Components")]
        [SerializeField] private GameObject xrTemplate;
        [SerializeField] private GameObject canvas;
        [SerializeField] private GameObject ball;
        [SerializeField] private GameObject targetRing;
        [SerializeField] private GameObject spawnZone;

        void Awake()
        {
            CheckSetupStatus();
        }

        private void CheckSetupStatus()
        {
            vrEnvironmentSetup = GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null;
            uiSystemSetup = GameObject.FindObjectOfType<Canvas>() != null;
            mainMenuSetup = GameObject.FindObjectOfType<MainMenuSetup>() != null;
            throwingSceneSetup = GameObject.FindObjectOfType<ThrowingSceneSetup>() != null;
            bridgeSceneSetup = GameObject.FindObjectOfType<BridgeSceneSetup>() != null;

            Debug.Log($"Scene Setup Status:\n" +
                     $"VR Environment: {vrEnvironmentSetup}\n" +
                     $"UI System: {uiSystemSetup}\n" +
                     $"Main Menu: {mainMenuSetup}\n" +
                     $"Throwing Scene: {throwingSceneSetup}\n" +
                     $"Bridge Scene: {bridgeSceneSetup}");
        }

        [ContextMenu("Verify Scene Setup")]
        public void VerifySceneSetup()
        {
            CheckSetupStatus();
        }

        [ContextMenu("Generate Setup Report")]
        public void GenerateSetupReport()
        {
            string report = "VR Rehab Scene Setup Report\n";
            report += "=============================\n\n";

            report += $"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n\n";

            report += "REQUIRED COMPONENTS:\n";
            report += $"- XR Interaction Manager: {(GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null ? "✓" : "✗")}\n";
            report += $"- Canvas: {(GameObject.FindObjectOfType<Canvas>() != null ? "✓" : "✗")}\n";
            report += $"- UIManager: {(GameObject.FindObjectOfType<UIManager>() != null ? "✓" : "✗")}\n";
            report += $"- DataPersistenceManager: {(GameObject.FindObjectOfType<DataPersistenceManager>() != null ? "✓" : "✗")}\n";
            report += $"- ProgressionSystem: {(GameObject.FindObjectOfType<ProgressionSystem>() != null ? "✓" : "✗")}\n";
            report += $"- PerformanceAnalytics: {(GameObject.FindObjectOfType<PerformanceAnalytics>() != null ? "✓" : "✗")}\n";

            report += "\nEXERCISE COMPONENTS:\n";
            report += $"- Ball: {(GameObject.FindGameObjectWithTag("Throwable") != null ? "✓" : "✗")}\n";
            report += $"- Target Rings: {(GameObject.FindGameObjectsWithTag("TargetRing").Length > 0 ? "✓" : "✗")}\n";
            report += $"- Ball Spawner: {(GameObject.FindGameObjectWithTag("BallSpawner") != null ? "✓" : "✗")}\n";

            Debug.Log(report);
        }
    }
}
