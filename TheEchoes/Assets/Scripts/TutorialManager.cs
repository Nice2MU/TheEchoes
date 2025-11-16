using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static bool IsTutorialLockActive { get; private set; } = false;

    [System.Serializable]
    public class TutorialZone
    {
        public string tag;
        public int gifIndex;
        public float autoHideDelay = 5f;
    }

    [Header("UI Button")]
    public GameObject tutorialButton;

    [Header("Popup UI")]
    public GameObject tutorialPanel;
    public GameObject[] gifObjects;

    [Header("Tutorial Zones")]
    public List<TutorialZone> tutorialZones = new List<TutorialZone>();

    private PlayerInput playerInput;
    private Rigidbody2D playerRb;
    private PlayerControl playerControl;

    private bool inputWasEnabled = true;
    private bool controlWasEnabled = true;
    private RigidbodyConstraints2D originalConstraints;

    private bool lockFacing = false;
    private float savedScaleX = 1f;
    private SpriteRenderer[] spriteRenderers;
    private readonly Dictionary<SpriteRenderer, bool> savedFlipX = new Dictionary<SpriteRenderer, bool>();
    private RigidbodyConstraints2D savedConstraintsForFacing;

    private int currentGifIndex = -1;
    private bool isInShowZone = false;

    private readonly HashSet<string> autoShownTags = new HashSet<string>();
    private Coroutine autoHideRoutine;
    private float currentAutoHideDelay = 0f;

    private void Awake()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerRb = player.GetComponent<Rigidbody2D>();
            playerControl = player.GetComponent<PlayerControl>();
            spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (playerRb != null) originalConstraints = playerRb.constraints;

        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            IsTutorialLockActive = true;
            LockPlayer();
            CaptureFacing();
            ApplyFacingLock(true);
        }
    }

    private void Update()
    {
        if (lockFacing)
        {
            ForceFacing();
            ZeroOutHorizontalVelocity();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        foreach (var zone in tutorialZones)
        {
            if (collision.CompareTag(zone.tag))
            {
                isInShowZone = true;
                currentGifIndex = zone.gifIndex;
                currentAutoHideDelay = zone.autoHideDelay;

                if (!autoShownTags.Contains(zone.tag))
                {
                    AutoShowOnce(zone.gifIndex, zone.autoHideDelay);
                    autoShownTags.Add(zone.tag);
                }

                else
                {
                    if (tutorialPanel != null && !tutorialPanel.activeSelf && tutorialButton != null)
                        tutorialButton.SetActive(true);
                }

                return;
            }
        }

        if (collision.CompareTag("THide"))
        {
            isInShowZone = false;
            currentGifIndex = -1;

            CancelAutoHideIfAny();
            ForceHideAll();

            if (tutorialButton != null)
                tutorialButton.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (currentGifIndex >= 0 && currentGifIndex < gifObjects.Length)
        {
            ShowGif(currentGifIndex);
            if (tutorialButton != null) tutorialButton.SetActive(false);
            CancelAutoHideIfAny();
        }
    }

    public void HideTutorial()
    {
        ForceHideAll();

        if (isInShowZone && tutorialButton != null)
        {
            tutorialButton.SetActive(true);
        }
    }

    private void AutoShowOnce(int gifIndex, float delay)
    {
        CancelAutoHideIfAny();
        ShowGif(gifIndex);
        RestartAutoHide(delay);
    }

    private void ShowGif(int index)
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            IsTutorialLockActive = true;

            LockPlayer();
            CaptureFacing();
            ApplyFacingLock(true);
        }

        for (int i = 0; i < gifObjects.Length; i++)
            if (gifObjects[i] != null) gifObjects[i].SetActive(false);

        if (index >= 0 && index < gifObjects.Length && gifObjects[index] != null)
            gifObjects[index].SetActive(true);

        if (tutorialButton != null) tutorialButton.SetActive(false);
    }

    private void ForceHideAll()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            IsTutorialLockActive = false;

            ApplyFacingLock(false);
            UnlockPlayer();
        }

        for (int i = 0; i < gifObjects.Length; i++)
            if (gifObjects[i] != null) gifObjects[i].SetActive(false);
    }

    private void RestartAutoHide(float delay)
    {
        if (delay <= 0f) return;
        CancelAutoHideIfAny();
        autoHideRoutine = StartCoroutine(AutoHideAfterDelay(delay));
    }

    private void CancelAutoHideIfAny()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }
    }

    private IEnumerator AutoHideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideTutorial();
        autoHideRoutine = null;
    }

    private void LockPlayer()
    {
        if (tutorialPanel != null && !tutorialPanel.activeSelf) return;

        if (playerInput != null)
        {
            inputWasEnabled = playerInput.enabled;
            playerInput.enabled = false;
        }

        if (playerControl != null)
        {
            controlWasEnabled = playerControl.enabled;
            playerControl.enabled = false;
        }

        if (playerRb != null)
        {
            originalConstraints = playerRb.constraints;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void UnlockPlayer()
    {
        if (playerInput != null && inputWasEnabled)
            playerInput.enabled = true;

        if (playerControl != null && controlWasEnabled)
            playerControl.enabled = true;

        if (playerRb != null)
            playerRb.constraints = originalConstraints;
    }

    private void CaptureFacing()
    {
        if (spriteRenderers == null) return;

        savedFlipX.Clear();
        foreach (var sr in spriteRenderers)
            if (sr != null) savedFlipX[sr] = sr.flipX;

        Transform t = playerControl != null ? playerControl.transform :
            (spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].transform : null);

        if (t != null) savedScaleX = t.localScale.x;
    }

    private void ApplyFacingLock(bool enable)
    {
        lockFacing = enable;

        if (playerRb != null)
        {
            if (enable)
            {
                savedConstraintsForFacing = playerRb.constraints;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }

            else
            {
                playerRb.constraints = savedConstraintsForFacing;
            }
        }

        if (!enable) RestoreFacingOnce();
    }

    private void ForceFacing()
    {
        foreach (var kv in savedFlipX)
            if (kv.Key != null) kv.Key.flipX = kv.Value;

        Transform t = playerControl != null ? playerControl.transform :
            (spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].transform : null);

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
            if (kv.Key != null) kv.Key.flipX = kv.Value;

        Transform t = playerControl != null ? playerControl.transform :
            (spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].transform : null);

        if (t != null)
        {
            var s = t.localScale;
            t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void ZeroOutHorizontalVelocity()
    {
        if (playerRb == null) return;
        if (!Mathf.Approximately(playerRb.linearVelocity.x, 0f))
            playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);
    }
}