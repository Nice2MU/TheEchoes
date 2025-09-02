using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public GameObject loadingPanel;
    public float displayTime = 0.7f;

    private void Awake()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);

            if (SoundManager.instance != null)
                SoundManager.instance.MuteAll(false);
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