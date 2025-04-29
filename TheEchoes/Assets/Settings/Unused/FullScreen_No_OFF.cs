using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FullScreen_No_OFF : MonoBehaviour
{
    public Button myButton;
    public Button saveButton;
    public Button resetButton;
    private TextMeshProUGUI buttonText;
    private bool isOn = false;
    private bool tempIsOn = false;

    private const string FullScreenKey = "FullScreenState";

    void Start()
    {
        buttonText = myButton.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null)
        {
            return;
        }

        isOn = PlayerPrefs.GetInt(FullScreenKey, 0) == 1;
        tempIsOn = isOn;
        Screen.fullScreen = isOn;

        myButton.onClick.AddListener(Toggle);
        saveButton.onClick.AddListener(SaveState);
        resetButton.onClick.AddListener(ResetState);
        UpdateText();
    }

    void Toggle()
    {
        tempIsOn = !tempIsOn;
        UpdateText();
    }

    void SaveState()
    {
        if (tempIsOn != isOn)
        {
            isOn = tempIsOn;
            Screen.fullScreen = isOn;
            PlayerPrefs.SetInt(FullScreenKey, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void ResetState()
    {
        tempIsOn = false;
        UpdateText();
    }

    void UpdateText()
    {
        buttonText.text = tempIsOn ? "เปิด" : "ปิด";
    }
}