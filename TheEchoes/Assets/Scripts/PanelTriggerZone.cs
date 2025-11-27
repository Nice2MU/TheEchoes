using UnityEngine;
using UnityEngine.UI;

public class PanelTriggerZone : MonoBehaviour
{
    [Header("Player Settings")]
    public string playerTag = "Player";
    public MonoBehaviour playerControlScript;

    [Header("UI Panel")]
    public GameObject targetPanel;

    [Header("Close Button")]
    public Button closeButton;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (targetPanel != null)
            targetPanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            OpenPanel();
        }
    }

    void OpenPanel()
    {
        if (targetPanel != null)
            targetPanel.SetActive(true);

        if (playerControlScript != null)
            playerControlScript.enabled = false;
    }

    void ClosePanel()
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);

        if (playerControlScript != null)
            playerControlScript.enabled = true;
    }
}