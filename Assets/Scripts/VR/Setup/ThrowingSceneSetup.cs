using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRRehab.UI;
using VRRehab.DataPersistence;

namespace VRRehab.SceneSetup
{
    public class ThrowingSceneSetup : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject targetRingPrefab;
        [SerializeField] private GameObject spawnZonePrefab;

        [Header("Scene Configuration")]
        [SerializeField] private int numberOfRings = 5;
        [SerializeField] private float ringSpacing = 2f;
        [SerializeField] private float ringHeight = 1.5f;
        [SerializeField] private float ringDistance = 3f;

        [Header("Level Data")]
        [SerializeField] private ThrowingLevelData levelData;

        private void Awake()
        {
            StartCoroutine(SetupThrowingScene());
        }

        private IEnumerator SetupThrowingScene()
        {
            yield return new WaitForEndOfFrame();

            // Setup VR Environment
            SetupVREnvironment();

            // Setup throwing components
            SetupThrowingComponents();

            // Setup UI
            SetupUI();

            // Initialize level if level data is provided
            if (levelData != null)
            {
                InitializeFromLevelData();
            }
            else
            {
                SetupDefaultLevel();
            }

            Debug.Log("Throwing scene setup completed!");
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

        private void SetupThrowingComponents()
        {
            // Setup spawn zone and ball spawner
            SetupBallSpawner();

            // Setup target rings
            SetupTargetRings();

            // Setup stage manager
            SetupStageManager();
        }

        private void SetupBallSpawner()
        {
            if (GameObject.FindGameObjectWithTag("BallSpawner") == null)
            {
                // Create spawn zone
                Vector3 spawnPosition = new Vector3(0, 0, 0);
                GameObject spawnZone = Instantiate(spawnZonePrefab, spawnPosition, Quaternion.identity);
                spawnZone.name = "BallSpawnZone";

                // Create ball spawner
                GameObject spawnerObj = new GameObject("BallSpawner");
                var ballSpawner = spawnerObj.AddComponent<BallSpawner>();
                ballSpawner.ballPrefab = ballPrefab;
                ballSpawner.spawnPoint = spawnZone.transform;
                ballSpawner.respawnDelay = 1f;

                // Setup respawn zone
                var respawnZone = spawnZone.GetComponent<BallRespawnZone>();
                if (respawnZone != null)
                {
                    respawnZone.spawner = ballSpawner;
                }

                // Spawn initial ball
                ballSpawner.SpawnBall();

                Debug.Log("Ball spawner setup completed");
            }
        }

        private void SetupTargetRings()
        {
            // Clear existing rings
            GameObject[] existingRings = GameObject.FindGameObjectsWithTag("TargetRing");
            foreach (GameObject ring in existingRings)
            {
                Destroy(ring);
            }

            // Create new rings
            for (int i = 0; i < numberOfRings; i++)
            {
                Vector3 position = new Vector3(
                    (i - (numberOfRings - 1) / 2f) * ringSpacing,
                    ringHeight,
                    ringDistance
                );

                GameObject ring = Instantiate(targetRingPrefab, position, Quaternion.identity);
                ring.name = $"TargetRing_{i + 1}";
                ring.tag = "TargetRing";
            }

            Debug.Log($"Created {numberOfRings} target rings");
        }

        private void SetupStageManager()
        {
            // Find or create stage manager
            ObjectStageManager stageManager = FindObjectOfType<ObjectStageManager>();
            if (stageManager == null)
            {
                GameObject managerObj = new GameObject("StageManager");
                stageManager = managerObj.AddComponent<ObjectStageManager>();
            }

            // Setup stage manager with rings
            GameObject[] rings = GameObject.FindGameObjectsWithTag("TargetRing");
            TargetRing[] targetRings = new TargetRing[rings.Length];
            for (int i = 0; i < rings.Length; i++)
            {
                targetRings[i] = rings[i].GetComponent<TargetRing>();
            }

            stageManager.targets = targetRings;
            stageManager.stageTime = 60f;
            stageManager.enableTimer = true;

            // Setup UI controller
            var uiController = FindObjectOfType<StageUIController>();
            if (uiController != null)
            {
                stageManager.uiController = uiController;
            }

            Debug.Log("Stage manager setup completed");
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
        }

        private void InitializeFromLevelData()
        {
            if (levelData == null) return;

            // Setup rings from level data
            if (levelData.ringPositions != null && levelData.ringPositions.Count > 0)
            {
                // Clear existing rings
                GameObject[] existingRings = GameObject.FindGameObjectsWithTag("TargetRing");
                foreach (GameObject ring in existingRings)
                {
                    Destroy(ring);
                }

                // Create rings from level data
                for (int i = 0; i < levelData.ringPositions.Count; i++)
                {
                    GameObject ring = Instantiate(targetRingPrefab, levelData.ringPositions[i], Quaternion.identity);
                    ring.name = $"TargetRing_{i + 1}";
                    ring.tag = "TargetRing";
                }

                Debug.Log($"Created {levelData.ringPositions.Count} target rings from level data");
            }

            // Setup ball spawn position
            if (spawnZonePrefab != null)
            {
                GameObject spawnZone = GameObject.Find("BallSpawnZone");
                if (spawnZone != null)
                {
                    spawnZone.transform.position = levelData.ballSpawnPosition;
                }
            }
        }

        private void SetupDefaultLevel()
        {
            // Create a simple level with 5 rings
            numberOfRings = 5;
            SetupTargetRings();
        }

        // Public methods for runtime level changes
        public void SetNumberOfRings(int count)
        {
            numberOfRings = count;
            SetupTargetRings();
            SetupStageManager();
        }

        public void SetRingConfiguration(float spacing, float height, float distance)
        {
            ringSpacing = spacing;
            ringHeight = height;
            ringDistance = distance;
            SetupTargetRings();
        }
    }
}
