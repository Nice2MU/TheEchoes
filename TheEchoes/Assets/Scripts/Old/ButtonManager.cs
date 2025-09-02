using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Panel Management")]
    public GameObject currentPanel;
    public GameObject targetPanel;

    [Header("Scene Loading")]
    public int sceneIndex = -1;

    [Header("Mobile Control")]
    public GameObject mobileControlUI;

    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.instance.effectSource.Stop();
        SoundManager.instance.PlaySFX("Point");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.instance.effectSource.Stop();
        SoundManager.instance.PlaySFX("Click");
    }

    public void SwitchScene()
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);

            if (SoundManager.instance != null)
                SoundManager.instance.MuteAll(false);
        }
    }

    public void SwitchPanel()
    {
        if (currentPanel != null && targetPanel != null)
        {
            PanelManager.Instance.OpenPanel(currentPanel, targetPanel);

            if (SoundManager.instance != null)
                SoundManager.instance.MuteAll(true);
        }
    }

    public void BackPanel()
    {
        PanelManager.Instance.BackPanel();
    }

    public void MobileControl()
    {
        if (mobileControlUI != null)
        {
            mobileControlUI.SetActive(!mobileControlUI.activeSelf);
        }
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