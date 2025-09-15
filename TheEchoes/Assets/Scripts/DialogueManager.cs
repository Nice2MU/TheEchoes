using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    private Queue<DialogueLine> lines;

    public bool isDialogueActive = false;
    public float typingSpeed = 0.07f;

    public Animator animator;

    private PlayerControl playerController;
    private PlayerInput playerInput;
    private Rigidbody2D rb2d;
    private SpriteRenderer[] spriteRenderers;

    public Button skipButton;

    private bool lockFacing = false;
    private float savedScaleX = 1f;
    private Dictionary<SpriteRenderer, bool> savedFlipX = new Dictionary<SpriteRenderer, bool>();
    private RigidbodyConstraints2D savedConstraints;

    private bool prevPlayerControlEnabled = true;
    private bool prevPlayerInputEnabled = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        lines = new Queue<DialogueLine>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerControl>();
            playerInput = player.GetComponent<PlayerInput>();
            rb2d = player.GetComponent<Rigidbody2D>();
            spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipDialogue);
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        animator.Play("show");

        lines.Clear();
        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        if (playerController != null)
        {
            prevPlayerControlEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (playerInput != null)
        {
            prevPlayerInputEnabled = playerInput.enabled;
            playerInput.enabled = false;
        }

        CaptureFacing();
        ApplyFacingLock(true);

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            StopTypingSound();
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.icon;
        characterName.text = currentLine.character.name;

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueArea.text = "";

        if (SoundManager.instance != null)
        {
            SoundManager.instance.effectSource.loop = true;
            SoundManager.instance.effectSource.clip = SoundManager.instance.typing;
            SoundManager.instance.effectSource.Play();
        }

        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        StopTypingSound();
    }

    private void StopTypingSound()
    {
        if (SoundManager.instance != null &&
            SoundManager.instance.effectSource.clip == SoundManager.instance.typing)
        {
            SoundManager.instance.effectSource.Stop();
            SoundManager.instance.effectSource.loop = false;
            SoundManager.instance.effectSource.clip = null;
        }
    }

    void EndDialogue()
    {
        StopTypingSound();

        isDialogueActive = false;
        animator.Play("hide");

        ApplyFacingLock(false);

        if (playerController != null)
        {
            playerController.enabled = prevPlayerControlEnabled;
        }

        if (playerInput != null)
        {
            playerInput.enabled = prevPlayerInputEnabled;
        }
    }

    public void SkipDialogue()
    {
        StopAllCoroutines();
        StopTypingSound();
        dialogueArea.text = "";
        lines.Clear();

        EndDialogue();
    }

    private void Update()
    {
        if (lockFacing)
        {
            ForceFacing();
            ZeroOutHorizontalVelocity();
        }

        if (isDialogueActive && Input.GetKeyDown(KeyCode.F))
        {
            SkipDialogue();
        }
    }

    private void CaptureFacing()
    {
        if (spriteRenderers == null) return;

        savedFlipX.Clear();
        foreach (var sr in spriteRenderers)
        {
            if (sr != null) savedFlipX[sr] = sr.flipX;
        }

        Transform t = null;
        if (playerController != null) t = playerController.transform;
        else if (spriteRenderers != null && spriteRenderers.Length > 0) t = spriteRenderers[0].transform;

        if (t != null) savedScaleX = t.localScale.x;
    }

    private void ApplyFacingLock(bool enable)
    {
        lockFacing = enable;

        if (rb2d != null)
        {
            if (enable)
            {
                savedConstraints = rb2d.constraints;
                rb2d.velocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
                rb2d.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }

            else
            {
                rb2d.constraints = savedConstraints;
            }
        }

        if (!enable)
        {
            RestoreFacingOnce();
        }
    }

    private void ForceFacing()
    {
        foreach (var kv in savedFlipX)
        {
            if (kv.Key != null) kv.Key.flipX = kv.Value;
        }

        Transform t = null;
        if (playerController != null) t = playerController.transform;
        else if (spriteRenderers != null && spriteRenderers.Length > 0) t = spriteRenderers[0].transform;

        if (t != null)
        {
            var s = t.localScale;
            if (!Mathf.Approximately(s.x, savedScaleX))
                t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void RestoreFacingOnce()
    {
        foreach (var kv in savedFlipX)
        {
            if (kv.Key != null) kv.Key.flipX = kv.Value;
        }

        Transform t = null;
        if (playerController != null) t = playerController.transform;
        else if (spriteRenderers != null && spriteRenderers.Length > 0) t = spriteRenderers[0].transform;

        if (t != null)
        {
            var s = t.localScale;
            t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void ZeroOutHorizontalVelocity()
    {
        if (rb2d == null) return;
        if (!Mathf.Approximately(rb2d.velocity.x, 0f))
        {
            rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
        }
    }
}