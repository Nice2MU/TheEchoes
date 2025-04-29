using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    public Canvas loadingCanvas;
    public float displayTime = 1.0f;

    private void Awake()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.enabled = true;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HideLoadingScreenAfterDelay());
    }

    private IEnumerator HideLoadingScreenAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);

        if (loadingCanvas != null)
        {
            loadingCanvas.enabled = false;
        }
    }
}