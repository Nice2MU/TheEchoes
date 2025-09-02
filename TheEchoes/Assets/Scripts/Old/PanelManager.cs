using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    private static PanelManager instance;

    private Stack<GameObject> panelStack = new Stack<GameObject>();
    private GameObject currentPanel;

    public static PanelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<PanelManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("PanelManager");
                    instance = obj.AddComponent<PanelManager>();
                }
            }

            return instance;
        }
    }

    public void OpenPanel(GameObject currentPanel, GameObject newPanel)
    {
        if (currentPanel != null && currentPanel.activeSelf)
        {
            currentPanel.SetActive(false);
        }

        if (newPanel != null)
        {
            newPanel.SetActive(true);
            this.currentPanel = newPanel;
        }

        panelStack.Push(currentPanel);
    }

    public void BackPanel()
    {
        if (panelStack.Count > 0)
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }

            currentPanel = panelStack.Pop();
            if (currentPanel != null)
            {
                currentPanel.SetActive(true);
            }
        }
    }
}