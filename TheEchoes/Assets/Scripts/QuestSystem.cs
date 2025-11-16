using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }
    public enum QState { NotAccepted = 0, Active = 1, Completed = 2, TurnedIn = 3 }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    static int Slot() => Mathf.Max(1, GlobalGame.CurrentSlot);
    static string Key(int slot, string qid, string field) => $"q_{slot}_{qid}_{field}";

    public static QState GetState(string qid)
    {
        int v = PlayerPrefs.GetInt(Key(Slot(), qid, "state"), 0);
        return (QState)Mathf.Clamp(v, 0, 3);
    }
    public static int GetCount(string qid) => PlayerPrefs.GetInt(Key(Slot(), qid, "count"), 0);
    public static (string itemId, int need) GetTarget(string qid)
    {
        int s = Slot();
        string item = PlayerPrefs.GetString(Key(s, qid, "target"), "");
        int need = PlayerPrefs.GetInt(Key(s, qid, "need"), 1);
        return (item, Mathf.Max(1, need));
    }
    public static string GetGiver(string qid) => PlayerPrefs.GetString(Key(Slot(), qid, "giver"), "");

    public static void Accept(string qid, string giverStableId, string itemId, int need)
    {
        int s = Slot();
        PlayerPrefs.SetInt(Key(s, qid, "state"), (int)QState.Active);
        PlayerPrefs.SetString(Key(s, qid, "target"), itemId ?? "");
        PlayerPrefs.SetInt(Key(s, qid, "need"), Mathf.Max(1, need));
        if (!string.IsNullOrEmpty(giverStableId))
            PlayerPrefs.SetString(Key(s, qid, "giver"), giverStableId);

        EnsureQuestListed(qid);
        PlayerPrefs.Save();
        TryPromote(qid);
    }

    public static void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        int s = Slot();

        for (int i = 0; i < 64; i++)
        {
            string qid = PlayerPrefs.GetString($"q_list_{s}_{i}", "");
            if (string.IsNullOrEmpty(qid)) continue;
            if (GetState(qid) != QState.Active) continue;

            var (tid, need) = GetTarget(qid);
            if (tid == itemId)
            {
                int cur = Mathf.Clamp(GetCount(qid) + amount, 0, need);
                PlayerPrefs.SetInt(Key(s, qid, "count"), cur);
                TryPromote(qid);
            }
        }
        PlayerPrefs.Save();
    }

    public static bool CanTurnIn(string qid) => GetState(qid) == QState.Completed;

    public static bool TurnIn(string qid, bool markNpcCompleted)
    {
        if (!CanTurnIn(qid)) return false;
        int s = Slot();
        PlayerPrefs.SetInt(Key(s, qid, "state"), (int)QState.TurnedIn);
        PlayerPrefs.Save();

        if (markNpcCompleted)
        {
            string giver = GetGiver(qid);
            if (!string.IsNullOrEmpty(giver))
                DialogueRuntime.Instance?.MarkNpcCompleted(giver);
        }
        return true;
    }

    static void TryPromote(string qid)
    {
        if (GetState(qid) != QState.Active) return;
        var (_, need) = GetTarget(qid);
        if (GetCount(qid) >= need)
        {
            PlayerPrefs.SetInt(Key(Slot(), qid, "state"), (int)QState.Completed);
            PlayerPrefs.Save();
        }
    }

    public static void EnsureQuestListed(string qid)
    {
        int s = Slot();
        for (int i = 0; i < 64; i++)
        {
            string k = $"q_list_{s}_{i}";
            string exist = PlayerPrefs.GetString(k, "");
            if (exist == qid) return;
            if (string.IsNullOrEmpty(exist))
            {
                PlayerPrefs.SetString(k, qid);
                PlayerPrefs.Save();
                return;
            }
        }
    }
}