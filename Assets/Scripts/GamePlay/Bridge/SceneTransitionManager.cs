using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private CanvasGroup fadeCanvas;

    private static SceneTransitionManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    public void RestartCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    private System.Collections.IEnumerator LoadSceneWithFade(string sceneName)
    {
        // Fade out
        if (fadeCanvas != null)
            yield return StartCoroutine(FadeOut());

        // Load scene
        SceneManager.LoadScene(sceneName);

        // Fade in
        if (fadeCanvas != null)
            yield return StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, timer / fadeOutDuration);
            yield return null;
        }
        fadeCanvas.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, timer / fadeInDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }
}