using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Panel Management")]
    public GameObject currentPanel;
    public GameObject targetPanel;

    [Header("Scene Management")]
    public int sceneNumber = -1;

    private int currentSceneIndex;
    private int previousSceneIndex;

    void Start()
    {
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (!PlayerPrefs.HasKey("previousScene" + currentSceneIndex))
        {
            PlayerPrefs.SetInt("previousScene" + currentSceneIndex, currentSceneIndex);
        }

        previousSceneIndex = PlayerPrefs.GetInt("previousScene" + currentSceneIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.instance.effectSource.Stop();
        SoundManager.instance.PlaySFX("Point");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.instance.PlaySFX("Click");

        if (sceneNumber >= 0)
        {
            LoadScene();
        }

        if (currentPanel != null && targetPanel != null)
        {
            SwitchPanel();
        }
    }

    public void LoadScene()
    {
        Time.timeScale = 1f;

        PlayerPrefs.SetInt("previousScene" + sceneNumber, currentSceneIndex);
        SceneManager.LoadScene(sceneNumber);
    }

    public void BackScene()
    {
        SceneManager.LoadScene(previousSceneIndex);
    }

    public void SwitchPanel()
    {
        if (currentPanel != null && targetPanel != null)
        {
            PanelManager.Instance.OpenPanel(currentPanel, targetPanel);
        }
    }

    public void BackPanel()
    {
        PanelManager.Instance.BackPanel();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}