using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class TriggerDialogue : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    [Header("Lines")]
    [TextArea(2, 5)]
    public string[] dialogues;

    [Header("Typing")]
    public float typeSpeed = 0.07f;
    public float waitBetweenLines = 1f;

    [Header("Play Once Per Save Slot")]
    public bool playOnce = true;

    private int currentIndex = 0;
    private Coroutine typingRoutine;
    private Collider2D col;

    [SerializeField, HideInInspector]
    private string uniqueId;

    private string StableId
    {
        get
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = Guid.NewGuid().ToString();
            return uniqueId;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = Guid.NewGuid().ToString();

            var all = UnityEngine.Object.FindObjectsOfType<TriggerDialogue>(true);
            foreach (var t in all)
            {
                if (t == this) continue;
                if (t.uniqueId == uniqueId)
                {
                    uniqueId = Guid.NewGuid().ToString();
                    break;
                }
            }
        }
    }
#endif

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (string.IsNullOrEmpty(uniqueId))
            uniqueId = Guid.NewGuid().ToString();
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
        if (!playOnce)
        {
            if (col) col.enabled = true;
            if (dialoguePanel) dialoguePanel.SetActive(false);
            currentIndex = 0;
            return;
        }

        bool completedInThisSlot =
            DialogueRuntime.Instance &&
            DialogueRuntime.Instance.IsNpcCompleted(StableId);

        if (completedInThisSlot)
        {
            if (col) col.enabled = false;
            if (dialoguePanel) dialoguePanel.SetActive(false);
        }
        else
        {
            if (col) col.enabled = true;
            if (dialoguePanel) dialoguePanel.SetActive(false);
            currentIndex = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (dialogues == null || dialogues.Length == 0) return;
        if (dialoguePanel == null || dialogueText == null) return;

        if (playOnce &&
            DialogueRuntime.Instance &&
            DialogueRuntime.Instance.IsNpcCompleted(StableId))
        {
            return;
        }

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
            if (playOnce)
            {
                DialogueRuntime.Instance?.MarkNpcCompleted(StableId);

                if (col) col.enabled = false;
            }

            if (dialoguePanel) dialoguePanel.SetActive(false);
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