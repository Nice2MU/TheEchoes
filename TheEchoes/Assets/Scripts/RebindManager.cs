using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public class RebindActionEntry
{
    public InputActionReference actionRef;
    public int bindingIndex;
}

public class RebindManager : MonoBehaviour
{
    public List<RebindActionEntry> rebindActions;

    [Header("UI")]
    public Transform buttonContainer;
    public Button buttonPrefab;
    public GameObject rebindingPanel;

    [Header("Reset UI")]
    public Button resetButton;
    public GameObject confirmPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private Dictionary<RebindActionEntry, Button> actionButtons = new Dictionary<RebindActionEntry, Button>();
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private RebindActionEntry currentRebindingEntry;

    public static System.Action OnRebindComplete;

    void Start()
    {
        LoadAllBindings();
        rebindingPanel.SetActive(false);

        foreach (var entry in rebindActions)
        {
            if (entry.actionRef == null) continue;

            Button btn = Instantiate(buttonPrefab, buttonContainer);

            string keyName = GetBindingDisplayName(entry.actionRef, entry.bindingIndex);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = keyName;

            btn.onClick.AddListener(() => StartRebind(entry));
            actionButtons[entry] = btn;
        }

        resetButton.onClick.AddListener(ShowConfirmResetPanel);
        confirmYesButton.onClick.AddListener(ResetToDefault);
        confirmNoButton.onClick.AddListener(() => confirmPanel.SetActive(false));
        confirmPanel.SetActive(false);
    }

    string GetBindingDisplayName(InputActionReference actionRef, int bindingIndex)
    {
        if (actionRef.action.bindings.Count > bindingIndex && bindingIndex >= 0)
        {
            return InputControlPath.ToHumanReadableString(
                actionRef.action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        return "Unbound";
    }

    public void StartRebind(RebindActionEntry entry)
    {
        if (rebindingOperation != null) return;

        if (entry.bindingIndex < 0 || entry.bindingIndex >= entry.actionRef.action.bindings.Count)
        {
            return;
        }

        foreach (var rebindEntry in rebindActions)
        {
            rebindEntry.actionRef.action.Disable();
        }

        currentRebindingEntry = entry;
        rebindingPanel.SetActive(true);

        rebindingOperation = entry.actionRef.action.PerformInteractiveRebinding(entry.bindingIndex)
            .WithControlsExcluding("Mouse")
            .WithControlsExcluding("Pointer")
            .WithControlsExcluding("Gamepad")
            .WithControlsExcluding("Joystick")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation =>
            {
                string newBindingPath = entry.actionRef.action.bindings[entry.bindingIndex].effectivePath;

                foreach (var rebindEntry in rebindActions)
                {
                    if (rebindEntry.bindingIndex == entry.bindingIndex)
                    {
                        rebindEntry.actionRef.action.ApplyBindingOverride(rebindEntry.bindingIndex, newBindingPath);
                    }
                }

                SaveAllBindings();
                UpdateAllUI();

                foreach (var rebindEntry in rebindActions)
                {
                    rebindEntry.actionRef.action.Enable();
                }

                rebindingOperation.Dispose();
                rebindingOperation = null;
                rebindingPanel.SetActive(false);
                OnRebindComplete?.Invoke();
            })

            .OnCancel(operation =>
            {
                foreach (var rebindEntry in rebindActions)
                {
                    rebindEntry.actionRef.action.Enable();
                }

                rebindingOperation.Dispose();
                rebindingOperation = null;
                rebindingPanel.SetActive(false);
            })

            .Start();
    }

    void UpdateAllUI()
    {
        foreach (var entry in rebindActions)
        {
            if (entry.actionRef != null && actionButtons.TryGetValue(entry, out Button button))
            {
                string keyName = GetBindingDisplayName(entry.actionRef, entry.bindingIndex);
                button.GetComponentInChildren<TextMeshProUGUI>().text = keyName;
            }
        }
    }

    void SaveAllBindings()
    {
        foreach (var entry in rebindActions)
        {
            if (entry.actionRef != null)
            {
                string key = $"rebind_{entry.actionRef.action.actionMap.name}_{entry.actionRef.action.name}";
                string json = entry.actionRef.action.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString(key, json);
            }
        }

        PlayerPrefs.Save();
    }

    void LoadAllBindings()
    {
        foreach (var entry in rebindActions)
        {
            if (entry.actionRef != null)
            {
                string key = $"rebind_{entry.actionRef.action.actionMap.name}_{entry.actionRef.action.name}";
                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key);
                    entry.actionRef.action.LoadBindingOverridesFromJson(json);
                }
            }
        }
    }

    void ShowConfirmResetPanel()
    {
        confirmPanel.SetActive(true);
    }

    void ResetToDefault()
    {
        foreach (var entry in rebindActions)
        {
            if (entry.actionRef != null)
            {
                string key = $"rebind_{entry.actionRef.action.actionMap.name}_{entry.actionRef.action.name}";
                PlayerPrefs.DeleteKey(key);

                entry.actionRef.action.RemoveAllBindingOverrides();
                OnRebindComplete?.Invoke();
            }
        }

        PlayerPrefs.Save();
        UpdateAllUI();
        confirmPanel.SetActive(false);
    }
}