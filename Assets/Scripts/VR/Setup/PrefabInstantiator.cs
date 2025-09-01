using System.Collections.Generic;
using UnityEngine;
using VRRehab.UI;
using VRRehab.DataPersistence;
using VRRehab.Analytics;

namespace VRRehab.SceneSetup
{
    public class PrefabInstantiator : MonoBehaviour
    {
        [System.Serializable]
        public class PrefabEntry
        {
            public string name;
            public GameObject prefab;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public Vector3 scale = Vector3.one;
            public Transform parent;
            public bool dontDestroyOnLoad = false;
        }

        [Header("Core System Prefabs")]
        [SerializeField] private GameObject xrTemplatePrefab;
        [SerializeField] private GameObject canvasPrefab;
        [SerializeField] private GameObject progressionSystemPrefab;
        [SerializeField] private GameObject dataManagerPrefab;
        [SerializeField] private GameObject analyticsManagerPrefab;

        [Header("Custom Prefabs to Instantiate")]
        [SerializeField] private List<PrefabEntry> prefabsToInstantiate = new List<PrefabEntry>();

        [Header("Instantiation Settings")]
        [SerializeField] private bool instantiateOnAwake = true;
        [SerializeField] private bool instantiateCoreSystems = true;
        [SerializeField] private bool instantiateCustomPrefabs = true;

        private void Awake()
        {
            if (instantiateOnAwake)
            {
                InstantiateAll();
            }
        }

        [ContextMenu("Instantiate All")]
        public void InstantiateAll()
        {
            if (instantiateCoreSystems)
            {
                InstantiateCoreSystems();
            }

            if (instantiateCustomPrefabs)
            {
                InstantiateCustomPrefabs();
            }
        }

        [ContextMenu("Instantiate Core Systems")]
        public void InstantiateCoreSystems()
        {
            // XR Template
            if (xrTemplatePrefab != null && GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                GameObject xrObj = Instantiate(xrTemplatePrefab);
                xrObj.name = "XR_Template";
                DontDestroyOnLoad(xrObj);
                Debug.Log("XR Template instantiated");
            }

            // Canvas
            if (canvasPrefab != null && GameObject.FindObjectOfType<Canvas>() == null)
            {
                GameObject canvasObj = Instantiate(canvasPrefab);
                canvasObj.name = "MainCanvas";
                Debug.Log("Canvas instantiated");
            }

            // Progression System
            if (progressionSystemPrefab != null && GameObject.FindObjectOfType<ProgressionSystem>() == null)
            {
                GameObject progObj = Instantiate(progressionSystemPrefab);
                progObj.name = "ProgressionSystem";
                DontDestroyOnLoad(progObj);
                Debug.Log("Progression System instantiated");
            }

            // Data Manager
            if (dataManagerPrefab != null && GameObject.FindObjectOfType<DataPersistenceManager>() == null)
            {
                GameObject dataObj = Instantiate(dataManagerPrefab);
                dataObj.name = "DataPersistenceManager";
                DontDestroyOnLoad(dataObj);
                Debug.Log("Data Persistence Manager instantiated");
            }

            // Analytics Manager
            if (analyticsManagerPrefab != null && GameObject.FindObjectOfType<PerformanceAnalytics>() == null)
            {
                GameObject analyticsObj = Instantiate(analyticsManagerPrefab);
                analyticsObj.name = "AnalyticsManager";
                DontDestroyOnLoad(analyticsObj);
                Debug.Log("Analytics Manager instantiated");
            }
        }

        [ContextMenu("Instantiate Custom Prefabs")]
        public void InstantiateCustomPrefabs()
        {
            foreach (var entry in prefabsToInstantiate)
            {
                if (entry.prefab != null)
                {
                    // Check if already exists
                    if (GameObject.Find(entry.name) == null)
                    {
                        GameObject instance = Instantiate(
                            entry.prefab,
                            entry.position,
                            Quaternion.Euler(entry.rotation),
                            entry.parent
                        );

                        instance.name = entry.name;
                        instance.transform.localScale = entry.scale;

                        if (entry.dontDestroyOnLoad)
                        {
                            DontDestroyOnLoad(instance);
                        }

                        Debug.Log($"Custom prefab '{entry.name}' instantiated");
                    }
                }
                else
                {
                    Debug.LogWarning($"Prefab for '{entry.name}' is null");
                }
            }
        }

        [ContextMenu("Clear All Instantiated Objects")]
        public void ClearAllInstantiatedObjects()
        {
            // Clear core systems
            DestroyObjectIfExists("XR_Template");
            DestroyObjectIfExists("MainCanvas");
            DestroyObjectIfExists("ProgressionSystem");
            DestroyObjectIfExists("DataPersistenceManager");
            DestroyObjectIfExists("AnalyticsManager");

            // Clear custom prefabs
            foreach (var entry in prefabsToInstantiate)
            {
                DestroyObjectIfExists(entry.name);
            }

            Debug.Log("All instantiated objects cleared");
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

        // Public methods for runtime instantiation
        public void InstantiatePrefab(string prefabName)
        {
            var entry = prefabsToInstantiate.Find(p => p.name == prefabName);
            if (entry != null && entry.prefab != null)
            {
                GameObject instance = Instantiate(
                    entry.prefab,
                    entry.position,
                    Quaternion.Euler(entry.rotation),
                    entry.parent
                );

                instance.name = entry.name;
                instance.transform.localScale = entry.scale;

                if (entry.dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(instance);
                }

                Debug.Log($"Prefab '{prefabName}' instantiated");
            }
        }

        public void AddPrefabToList(string name, GameObject prefab, Vector3 position = default, Vector3 rotation = default, Vector3 scale = default, Transform parent = null)
        {
            if (scale == Vector3.zero) scale = Vector3.one;

            var entry = new PrefabEntry
            {
                name = name,
                prefab = prefab,
                position = position,
                rotation = rotation,
                scale = scale,
                parent = parent
            };

            prefabsToInstantiate.Add(entry);
        }

        public void RemovePrefabFromList(string name)
        {
            prefabsToInstantiate.RemoveAll(p => p.name == name);
        }
    }
}
