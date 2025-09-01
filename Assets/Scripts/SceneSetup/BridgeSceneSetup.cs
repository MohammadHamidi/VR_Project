using System.Collections;
using UnityEngine;
using VRRehab.UI;
using VRRehab.DataPersistence;

namespace VRRehab.SceneSetup
{
    public class BridgeSceneSetup : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;

        [Header("Bridge Configuration")]
        [SerializeField] private BridgeConfiguration bridgeConfig;
        [SerializeField] private AnchorFactory.AnchorType anchorType = AnchorFactory.AnchorType.Standard;

        [Header("Scene Objects")]
        [SerializeField] private GameObject groundPlane;
        [SerializeField] private GameObject environmentPrefab;

        private SOLIDBridgeBuilder bridgeBuilder;

        private void Awake()
        {
            StartCoroutine(SetupBridgeScene());
        }

        private IEnumerator SetupBridgeScene()
        {
            yield return new WaitForEndOfFrame();

            // Setup VR Environment
            SetupVREnvironment();

            // Setup environment
            SetupEnvironment();

            // Setup bridge builder
            SetupBridgeBuilder();

            // Setup UI
            SetupUI();

            Debug.Log("Bridge scene setup completed!");
        }

        private void SetupVREnvironment()
        {
            if (GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                if (xrTemplatePrefab != null)
                {
                    Instantiate(xrTemplatePrefab);
                }
            }
        }

        private void SetupEnvironment()
        {
            // Create ground plane if it doesn't exist
            if (GameObject.Find("Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(10, 1, 10);

                // Add collider and material
                var renderer = ground.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // You can assign a material here if needed
                }
            }

            // Instantiate environment prefab if provided
            if (environmentPrefab != null && GameObject.Find("Environment") == null)
            {
                Instantiate(environmentPrefab);
            }
        }

        private void SetupBridgeBuilder()
        {
            // Find existing bridge builder or create new one
            bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();
            if (bridgeBuilder == null)
            {
                GameObject builderObj = new GameObject("BridgeBuilder");
                bridgeBuilder = builderObj.AddComponent<SOLIDBridgeBuilder>();
            }

            // Configure bridge builder
            if (bridgeConfig != null)
            {
                bridgeBuilder.SetBridgeConfiguration(bridgeConfig);
                bridgeBuilder.SetAnchorType(anchorType);
            }
            else
            {
                // Create default configuration
                bridgeConfig = new BridgeConfiguration();
                bridgeConfig.numberOfPlanks = 8;
                bridgeConfig.totalBridgeLength = 12f;
                bridgeConfig.plankWidth = 1.5f;
                bridgeConfig.createPlatforms = true;

                bridgeBuilder.SetBridgeConfiguration(bridgeConfig);
                bridgeBuilder.SetAnchorType(AnchorFactory.AnchorType.Standard);
            }

            // Build the bridge
            bridgeBuilder.BuildBridge();

            Debug.Log("Bridge builder setup completed");
        }

        private void SetupUI()
        {
            // Setup Canvas
            if (GameObject.FindObjectOfType<Canvas>() == null)
            {
                if (canvasPrefab != null)
                {
                    Instantiate(canvasPrefab);
                }
            }

            // Setup UIManager
            if (GameObject.FindObjectOfType<UIManager>() == null)
            {
                GameObject uiManager = new GameObject("UIManager");
                uiManager.AddComponent<UIManager>();
            }

            // Setup Bridge UI Controller if needed
            if (FindObjectOfType<BridgeUIController>() == null)
            {
                GameObject uiController = new GameObject("BridgeUIController");
                uiController.AddComponent<BridgeUIController>();
            }
        }

        // Public methods for runtime configuration
        public void SetBridgeConfiguration(int planks, float length, float width)
        {
            if (bridgeConfig != null)
            {
                bridgeConfig.numberOfPlanks = planks;
                bridgeConfig.totalBridgeLength = length;
                bridgeConfig.plankWidth = width;

                if (bridgeBuilder != null)
                {
                    bridgeBuilder.RebuildBridge();
                }
            }
        }

        public void ChangeAnchorType(AnchorFactory.AnchorType newType)
        {
            anchorType = newType;
            if (bridgeBuilder != null)
            {
                bridgeBuilder.SetAnchorType(newType);
            }
        }

        public void RebuildBridge()
        {
            if (bridgeBuilder != null)
            {
                bridgeBuilder.RebuildBridge();
            }
        }

        public void ShowAnchors(bool show)
        {
            if (bridgeBuilder != null)
            {
                if (show)
                    bridgeBuilder.ShowAnchors();
                else
                    bridgeBuilder.HideAnchors();
            }
        }
    }
}
