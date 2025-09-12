using System;
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

[Serializable] public class LumerinSave { public float currentBoost; }
[Serializable] public class RambleSave { }

[Serializable]
public class SlotSummary
{
    public int slot;
    public string scene;
    public string lastSavedIso;
    public double totalPlaySeconds;
    public bool isEmpty => string.IsNullOrEmpty(scene);
}

public static class GlobalGame { public static int CurrentSlot = 1; }

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
        try { return JsonUtility.FromJson<SaveData>(json); }
        catch { Debug.LogWarning($"[SaveDataSystem] Corrupt json at {key}"); return null; }
    }

    public static void Delete(int slot)
    {
        PlayerPrefs.DeleteKey(Key(slot));
        PlayerPrefs.Save();
    }
}