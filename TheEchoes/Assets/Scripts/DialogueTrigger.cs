using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class DialogueCharacter { public string name; public Sprite icon; }
[System.Serializable] public class DialogueLine { public DialogueCharacter character; [TextArea(3, 10)] public string line; }
[System.Serializable]
public class DialogueChoice
{
    [TextArea(1, 5)] public string choiceText;
    public Dialogue nextDialogue;

    [Header("Objects to toggle on choose")]
    public List<GameObject> enableOnChoose = new();
    public List<GameObject> disableOnChoose = new();

    public enum QuestChoiceAction { None, Accept, TurnIn }
    public enum RequireState { Any, NotAccepted, Active, Completed, TurnedIn }

    [Header("Quest (optional)")]
    public QuestChoiceAction questAction = QuestChoiceAction.None;
    public string questId;
    public string targetItemId;
    public int targetCount = 1;
    public RequireState requiredState = RequireState.Any;
}

[System.Serializable] public class DialogueNode { public List<DialogueLine> linesInNode = new(); public List<DialogueChoice> choices = new(); }
[System.Serializable]
public class Dialogue
{
    [Header("Linear Mode")] public List<DialogueLine> dialogueLines = new();
    [Header("Branching Mode")] public bool useBranching = false;
    [HideInInspector] public int startNodeIndex = 0;
    public List<DialogueNode> nodes = new();
#if UNITY_EDITOR
    private void OnValidate() { startNodeIndex = 0; }
#endif
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Default Dialogue")]
    public Dialogue defaultDialogue;

    [Header("Quest Conditional Dialogue")]
    public bool useQuestCondition = false;
    public string questId;
    public Dialogue dialogueIfActive;
    public Dialogue dialogueIfCompleted;

    [Header("One-time Dialogue Setting")]
    public bool isOneTimeDialogue = false;
    private bool hasTriggered = false;

    [Header("Completed Lock")]
    public bool lockAfterCompletedOnce = true;

    public string NpcStableId => DialogueIdUtil.GetStableId(gameObject);

    private static string S(bool v) => v ? "TRUE" : "FALSE";
    private static string DName(Dialogue d) => d == null ? "null" : (d.useBranching ? "BranchingDialogue" : "LinearDialogue");

    private void OnEnable() { EnableTriggerIfAllowed(); }

    public void TriggerDialogue()
    {
        var st = useQuestCondition && !string.IsNullOrEmpty(questId)
            ? QuestSystem.GetState(questId)
            : QuestSystem.QState.NotAccepted;

        DebugState("[TriggerDialogue]");

        if (isOneTimeDialogue && hasTriggered)
        {
            bool allowLock = true;
            if (useQuestCondition && !string.IsNullOrEmpty(questId))
                allowLock = (st == QuestSystem.QState.TurnedIn);

            if (allowLock)
            {
                return;
            }
        }

        Dialogue targetDialogue = GetDialogueByQuestState(st);

        if (targetDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(targetDialogue, NpcStableId);
            hasTriggered = true;

            if (lockAfterCompletedOnce && useQuestCondition && !string.IsNullOrEmpty(questId)
                && st == QuestSystem.QState.Completed)
            {
                DialogueRuntime.Instance?.MarkNpcCompleted(NpcStableId);

                DisableTriggerBecauseCompleted();
            }

            if (isOneTimeDialogue)
            {
                bool shouldDisable =
                    !useQuestCondition
                    || string.IsNullOrEmpty(questId)
                    || st == QuestSystem.QState.TurnedIn;

                if (shouldDisable)
                {
                    DisableTriggerBecauseCompleted();
                }
            }
        }
    }

    private Dialogue GetDialogueByQuestState(QuestSystem.QState st)
    {
        if (!useQuestCondition || string.IsNullOrEmpty(questId))
            return defaultDialogue;

        switch (st)
        {
            case QuestSystem.QState.Active:
                return dialogueIfActive ?? defaultDialogue;
            case QuestSystem.QState.Completed:
            case QuestSystem.QState.TurnedIn:
                return dialogueIfCompleted ?? defaultDialogue;
            default:
                return defaultDialogue;
        }
    }

    public void DisableTriggerBecauseCompleted()
    {
        enabled = false;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    public void EnableTriggerIfAllowed()
    {
        enabled = true;
        hasTriggered = false;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = true;
    }

    public void ForceRecheckAgainstRuntime()
    {
        if (DialogueRuntime.Instance && DialogueRuntime.Instance.IsNpcCompleted(NpcStableId))
            DisableTriggerBecauseCompleted();
        else
            EnableTriggerIfAllowed();
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (!enabled) return;
        if (c.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    private void DebugState(string prefix)
    {
        int slot = Mathf.Max(1, GlobalGame.CurrentSlot);
        bool npcCompleted = DialogueRuntime.Instance != null && DialogueRuntime.Instance.IsNpcCompleted(NpcStableId);
        bool colliderEnabled = GetComponent<Collider2D>() ? GetComponent<Collider2D>().enabled : false;

        string questInfo = "NoQuestCondition";
        if (useQuestCondition && !string.IsNullOrEmpty(questId))
        {
            var state = QuestSystem.GetState(questId);
            var target = QuestSystem.GetTarget(questId);
            int cur = QuestSystem.GetCount(questId);
            questInfo = $"QuestId={questId} State={state} TargetItem='{target.itemId}' Need={target.need} Current={cur} CanTurnIn={QuestSystem.CanTurnIn(questId)}";
        }
    }
}