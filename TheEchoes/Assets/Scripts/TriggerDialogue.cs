using System.Collections;
using UnityEngine;
using TMPro;

public class TriggerDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    [TextArea(2, 5)]
    public string[] dialogues;

    public float typeSpeed = 0.07f;
    public float waitBetweenLines = 1f;
    public bool playOnce = true;

    private int currentIndex = 0;
    private Coroutine typingRoutine;
    private Collider2D col;

    private string StableId => DialogueIdUtil.GetStableId(gameObject);

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        RecheckAgainstRuntime();
        GameSaveManager.OnSlotApplied += OnSlotApplied;
    }

    private void OnDisable()
    {
        GameSaveManager.OnSlotApplied -= OnSlotApplied;
    }

    private void OnSlotApplied()
    {
        RecheckAgainstRuntime();
    }

    private void RecheckAgainstRuntime()
    {
        bool completedInThisSlot = DialogueRuntime.Instance &&
                                   DialogueRuntime.Instance.IsNpcCompleted(StableId);

        if (completedInThisSlot)
        {
            if (col) col.enabled = false;
            if (dialoguePanel) dialoguePanel.SetActive(false);
        }
        else
        {
            if (col) col.enabled = true;
            currentIndex = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (dialogues == null || dialogues.Length == 0) return;
        if (dialoguePanel == null || dialogueText == null) return;

        if (DialogueRuntime.Instance && DialogueRuntime.Instance.IsNpcCompleted(StableId))
            return;

        dialoguePanel.SetActive(true);
        currentIndex = 0;
        StartTyping(dialogues[currentIndex]);
    }

    private void StartTyping(string line)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";

        if (SoundManager.instance)
        {
            SoundManager.instance.effectSource.loop = true;
            SoundManager.instance.effectSource.clip = SoundManager.instance.typing;
            SoundManager.instance.effectSource.Play();
        }

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        StopTypingSound();

        yield return new WaitForSeconds(waitBetweenLines);
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        currentIndex++;
        if (currentIndex < dialogues.Length)
        {
            StartTyping(dialogues[currentIndex]);
        }
        else
        {
            DialogueRuntime.Instance?.MarkNpcCompleted(StableId);

            if (dialoguePanel) dialoguePanel.SetActive(false);

            if (col) col.enabled = false;
        }
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
}