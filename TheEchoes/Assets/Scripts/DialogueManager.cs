using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

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

    public Button skipButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        lines = new Queue<DialogueLine>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerControl>();
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
            playerController.canMove = false;
        }

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

        if (playerController != null)
        {
            playerController.canMove = true;
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
        if (isDialogueActive && Input.GetKeyDown(KeyCode.F))
        {
            SkipDialogue();
        }
    }
}