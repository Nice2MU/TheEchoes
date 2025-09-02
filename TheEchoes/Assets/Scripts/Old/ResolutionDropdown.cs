using UnityEngine;
using TMPro;

public class ResolutionDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    private readonly Vector2Int[] resolutions = new Vector2Int[]
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440)
    };

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "1280x720",
            "1920x1080",
            "2560x1440"
        });

        // ตั้งค่าเริ่มต้นให้เป็น 1920x1080
        int defaultIndex = System.Array.FindIndex(resolutions, r => r.x == 1920 && r.y == 1080);
        if (defaultIndex >= 0)
        {
            dropdown.value = defaultIndex;
        }

        dropdown.onValueChanged.AddListener(SetResolution);
    }

    void SetResolution(int index)
    {
        Vector2Int res = resolutions[index];
        Screen.SetResolution(res.x, res.y, Screen.fullScreen);

        Canvas.ForceUpdateCanvases();
    }
}
