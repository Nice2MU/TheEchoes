using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialZone
    {
        public string tag;
        public int gifIndex;
    }

    [Header("UI Button")]
    public GameObject tutorialButton;

    [Header("Popup UI")]
    public GameObject tutorialPanel;
    public GameObject[] gifObjects;

    [Header("Tutorial Zones")]
    public List<TutorialZone> tutorialZones = new List<TutorialZone>();

    private int currentGifIndex = -1;
    private bool isInShowZone = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        foreach (var zone in tutorialZones)
        {
            if (collision.CompareTag(zone.tag))
            {
                isInShowZone = true;
                currentGifIndex = zone.gifIndex;

                if (!tutorialPanel.activeSelf)
                {
                    tutorialButton.SetActive(true);
                }

                return;
            }
        }

        if (collision.CompareTag("THide"))
        {
            isInShowZone = false;
            currentGifIndex = -1;
            tutorialButton.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (currentGifIndex >= 0 && currentGifIndex < gifObjects.Length)
        {
            tutorialPanel.SetActive(true);
            gifObjects[currentGifIndex].SetActive(true);
            tutorialButton.SetActive(false);
        }

        else
        {

        }
    }

    public void HideTutorial()
    {
        tutorialPanel.SetActive(false);

        foreach (GameObject gif in gifObjects)
        {
            gif.SetActive(false);
        }

        if (isInShowZone)
        {
            tutorialButton.SetActive(true);
        }
    }
}