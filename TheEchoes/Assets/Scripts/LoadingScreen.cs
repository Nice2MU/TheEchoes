using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingPanel;
    public float displayTime = 0.7f;

    private void Awake()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
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

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}