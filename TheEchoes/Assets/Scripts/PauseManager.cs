using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    bool gamePaused = false;
    [SerializeField] GameObject pauseMenu;

    void Start()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }

        gamePaused = false;
        pauseMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gamePaused == false)
        {
            PauseGame();
        }

        else if (Input.GetKeyDown(KeyCode.Escape) && gamePaused == true)
        {
            ResumeGame();
        }
    }

    public void Continue()
    {
        ResumeGame();
    }

    public void ClosePauseMenu()
    {
        ResumeGame();
    }

    void PauseGame()
    {
        Time.timeScale = 0;
        gamePaused = true;
        pauseMenu.SetActive(true);
    }

    void ResumeGame()
    {
        Time.timeScale = 1;
        gamePaused = false;
        pauseMenu.SetActive(false);
    }
}