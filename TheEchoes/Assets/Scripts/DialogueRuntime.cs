using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class DialogueRuntime : MonoBehaviour
{
    public static DialogueRuntime Instance { get; private set; }

    private HashSet<string> completedNpc = new HashSet<string>();
    private Dictionary<string, bool> objectStates = new Dictionary<string, bool>();

    private Dictionary<string, bool> defaultActive = new Dictionary<string, bool>();
    private HashSet<string> touchedIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SnapshotSceneDefaults();
        touchedIds.Clear();
        RefreshSceneForCurrentSlot();
    }

    public void ClearAll()
    {
        completedNpc.Clear();
        objectStates.Clear();
    }

    public void MarkNpcCompleted(string npcStableId)
    {
        if (string.IsNullOrEmpty(npcStableId)) return;
        completedNpc.Add(npcStableId);
        ApplyNpcCompletionToScene(npcStableId);
    }
    public bool IsNpcCompleted(string npcStableId)
        => !string.IsNullOrEmpty(npcStableId) && completedNpc.Contains(npcStableId);
    private void ApplyNpcCompletionToScene(string npcStableId)
    {
        var allTriggers = FindObjectsOfType<DialogueTrigger>(true);
        foreach (var t in allTriggers)
            if (t != null && t.NpcStableId == npcStableId)
                t.DisableTriggerBecauseCompleted();
    }

    public void RecordObjectActive(GameObject go, bool active)
    {
        if (!go) return;
        var id = DialogueIdUtil.GetStableId(go);
        if (string.IsNullOrEmpty(id)) return;
        objectStates[id] = active;
        touchedIds.Add(id);
    }
    public bool TryGetObjectActiveById(string id, out bool active)
        => objectStates.TryGetValue(id, out active);

    public void LoadFromSave(DialogueSave save)
    {
        ClearAll();
        if (save != null)
        {
            if (save.completedNpcIds != null)
                foreach (var id in save.completedNpcIds)
                    if (!string.IsNullOrEmpty(id)) completedNpc.Add(id);

            if (save.toggledObjectIds != null && save.toggledObjectStates != null)
            {
                int n = Mathf.Min(save.toggledObjectIds.Count, save.toggledObjectStates.Count);
                for (int i = 0; i < n; i++)
                {
                    var id = save.toggledObjectIds[i];
                    var st = save.toggledObjectStates[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        objectStates[id] = st;
                        touchedIds.Add(id);
                    }
                }
            }
        }
        RefreshSceneForCurrentSlot();
    }

    public DialogueSave BuildSave()
    {
        var s = new DialogueSave();
        s.completedNpcIds = completedNpc.ToList();
        foreach (var kv in objectStates)
        {
            s.toggledObjectIds.Add(kv.Key);
            s.toggledObjectStates.Add(kv.Value);
        }
        return s;
    }

    private void SnapshotSceneDefaults()
    {
        defaultActive.Clear();
        var index = DialogueIdUtil.BuildScenePathIndex();
        foreach (var kv in index)
        {
            var go = kv.Value;
            if (!go) continue;
            defaultActive[kv.Key] = go.activeSelf;
        }
    }

    private void RestoreSceneDefaults()
    {
        if (defaultActive.Count == 0) SnapshotSceneDefaults();

        var index = DialogueIdUtil.BuildScenePathIndex();

        foreach (var id in touchedIds)
        {
            if (index.TryGetValue(id, out var go) && go)
            {
                bool baseActive = defaultActive.TryGetValue(id, out var v) ? v : go.activeSelf;
                if (go.activeSelf != baseActive) go.SetActive(baseActive);
            }
        }

        var allTriggers = FindObjectsOfType<DialogueTrigger>(true);
        foreach (var t in allTriggers)
        {
            if (t == null) continue;
            t.EnableTriggerIfAllowed();
        }
    }

    public void RefreshSceneForCurrentSlot()
    {
        RestoreSceneDefaults();
        ApplyLoadedStateToScene();
        StartCoroutine(RescanTriggersEndOfFrame());
    }

    public void ApplyLoadedStateToScene()
    {
        var index = DialogueIdUtil.BuildScenePathIndex();
        foreach (var kv in objectStates)
        {
            if (index.TryGetValue(kv.Key, out var go) && go && go.activeSelf != kv.Value)
            {
                go.SetActive(kv.Value);
                touchedIds.Add(kv.Key);
            }
        }

        var allTriggers = FindObjectsOfType<DialogueTrigger>(true);
        foreach (var t in allTriggers)
        {
            if (t == null) continue;
            if (IsNpcCompleted(t.NpcStableId))
                t.DisableTriggerBecauseCompleted();
            else
                t.EnableTriggerIfAllowed();
        }
    }

    private IEnumerator RescanTriggersEndOfFrame()
    {
        yield return null;
        var triggers = FindObjectsOfType<DialogueTrigger>(true);
        foreach (var t in triggers)
        {
            if (t == null) continue;
            t.ForceRecheckAgainstRuntime();
        }
    }
}