using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI: Header & Text")]
    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    [Header("UI: Choices")]
    public GameObject choicePanel;
    public Transform choiceContainer;
    public Button choiceButtonPrefab;

    private Queue<DialogueLine> lines;

    [Header("Typing Settings")]
    public bool isDialogueActive = false;
    public float typingSpeed = 0.07f;
    public Animator animator;

    private PlayerControl playerController;
    private PlayerInput playerInput;
    private Rigidbody2D rb2d;
    private SpriteRenderer[] spriteRenderers;

    private bool lockFacing = false;
    private float savedScaleX = 1f;
    private Dictionary<SpriteRenderer, bool> savedFlipX = new();
    private RigidbodyConstraints2D savedConstraints;

    private bool prevPlayerControlEnabled = true;
    private bool prevPlayerInputEnabled = true;

    private Dialogue currentDialogue = null;
    private int currentNodeIndex = -1;
    private DialogueNode currentNode = null;
    private bool isBranchingMode = false;
    private bool isShowingChoices = false;

    private Coroutine typingRoutine = null;
    private bool isTyping = false;
    private string currentFullLineText = "";

    private string currentNpcStableId = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        lines = new Queue<DialogueLine>();

        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            playerController = player.GetComponent<PlayerControl>();
            playerInput = player.GetComponent<PlayerInput>();
            rb2d = player.GetComponent<Rigidbody2D>();
            spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (choicePanel) choicePanel.SetActive(false);
    }

    public void StartDialogue(Dialogue d) => StartDialogue(d, null);

    public void StartDialogue(Dialogue d, string npcStableId)
    {
        currentNpcStableId = npcStableId;
        currentDialogue = d;
        isBranchingMode = (d != null && d.useBranching);

        currentNodeIndex = isBranchingMode ? 0 : -1;
        currentNode = null;

        isDialogueActive = true;
        if (animator) animator.Play("show");

        PrepareAndLockPlayer();

        if (isBranchingMode)
            StartNode(currentNodeIndex);
        else
        {
            lines.Clear();
            if (d != null && d.dialogueLines != null)
                foreach (var dl in d.dialogueLines)
                    lines.Enqueue(dl);

            DisplayNextDialogueLine();
        }
    }

    private void StartNode(int nodeIndex)
    {
        if (currentDialogue == null || currentDialogue.nodes == null ||
            nodeIndex < 0 || nodeIndex >= currentDialogue.nodes.Count)
        { EndDialogue(); return; }

        currentNodeIndex = nodeIndex;
        currentNode = currentDialogue.nodes[currentNodeIndex];

        lines.Clear();
        if (currentNode.linesInNode != null)
            foreach (var dl in currentNode.linesInNode)
                lines.Enqueue(dl);

        HideChoices();
        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (isShowingChoices) return;
        if (lines.Count == 0) return;

        var line = lines.Dequeue();

        if (characterIcon)
            characterIcon.sprite = (line.character != null) ? line.character.icon : null;
        if (characterName)
            characterName.text = (line.character != null) ? line.character.name : "";

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeSentence(line));
    }

    private IEnumerator TypeSentence(DialogueLine dl)
    {
        isTyping = true;
        currentFullLineText = dl.line ?? "";
        if (dialogueArea) dialogueArea.text = "";

        if (SoundManager.instance)
        {
            SoundManager.instance.effectSource.loop = true;
            SoundManager.instance.effectSource.clip = SoundManager.instance.typing;
            SoundManager.instance.effectSource.Play();
        }

        foreach (char ch in currentFullLineText)
        {
            if (!isTyping) break;
            if (dialogueArea) dialogueArea.text += ch;
            yield return new WaitForSeconds(typingSpeed);
        }

        StopTypingSound();
        typingRoutine = null;
        isTyping = false;
    }

    private void ShowChoices(List<DialogueChoice> choices)
    {
        if (!choicePanel || !choiceContainer || !choiceButtonPrefab)
        { GoNextNodeOrEnd(); return; }

        foreach (Transform t in choiceContainer)
            Destroy(t.gameObject);

        foreach (var c in choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.text = c.choiceText;
            btn.onClick.AddListener(() => OnChoiceSelected(c));
        }

        isShowingChoices = true;
        choicePanel.SetActive(true);
    }

    private void HideChoices()
    {
        if (choicePanel) choicePanel.SetActive(false);
        isShowingChoices = false;
        if (choiceContainer)
            foreach (Transform t in choiceContainer)
                Destroy(t.gameObject);
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        ApplyChoiceQuest(choice);
        ApplyChoiceToggles(choice);
        HideChoices();

        if (choice.nextDialogue != null)
        {
            StartDialogue(choice.nextDialogue, currentNpcStableId);
            return;
        }
        GoNextNodeOrEnd();
    }

    private void ApplyChoiceQuest(DialogueChoice choice)
    {
        if (choice == null) return;
        if (choice.questAction == DialogueChoice.QuestChoiceAction.None) return;
        if (string.IsNullOrEmpty(choice.questId)) return;

        bool PassRequired()
        {
            var st = QuestSystem.GetState(choice.questId);
            switch (choice.requiredState)
            {
                case DialogueChoice.RequireState.Any: return true;
                case DialogueChoice.RequireState.NotAccepted: return st == QuestSystem.QState.NotAccepted;
                case DialogueChoice.RequireState.Active: return st == QuestSystem.QState.Active;
                case DialogueChoice.RequireState.Completed: return st == QuestSystem.QState.Completed;
                case DialogueChoice.RequireState.TurnedIn: return st == QuestSystem.QState.TurnedIn;
            }
            return true;
        }

        if (!PassRequired()) return;

        string giverId = currentNpcStableId;

        if (choice.questAction == DialogueChoice.QuestChoiceAction.Accept)
        {
            int need = Mathf.Max(1, choice.targetCount);
            QuestSystem.Accept(choice.questId, giverId, choice.targetItemId, need);
        }
        else if (choice.questAction == DialogueChoice.QuestChoiceAction.TurnIn)
        {
            QuestSystem.TurnIn(choice.questId, markNpcCompleted: true);
        }
    }

    private void ApplyChoiceToggles(DialogueChoice choice)
    {
        if (choice == null) return;

        if (choice.enableOnChoose != null)
            foreach (var go in choice.enableOnChoose)
                if (go && !go.activeSelf)
                {
                    go.SetActive(true);
                    DialogueRuntime.Instance?.RecordObjectActive(go, true);
                }

        if (choice.disableOnChoose != null)
            foreach (var go in choice.disableOnChoose)
                if (go && go.activeSelf)
                {
                    go.SetActive(false);
                    DialogueRuntime.Instance?.RecordObjectActive(go, false);
                }
    }

    private void GoNextNodeOrEnd()
    {
        if (!isBranchingMode) { EndDialogue(); return; }

        int next = currentNodeIndex + 1;
        if (currentDialogue != null && currentDialogue.nodes != null &&
            next < currentDialogue.nodes.Count)
            StartNode(next);
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        StopTypingSound();

        isDialogueActive = false;
        if (animator) animator.Play("hide");

        lockFacing = false;

        if (rb2d)
        {
            rb2d.constraints = savedConstraints;
            rb2d.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            if ((rb2d.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
                rb2d.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        RestorePlayer();
        StartCoroutine(RestorePlayerNextFrame());
        HideChoices();

        currentDialogue = null;
        currentNodeIndex = -1;
        currentNode = null;
        isBranchingMode = false;
        isTyping = false;
        currentFullLineText = "";
        currentNpcStableId = null;
    }

    private void StopTypingSound()
    {
        if (SoundManager.instance &&
            SoundManager.instance.effectSource.clip == SoundManager.instance.typing)
        {
            SoundManager.instance.effectSource.Stop();
            SoundManager.instance.effectSource.loop = false;
            SoundManager.instance.effectSource.clip = null;
        }
    }

    private void PrepareAndLockPlayer()
    {
        if (playerController)
        {
            prevPlayerControlEnabled = playerController.enabled;
            playerController.enabled = false;
        }
        if (playerInput)
        {
            prevPlayerInputEnabled = playerInput.enabled;
            playerInput.enabled = false;
        }
        CaptureFacing();
        ApplyFacingLock(true);
    }

    private void RestorePlayer()
    {
        if (rb2d)
        {
            rb2d.constraints = savedConstraints;
            rb2d.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            if ((rb2d.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
                rb2d.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
        if (playerController)
        {
            playerController.enabled = true;
            try { playerController.canMove = true; } catch { }
        }
        if (playerInput)
        {
            playerInput.enabled = true;
            try { playerInput.ActivateInput(); } catch { }
        }
    }

    private IEnumerator RestorePlayerNextFrame()
    {
        yield return null;
        if (rb2d)
        {
            rb2d.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            if ((rb2d.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
                rb2d.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
        if (playerController) playerController.enabled = true;
        if (playerInput)
        {
            playerInput.enabled = true;
            try { playerInput.ActivateInput(); } catch { }
        }
    }

    private void CaptureFacing()
    {
        if (spriteRenderers == null) return;
        savedFlipX.Clear();
        foreach (var sr in spriteRenderers)
            if (sr) savedFlipX[sr] = sr.flipX;

        Transform t = playerController
            ? playerController.transform
            : (spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].transform : null);

        if (t) savedScaleX = t.localScale.x;
    }

    private void ApplyFacingLock(bool enable)
    {
        lockFacing = enable;

        if (rb2d)
        {
            if (enable)
            {
                savedConstraints = rb2d.constraints;
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
                rb2d.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
            else rb2d.constraints = savedConstraints;
        }

        if (!enable) RestoreFacingOnce();
    }

    private void RestoreFacingOnce()
    {
        foreach (var kv in savedFlipX)
            if (kv.Key) kv.Key.flipX = kv.Value;

        Transform t = playerController
            ? playerController.transform
            : (spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].transform : null);

        if (t)
        {
            var s = t.localScale;
            t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        bool pressed =
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        if (!pressed) return;

        if (isTyping) { FastRevealCurrentLine(); return; }
        if (isShowingChoices) return;

        if (lines.Count > 0) { DisplayNextDialogueLine(); return; }

        if (isBranchingMode)
        {
            if (currentNode != null && currentNode.choices != null && currentNode.choices.Count > 0)
                ShowChoices(currentNode.choices);
            else
                GoNextNodeOrEnd();
        }
        else EndDialogue();
    }

    private void FastRevealCurrentLine()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        StopTypingSound();
        if (dialogueArea) dialogueArea.text = currentFullLineText;
        isTyping = false;
    }
}