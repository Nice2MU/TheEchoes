using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public GameObject[] panels;
    public Button[] skipButtons;
    public Button[] pauseButtons;
    public float[] sceneTimes;
    public bool triggerOnce = true;
    public bool allowSkip = true;

    public Sprite pauseSprite;
    public Sprite playSprite;

    private int currentSceneIndex = 0;
    private bool hasTriggered = false;
    private bool isPaused = false;
    private bool isSkipping = false;
    private bool isCutscenePlaying = false;

    private void Start()
    {
        foreach (var panel in panels)
            panel.SetActive(false);

        foreach (var skipButton in skipButtons)
        {
            skipButton.onClick.AddListener(SkipAllScenes);
            skipButton.gameObject.SetActive(allowSkip);
        }

        foreach (var pauseButton in pauseButtons)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }
    }

    private void Update()
    {
        if (isCutscenePlaying && Input.GetKeyDown(KeyCode.F) && !isSkipping && allowSkip)
        {
            GoToNextScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(ShowCutscene());
        }
    }

    private IEnumerator ShowCutscene()
    {
        isCutscenePlaying = true;
        currentSceneIndex = 0;

        while (currentSceneIndex < panels.Length)
        {
            ShowPanel(currentSceneIndex);

            float elapsedTime = 0f;
            while (elapsedTime < sceneTimes[currentSceneIndex] && !isSkipping)
            {
                if (!isPaused)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                }
                yield return null;
            }

            if (!isSkipping)
            {
                panels[currentSceneIndex].SetActive(false);
                currentSceneIndex++;
            }
        }
        ClosePanel();
    }

    private void ShowPanel(int index)
    {
        if (index < panels.Length)
        {
            panels[index].SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void ClosePanel()
    {
        foreach (var panel in panels)
            panel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        isSkipping = false;
        isCutscenePlaying = false;

        if (!triggerOnce)
        {
            hasTriggered = false;
        }

        foreach (var pauseButton in pauseButtons)
        {
            Image img = pauseButton.GetComponent<Image>();
            if (img != null && pauseSprite != null)
            {
                img.sprite = pauseSprite;
            }
        }
    }

    private void GoToNextScene()
    {
        if (currentSceneIndex < panels.Length)
        {
            panels[currentSceneIndex].SetActive(false);
            currentSceneIndex++;
            if (currentSceneIndex < panels.Length)
            {
                ShowPanel(currentSceneIndex);
            }
            else
            {
                ClosePanel();
            }
        }
    }

    private void SkipAllScenes()
    {
        if (!isSkipping && allowSkip)
        {
            isSkipping = true;
            StopAllCoroutines();
            ClosePanel();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        foreach (var pauseButton in pauseButtons)
        {
            Image img = pauseButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = isPaused ? playSprite : pauseSprite;
            }
        }
    }
}