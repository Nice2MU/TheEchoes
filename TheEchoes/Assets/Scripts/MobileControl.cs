using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

[System.Serializable]
public class MobileButtonEntry
{
    public GameObject buttonObject;
    public RebindActionEntry[] targets;
}

[System.Serializable]
public class MobileStickEntry
{
    public OnScreenStick stick;
    public RebindActionEntry[] targets;
}

public class MobileControl : MonoBehaviour
{
    public MobileButtonEntry[] buttonEntries;
    public MobileStickEntry[] stickEntries;
    public bool loadOverridesFromPlayerPrefs = true;
    public string playerPrefsPrefix = "rebind_";

    void Awake()
    {
        Input.simulateMouseWithTouches = true;

        if (loadOverridesFromPlayerPrefs) LoadAllOverridesFromSaves();
        ApplyBindings();
    }

    void OnEnable()
    {
        if (loadOverridesFromPlayerPrefs) LoadAllOverridesFromSaves();
        ApplyBindings();

        TrySubscribeRebindEvent(true);
    }

    void OnDisable()
    {
        TrySubscribeRebindEvent(false);
    }

    private void TrySubscribeRebindEvent(bool subscribe)
    {
        var type = System.Type.GetType("RebindManager");
        if (type == null) return;

        var evtField = type.GetField("OnRebindComplete",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (evtField == null) return;

        var current = evtField.GetValue(null) as System.Action;
        System.Action handler = HandleRebindComplete;

        if (subscribe)
            evtField.SetValue(null, current + handler);

        else
            evtField.SetValue(null, current - handler);
    }

    private void HandleRebindComplete()
    {
        if (loadOverridesFromPlayerPrefs) LoadAllOverridesFromSaves();
        ApplyBindings();
    }

    private void LoadAllOverridesFromSaves()
    {
        foreach (var action in EnumerateUniqueActions())
            TryLoadOverrideFromPrefs(action);
    }

    private IEnumerable<InputAction> EnumerateUniqueActions()
    {
        var set = new HashSet<InputAction>();

        if (buttonEntries != null)
            foreach (var e in buttonEntries)
                if (e?.targets != null)
                    foreach (var t in e.targets)
                        if (t?.actionRef?.action != null)
                            set.Add(t.actionRef.action);

        if (stickEntries != null)
            foreach (var e in stickEntries)
                if (e?.targets != null)
                    foreach (var t in e.targets)
                        if (t?.actionRef?.action != null)
                            set.Add(t.actionRef.action);

        return set;
    }

    private void TryLoadOverrideFromPrefs(InputAction action)
    {
        if (action == null) return;
        string key = BuildPrefsKey(action);
        if (!PlayerPrefs.HasKey(key)) return;

        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            action.LoadBindingOverridesFromJson(json);
        }

        catch (System.Exception e)
        {

        }
    }

    private string BuildPrefsKey(InputAction action)
    {
        var map = action.actionMap != null ? action.actionMap.name : "Default";
        return $"{playerPrefsPrefix}{map}_{action.name}";
    }

    public void ApplyBindings()
    {
        ApplyButtons();
        ApplySticks();
    }

    private void ApplyButtons()
    {
        if (buttonEntries == null) return;

        foreach (var entry in buttonEntries)
        {
            if (entry == null || entry.buttonObject == null || entry.targets == null) continue;

            var desiredPaths = new HashSet<string>();
            foreach (var t in entry.targets)
            {
                var p = GetBindingPath(t?.actionRef?.action, t?.bindingIndex ?? -1);
                if (!string.IsNullOrEmpty(p)) desiredPaths.Add(p);
            }

            var existing = new List<OnScreenButton>();
            entry.buttonObject.GetComponents(existing);

            for (int i = existing.Count - 1; i >= 0; i--)
            {
                var osb = existing[i];
                bool keep = osb != null && !string.IsNullOrEmpty(osb.controlPath) && desiredPaths.Contains(osb.controlPath);
                if (!keep)
                {
                    if (osb != null) Destroy(osb);
                    existing.RemoveAt(i);
                }
            }

            foreach (var path in desiredPaths)
            {
                if (existing.Any(x => x != null && x.controlPath == path)) continue;

                var osb = entry.buttonObject.AddComponent<OnScreenButton>();
                osb.controlPath = path;
                existing.Add(osb);
            }
        }
    }

    private void ApplySticks()
    {
        if (stickEntries == null) return;

        foreach (var entry in stickEntries)
        {
            if (entry == null || entry.stick == null || entry.targets == null) continue;

            var hostGO = entry.stick.gameObject;

            var desiredPaths = new HashSet<string>();
            foreach (var t in entry.targets)
            {
                var p = GetBindingPath(t?.actionRef?.action, t?.bindingIndex ?? -1);
                if (!string.IsNullOrEmpty(p)) desiredPaths.Add(p);
            }

            var existing = new List<OnScreenStick>();
            hostGO.GetComponents(existing);

            for (int i = existing.Count - 1; i >= 0; i--)
            {
                var oss = existing[i];
                if (oss == null) { existing.RemoveAt(i); continue; }

                bool keep = !string.IsNullOrEmpty(oss.controlPath) && desiredPaths.Contains(oss.controlPath);
                if (!keep)
                {
                    if (oss == entry.stick && existing.Count == 1)
                    {
                        oss.controlPath = null;
                        continue;
                    }
                    Destroy(oss);
                    existing.RemoveAt(i);
                }
            }

            foreach (var path in desiredPaths)
            {
                bool has = existing.Any(x => x != null && x.controlPath == path);
                if (has) continue;

                var empty = existing.FirstOrDefault(x => string.IsNullOrEmpty(x.controlPath));
                if (empty != null)
                {
                    empty.controlPath = path;
                    continue;
                }

                var oss = hostGO.AddComponent<OnScreenStick>();
                oss.controlPath = path;
                existing.Add(oss);
            }
        }
    }

    private string GetBindingPath(InputAction action, int index)
    {
        if (action == null) return null;
        if (index < 0 || index >= action.bindings.Count) return null;

        var b = action.bindings[index];
        if (b.isComposite) return null;
        return string.IsNullOrEmpty(b.effectivePath) ? null : b.effectivePath;
    }
}