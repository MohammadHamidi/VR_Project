using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRRehab.DataPersistence;
using VRRehab.UI;
using VRRehab.Analytics;

namespace VRRehab.SceneSetup
{
    public class VRSceneSetup : MonoBehaviour
    {
        [Header("VR Components")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;

        [Header("Exercise Components")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject targetRingPrefab;
        [SerializeField] private GameObject spawnZonePrefab;

        [Header("Managers")]
        [SerializeField] private GameObject progressionSystemPrefab;
        [SerializeField] private GameObject dataManagerPrefab;
        [SerializeField] private GameObject analyticsManagerPrefab;

        private void Awake()
        {
            StartCoroutine(SetupVRScene());
        }

        private IEnumerator SetupVRScene()
        {
            yield return new WaitForEndOfFrame();

            // Setup VR Environment
            SetupVREnvironment();

            // Setup Core Systems
            SetupCoreSystems();

            // Setup Exercise Components
            SetupExerciseComponents();

            // Setup UI
            SetupUI();

            Debug.Log("VR Scene setup completed!");
        }

        private void SetupVREnvironment()
        {
            // Instantiate XR Template if not already present
            if (GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                if (xrTemplatePrefab != null)
                {
                    Instantiate(xrTemplatePrefab);
                    Debug.Log("XR Template instantiated");
                }
            }
        }

        private void SetupCoreSystems()
        {
            // Setup Progression System
            if (GameObject.FindObjectOfType<ProgressionSystem>() == null)
            {
                if (progressionSystemPrefab != null)
                {
                    Instantiate(progressionSystemPrefab);
                    Debug.Log("Progression System instantiated");
                }
            }

            // Setup Data Persistence Manager
            if (GameObject.FindObjectOfType<DataPersistenceManager>() == null)
            {
                if (dataManagerPrefab != null)
                {
                    Instantiate(dataManagerPrefab);
                    Debug.Log("Data Persistence Manager instantiated");
                }
            }

            // Setup Analytics Manager
            if (GameObject.FindObjectOfType<PerformanceAnalytics>() == null)
            {
                if (analyticsManagerPrefab != null)
                {
                    Instantiate(analyticsManagerPrefab);
                    Debug.Log("Analytics Manager instantiated");
                }
            }
        }

        private void SetupExerciseComponents()
        {
            // Setup Ball Spawner
            if (GameObject.FindGameObjectWithTag("BallSpawner") == null)
            {
                if (spawnZonePrefab != null)
                {
                    GameObject spawnZone = Instantiate(spawnZonePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    Debug.Log("Ball spawn zone instantiated");

                    // Setup ball spawner component
                    if (ballPrefab != null)
                    {
                        GameObject spawner = new GameObject("BallSpawner");
                        var ballSpawner = spawner.AddComponent<BallSpawner>();
                        ballSpawner.ballPrefab = ballPrefab;
                        ballSpawner.spawnPoint = spawnZone.transform;
                        ballSpawner.respawnDelay = 1f;

                        var respawnZone = spawnZone.GetComponent<BallRespawnZone>();
                        if (respawnZone != null)
                        {
                            respawnZone.spawner = ballSpawner;
                        }

                        Debug.Log("Ball spawner setup completed");
                    }
                }
            }

            // Setup Target Rings (create 3 rings in a line)
            if (GameObject.FindGameObjectsWithTag("TargetRing").Length == 0)
            {
                if (targetRingPrefab != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 position = new Vector3(i * 2f - 2f, 1.5f, 3f);
                        GameObject ring = Instantiate(targetRingPrefab, position, Quaternion.identity);
                        ring.name = $"TargetRing_{i + 1}";
                    }
                    Debug.Log("Target rings instantiated");
                }
            }
        }

        private void SetupUI()
        {
            // Setup Canvas if not already present
            if (GameObject.FindObjectOfType<Canvas>() == null)
            {
                if (canvasPrefab != null)
                {
                    GameObject canvas = Instantiate(canvasPrefab);
                    Debug.Log("UI Canvas instantiated");
                }
            }

            // Setup UIManager if not already present
            if (GameObject.FindObjectOfType<UIManager>() == null)
            {
                GameObject uiManager = new GameObject("UIManager");
                var manager = uiManager.AddComponent<UIManager>();
                Debug.Log("UIManager instantiated");
            }
        }
    }
}
