using System;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneStateManager : MonoBehaviour
{
    public static CutsceneStateManager I { get; private set; }

    private int activeSlot = 1;
    private bool deleteMode = false;

    private readonly Dictionary<int, HashSet<string>> seen = new();
    private readonly Dictionary<int, HashSet<string>> pending = new();

    private const string PlayerPrefsKey = "cutscene_state_v1";

    [Serializable]
    private class SlotData
    {
        public List<string> seen = new();
    }

    [Serializable]
    private class SaveData
    {
        public int version = 1;
        public int activeSlot = 1;
        public List<SlotData> slots = new()
        {
            new SlotData(), new SlotData(), new SlotData(), new SlotData()
        };
    }

    void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        for (int s = 1; s <= 4; s++)
        {
            seen[s] = new HashSet<string>();
            pending[s] = new HashSet<string>();
        }

        LoadFromPrefs();
    }

    public void OnSelectSlot1() => HandleSlotPress(1);
    public void OnSelectSlot2() => HandleSlotPress(2);
    public void OnSelectSlot3() => HandleSlotPress(3);
    public void OnSelectSlot4() => HandleSlotPress(4);

    public void StartGameWithSlot(int slot)
    {
        SelectSlot(slot);
        SaveToPrefs();
    }

    public void OnSaveButton() => CommitSaveForActiveSlot();

    public void OnDeleteButton()
    {
        deleteMode = true;
    }

    private void HandleSlotPress(int slot)
    {
        if (slot < 1 || slot > 4) return;

        if (deleteMode)
        {
            ResetSlot(slot);
            activeSlot = slot;
            deleteMode = false;
            SaveToPrefs();
            return;
        }

        SelectSlot(slot);
        SaveToPrefs();
    }

    private void SelectSlot(int slot)
    {
        if (slot < 1 || slot > 4) return;

        pending[activeSlot].Clear();
        activeSlot = slot;
    }

    public bool HasSeen(string cutsceneId)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId)) return false;
        return seen[activeSlot].Contains(cutsceneId);
    }

    public bool HasSeenOrPending(string cutsceneId)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId)) return false;
        return seen[activeSlot].Contains(cutsceneId) || pending[activeSlot].Contains(cutsceneId);
    }

    public void FlagPending(string cutsceneId)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId)) return;
        pending[activeSlot].Add(cutsceneId);
    }

    public void CommitSaveForActiveSlot()
    {
        foreach (var id in pending[activeSlot])
            seen[activeSlot].Add(id);

        pending[activeSlot].Clear();
        SaveToPrefs();
    }

    public void ResetSlot(int slot)
    {
        if (slot < 1 || slot > 4) return;
        seen[slot].Clear();
        pending[slot].Clear();
        SaveToPrefs();
    }

    public int GetActiveSlot() => activeSlot;

    private void SaveToPrefs()
    {
        try
        {
            var data = new SaveData
            {
                version = 1,
                activeSlot = Mathf.Clamp(activeSlot, 1, 4)
            };

            for (int s = 1; s <= 4; s++)
            {
                data.slots[s - 1].seen.Clear();
                data.slots[s - 1].seen.AddRange(seen[s]);
            }

            string json = JsonUtility.ToJson(data, false);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception)
        {

        }
    }

    private void LoadFromPrefs()
    {
        try
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

            string json = PlayerPrefs.GetString(PlayerPrefsKey, "");
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            activeSlot = Mathf.Clamp(data.activeSlot, 1, 4);

            for (int s = 1; s <= 4; s++)
            {
                seen[s].Clear();
                pending[s].Clear();

                var slotData = (data.slots != null && data.slots.Count >= s) ? data.slots[s - 1] : null;
                if (slotData?.seen != null)
                {
                    foreach (var id in slotData.seen)
                    {
                        if (!string.IsNullOrWhiteSpace(id))
                            seen[s].Add(id);
                    }
                }
            }
        }
        catch (Exception)
        {

        }
    }
}