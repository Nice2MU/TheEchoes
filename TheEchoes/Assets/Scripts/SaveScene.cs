using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveScene : MonoBehaviour
{
    int sceneIndex;
    int sceneToOpen;

    void Start()
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (!PlayerPrefs.HasKey("previousScene" + sceneIndex))
        {
            PlayerPrefs.SetInt("previousScene" + sceneIndex, sceneIndex);
        }
        sceneToOpen = PlayerPrefs.GetInt("previousScene" + sceneIndex);
    }

    public void BackScene()
    {
        SceneManager.LoadScene(sceneToOpen);
    }
}