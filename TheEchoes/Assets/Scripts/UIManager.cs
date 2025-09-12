using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject initialPanel;
    [SerializeField] private GameObject pauseMenu;

    [SerializeField] private GameObject[] disallowPauseWhenActive;

    private GameObject current;
    private readonly Stack<GameObject> history = new();
    private bool gamePaused = false;

    private void Start()
    {
        if (initialPanel != null) ShowOnly(initialPanel);
        if (pauseMenu != null) pauseMenu.SetActive(false);

        SetTimeScale(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsAnyBlockingPanelOpen()) return;

            if (!gamePaused) Pause();

            else Resume();
        }
    }

    public void ShowPanel(GameObject target)
    {
        if (target == null) return;

        if (target == current)
        {
            if (!current.activeInHierarchy)
                current.SetActive(true);
            return;
        }

        if (current != null)
        {
            current.SetActive(false);
            history.Push(current);
        }

        current = target;
        current.SetActive(true);
    }

    public void Back()
    {
        if (history.Count == 0) return;

        if (current != null) current.SetActive(false);

        current = history.Pop();
        if (current != null) current.SetActive(true);
    }

    public void GoHome()
    {
        if (current != null) current.SetActive(false);

        history.Clear();

        if (initialPanel != null)
        {
            current = initialPanel;
            current.SetActive(true);
        }

        else
        {
            current = null;
        }
    }

    private void ShowOnly(GameObject target)
    {
        if (target == null) return;

        var parent = target.transform.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        target.SetActive(true);
        current = target;
        history.Clear();
    }

    public void Pause()
    {
        if (gamePaused) return;

        gamePaused = true;
        SetTimeScale(true);

        if (pauseMenu != null)
            ShowPanel(pauseMenu);

        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(true);
    }

    public void Resume()
    {
        if (!gamePaused) return;

        gamePaused = false;
        SetTimeScale(false);

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            if (current == pauseMenu) current = null;
        }

        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(false);
    }

    public void ExitToHome()
    {
        gamePaused = false;
        SetTimeScale(false);

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (current == pauseMenu) current = null;

        GoHome();
        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(false);
    }

    public void OpenPauseSubPanel(GameObject targetPanel)
    {
        if (!gamePaused) Pause();
        if (targetPanel == null) return;

        ShowPanel(targetPanel);
    }

    public void Continue() => Resume();

    public void EnterGameplay()
    {
        current = null;
    }

    private bool IsAnyBlockingPanelOpen()
    {
        if (disallowPauseWhenActive == null) return false;

        foreach (var go in disallowPauseWhenActive)
        {
            if (go != null && go.activeInHierarchy) return true;
        }
        return false;
    }

    private void SetTimeScale(bool paused)
    {
        Time.timeScale = paused ? 0 : 1;
    }
}