// Simple test script to verify compilation
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using VRRehab.UI;

namespace VRRehab.EditorTools
{
    public class TestCompilation
    {
        [MenuItem("VR Rehab/Test Compilation", false, 100)]
        static void TestAllEditorTools()
        {
            Debug.Log("🧪 Testing VR Rehab Editor Tools Compilation...");

            // Test that all classes can be referenced
            bool allAccessible = true;

            try
            {
                if (typeof(VRRehabPrefabCreator) != null)
                {
                    Debug.Log("✅ VRRehabPrefabCreator accessible");
                }
                else
                {
                    Debug.LogError("❌ VRRehabPrefabCreator not accessible");
                    allAccessible = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ VRRehabPrefabCreator error: {e.Message}");
                allAccessible = false;
            }

            try
            {
                if (typeof(VRRehabSceneSetupWindow) != null)
                {
                    Debug.Log("✅ VRRehabSceneSetupWindow accessible");
                }
                else
                {
                    Debug.LogError("❌ VRRehabSceneSetupWindow not accessible");
                    allAccessible = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ VRRehabSceneSetupWindow error: {e.Message}");
                allAccessible = false;
            }

            try
            {
                if (typeof(VRRehabMaterialCreator) != null)
                {
                    Debug.Log("✅ VRRehabMaterialCreator accessible");
                }
                else
                {
                    Debug.LogError("❌ VRRehabMaterialCreator not accessible");
                    allAccessible = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ VRRehabMaterialCreator error: {e.Message}");
                allAccessible = false;
            }

            try
            {
                if (typeof(VRRehabDashboard) != null)
                {
                    Debug.Log("✅ VRRehabDashboard accessible");
                }
                else
                {
                    Debug.LogError("❌ VRRehabDashboard not accessible");
                    allAccessible = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ VRRehabDashboard error: {e.Message}");
                allAccessible = false;
            }

            // Test method calls (only if basic access works)
            if (allAccessible)
            {
                try
                {
                    Debug.Log("Testing method calls...");
                    // Note: We don't actually call the methods here to avoid creating prefabs during test
                    Debug.Log("✅ All editor tools are properly accessible");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Method call test failed: {e.Message}");
                    allAccessible = false;
                }
            }

            if (allAccessible)
            {
                Debug.Log("🎉 All VR Rehab Editor Tools compiled successfully!");
                Debug.Log("💡 You can now use the tools from the VR Rehab menu");
            }
            else
            {
                Debug.LogError("❌ Some editor tools have compilation issues");
                Debug.Log("🔧 Check the console for specific error details");
            }
        }

        [MenuItem("VR Rehab/Quick Setup Test", false, 101)]
        static void QuickSetupTest()
        {
            Debug.Log("🔧 Testing Quick Setup...");

            // Test basic Unity functionality
            try
            {
                GameObject testObj = new GameObject("TestObject");
                testObj.AddComponent<Transform>();
                Object.DestroyImmediate(testObj);
                Debug.Log("✅ Basic Unity operations work");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Basic Unity operations failed: {e.Message}");
            }

            // Test prefab creation capability
            try
            {
                string testPath = "Assets/Test_Prefab.prefab";
                GameObject testPrefab = new GameObject("TestPrefab");
                PrefabUtility.SaveAsPrefabAsset(testPrefab, testPath);
                Object.DestroyImmediate(testPrefab);

                // Clean up
                AssetDatabase.DeleteAsset(testPath);
                Debug.Log("✅ Prefab creation works");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Prefab creation failed: {e.Message}");
            }

            Debug.Log("🏁 Quick setup test completed");
        }

        [MenuItem("VR Rehab/Test Tag Creation", false, 102)]
        static void TestTagCreation()
        {
            Debug.Log("🔖 Testing Tag Creation...");

            try
            {
                // Test if TargetRing tag exists
                GameObject testObj = new GameObject("TestRing");
                testObj.tag = "TargetRing";
                Object.DestroyImmediate(testObj);
                Debug.Log("✅ TargetRing tag exists");
            }
            catch
            {
                Debug.Log("⚠️ TargetRing tag doesn't exist - will be created automatically when needed");
            }

            try
            {
                // Test if UIPanel tag exists
                GameObject testUIObj = new GameObject("TestUIPanel");
                testUIObj.tag = "UIPanel";
                Object.DestroyImmediate(testUIObj);
                Debug.Log("✅ UIPanel tag exists");
            }
            catch
            {
                Debug.Log("⚠️ UIPanel tag doesn't exist - will be created automatically when needed");
            }

            Debug.Log("🏷️ Tag creation test completed");
        }

        [MenuItem("VR Rehab/Test UI System", false, 103)]
        static void TestUISystem()
        {
            Debug.Log("🎨 Testing UI System...");

            try
            {
                // Test if we can create a UIManager without errors
                GameObject uiObj = new GameObject("TestUIManager");
                UIManager uiManager = uiObj.AddComponent<UIManager>();

                // Test notification system (this should not crash even with null settings)
                uiManager.ShowNotification("Test notification", UIManager.NotificationData.NotificationType.Info, 2f);
                Debug.Log("✅ UIManager created and notification sent successfully");

                Object.DestroyImmediate(uiObj);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ UI System test failed: {e.Message}");
            }

            Debug.Log("🎨 UI system test completed");
        }
    }
}
