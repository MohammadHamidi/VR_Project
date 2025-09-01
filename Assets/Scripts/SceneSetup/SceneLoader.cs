using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRRehab.UI;

namespace VRRehab.SceneSetup
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string sceneToLoad;
        [SerializeField] private bool showLoadingScreen = true;

        private UIManager uiManager;

        private void Awake()
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        public void LoadScene(string sceneName)
        {
            sceneToLoad = sceneName;
            StartCoroutine(LoadSceneAsync());
        }

        public void LoadThrowingScene()
        {
            LoadScene("ThrowBall");
        }

        public void LoadBridgeScene()
        {
            LoadScene("Bridge");
        }

        public void LoadSquatScene()
        {
            LoadScene("Squat");
        }

        public void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }

        private IEnumerator LoadSceneAsync()
        {
            if (showLoadingScreen && uiManager != null)
            {
                uiManager.ShowLoading($"Loading {sceneToLoad}...");
            }

            // Load the scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                if (showLoadingScreen && uiManager != null)
                {
                    uiManager.UpdateProgress(asyncLoad.progress, $"Loading {sceneToLoad}... {(asyncLoad.progress * 100):0}%");
                }

                yield return null;
            }

            if (showLoadingScreen && uiManager != null)
            {
                uiManager.HideLoading();
                uiManager.ShowSuccess($"{sceneToLoad} loaded successfully!");
            }

            Debug.Log($"Scene {sceneToLoad} loaded successfully!");
        }
    }
}
