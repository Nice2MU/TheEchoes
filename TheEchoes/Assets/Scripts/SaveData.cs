using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string scene;
    public float posX, posY, posZ;
    public string lastSavedIso;
    public double totalPlaySeconds;
    public string previewImageBase64;

    public MorphSave morph;
    public LumerinSave lumerin;
    public RambleSave ramble;
    public HealthSave health;

    public DialogueSave dialogue = new DialogueSave();

    public BossesSave bosses = new BossesSave();
}

[Serializable]
public class DialogueSave
{
    public List<string> completedNpcIds = new List<string>();

    public List<string> toggledObjectIds = new List<string>();
    public List<bool> toggledObjectStates = new List<bool>();

    public List<string> pickupIds = new List<string>();
    public List<string> pickupCooldownUntilIso = new List<string>();

    public void SetObjectState(string id, bool active)
    {
        int i = toggledObjectIds.IndexOf(id);
        if (i < 0)
        {
            toggledObjectIds.Add(id);
            toggledObjectStates.Add(active);
        }
        else
        {
            toggledObjectStates[i] = active;
        }
    }

    public bool TryGetObjectState(string id, out bool active)
    {
        int i = toggledObjectIds.IndexOf(id);
        if (i >= 0)
        {
            active = toggledObjectStates[i];
            return true;
        }
        active = false;
        return false;
    }

    public void SetPickupCooldown(string id, DateTime untilUtc)
    {
        string iso = untilUtc.ToString("o");
        int i = pickupIds.IndexOf(id);
        if (i < 0)
        {
            pickupIds.Add(id);
            pickupCooldownUntilIso.Add(iso);
        }
        else
        {
            pickupCooldownUntilIso[i] = iso;
        }
    }

    public bool TryGetPickupCooldown(string id, out DateTime untilUtc)
    {
        int i = pickupIds.IndexOf(id);
        if (i >= 0)
        {
            if (DateTime.TryParse(pickupCooldownUntilIso[i], out var t))
            {
                untilUtc = DateTime.SpecifyKind(t, DateTimeKind.Utc);
                return true;
            }
        }

        untilUtc = DateTime.MinValue;
        return false;
    }

    public void ClearPickupCooldown(string id)
    {
        int i = pickupIds.IndexOf(id);
        if (i >= 0)
        {
            pickupIds.RemoveAt(i);
            pickupCooldownUntilIso.RemoveAt(i);
        }
    }

    public void MergeFrom(DialogueSave src)
    {
        if (src == null) return;

        foreach (var id in src.completedNpcIds)
            if (!completedNpcIds.Contains(id))
                completedNpcIds.Add(id);

        for (int i = 0; i < src.toggledObjectIds.Count; i++)
            SetObjectState(src.toggledObjectIds[i], src.toggledObjectStates[i]);

        for (int i = 0; i < src.pickupIds.Count; i++)
        {
            var id = src.pickupIds[i];
            if (DateTime.TryParse(src.pickupCooldownUntilIso[i], out var t))
                SetPickupCooldown(id, DateTime.SpecifyKind(t, DateTimeKind.Utc));
        }
    }
}

[Serializable]
public class HealthSave
{
    public int hits;
    public float checkpointX, checkpointY, checkpointZ;
}

[Serializable]
public class MorphSave
{
    public string controllerName;
    public string morphTag;
    public float timeLeft;
    public bool isMorphing;
    public bool hasStored;
}

[Serializable]
public class LumerinSave
{
    public float currentBoost;
}

[Serializable]
public class RambleSave
{
}

[Serializable]
public class SlotSummary
{
    public int slot;
    public string scene;
    public string lastSavedIso;
    public double totalPlaySeconds;
    public bool isEmpty => string.IsNullOrEmpty(scene);
}

[Serializable]
public class BossesSave
{
    public List<BossSaveEntry> bosses = new List<BossSaveEntry>();

    public BossSaveEntry Get(string id)
    {
        return bosses.Find(b => b.id == id);
    }

    public void Upsert(BossSaveEntry entry)
    {
        int idx = bosses.FindIndex(b => b.id == entry.id);
        if (idx >= 0) bosses[idx] = entry;
        else bosses.Add(entry);
    }
}

[Serializable]
public class BossSaveEntry
{
    public string id;
    public bool isDead;

    public bool[] activateOnStartStates;
    public bool[] deactivateOnStartStates;
    public bool[] activateOnDeathStates;
    public bool[] deactivateOnDeathStates;
}

public static class GlobalGame
{
    public static int CurrentSlot = 1;
}

public static class SaveDataSystem
{
    public const int MaxSlots = 4;
    private const string KeyPrefix = "save_slot_";
    private static string Key(int slot) => $"{KeyPrefix}{slot}";

    public static bool Has(int slot)
    {
        var k = Key(slot);
        if (!PlayerPrefs.HasKey(k)) return false;
        var json = PlayerPrefs.GetString(k, "");
        return !string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("{");
    }

    public static void Save(int slot, SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key(slot), json);
        PlayerPrefs.Save();
    }

    public static SaveData Load(int slot)
    {
        string key = Key(slot);
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Delete(int slot)
    {
        PlayerPrefs.DeleteKey(Key(slot));
        PlayerPrefs.Save();
    }
}