using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialZone
    {
        public string tag;
        public int gifIndex;

        public float autoHideDelay = 5f;
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

    private readonly HashSet<string> autoShownTags = new HashSet<string>();

    private Coroutine autoHideRoutine;
    private float currentAutoHideDelay = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        foreach (var zone in tutorialZones)
        {
            if (collision.CompareTag(zone.tag))
            {
                isInShowZone = true;
                currentGifIndex = zone.gifIndex;
                currentAutoHideDelay = zone.autoHideDelay;

                if (!autoShownTags.Contains(zone.tag))
                {
                    AutoShowOnce(zone.gifIndex, zone.autoHideDelay);
                    autoShownTags.Add(zone.tag);
                }

                else
                {
                    if (tutorialPanel != null && !tutorialPanel.activeSelf && tutorialButton != null)
                        tutorialButton.SetActive(true);
                }
                return;
            }
        }

        if (collision.CompareTag("THide"))
        {
            isInShowZone = false;
            currentGifIndex = -1;

            CancelAutoHideIfAny();
            ForceHideAll();

            if (tutorialButton != null)
                tutorialButton.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (currentGifIndex >= 0 && currentGifIndex < gifObjects.Length)
        {
            ShowGif(currentGifIndex);
            if (tutorialButton != null) tutorialButton.SetActive(false);

            CancelAutoHideIfAny();
        }
    }

    public void HideTutorial()
    {
        ForceHideAll();

        if (isInShowZone && tutorialButton != null)
        {
            tutorialButton.SetActive(true);
        }
    }

    private void AutoShowOnce(int gifIndex, float delay)
    {
        CancelAutoHideIfAny();
        ShowGif(gifIndex);

        RestartAutoHide(delay);
    }

    private void ShowGif(int index)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        for (int i = 0; i < gifObjects.Length; i++)
        {
            if (gifObjects[i] != null) gifObjects[i].SetActive(false);
        }

        if (index >= 0 && index < gifObjects.Length && gifObjects[index] != null)
        {
            gifObjects[index].SetActive(true);
        }

        if (tutorialButton != null) tutorialButton.SetActive(false);
    }

    private void ForceHideAll()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        for (int i = 0; i < gifObjects.Length; i++)
        {
            if (gifObjects[i] != null) gifObjects[i].SetActive(false);
        }
    }

    private void RestartAutoHide(float delay)
    {
        if (delay <= 0f) return;

        CancelAutoHideIfAny();
        autoHideRoutine = StartCoroutine(AutoHideAfterDelay(delay));
    }

    private void CancelAutoHideIfAny()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }
    }

    private IEnumerator AutoHideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideTutorial();
        autoHideRoutine = null;
    }
}