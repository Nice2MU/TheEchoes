using UnityEngine;
using UnityEngine.InputSystem;

public class RebindSave : MonoBehaviour
{
    public InputActionAsset inputActions;

    private static bool alreadyLoaded = false;

    void Awake()
    {
        if (alreadyLoaded) return;
        alreadyLoaded = true;
        DontDestroyOnLoad(gameObject);

        if (inputActions == null)
        {
            return;
        }

        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                string key = $"rebind_{map.name}_{action.name}";
                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key);
                    action.LoadBindingOverridesFromJson(json);
                }
            }
        }
    }
}