using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject initialPanel;
    [SerializeField] private GameObject pauseMenu;

    [Header("In-game UI")]
    [SerializeField] private GameObject playerui;

    [FormerlySerializedAs("extraUi")]
    [SerializeField] private GameObject mobileui;

    [FormerlySerializedAs("extraUiMobileOnly")]
    [SerializeField] private bool mobileUiMobileOnly = true;

    [SerializeField] private GameObject[] disallowPauseWhenActive;

    private bool savedPlayerUiState;
    private bool savedMobileUiState;

    public static bool IsUiMovementLocked { get; private set; }

    private GameObject current;
    private readonly Stack<GameObject> history = new();
    private bool gamePaused = false;

    private PlayerInput playerInput;
    private Rigidbody2D playerRb;
    private PlayerControl playerControl;
    private SpriteRenderer[] spriteRenderers;

    private bool inputWasEnabled = true;
    private bool controlWasEnabled = true;
    private RigidbodyConstraints2D originalConstraints;
    private RigidbodyConstraints2D savedConstraintsForFacing;

    private bool uiLockActive = false;
    private bool lockFacing = false;
    private float savedScaleX = 1f;
    private readonly Dictionary<SpriteRenderer, bool> savedFlipX = new();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern bool IsMobile();
#endif

    private bool isMobileUiAllowed;

    private void Awake()
    {
        isMobileUiAllowed = !mobileUiMobileOnly || IsMobileBrowser();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerRb = player.GetComponent<Rigidbody2D>();
            playerControl = player.GetComponent<PlayerControl>();
            spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (playerRb != null) originalConstraints = playerRb.constraints;

        bool shouldLock = IsAnyUiOpen();
        IsUiMovementLocked = shouldLock;
        if (shouldLock) EngageUiLock();

        if (mobileui != null && !isMobileUiAllowed)
            mobileui.SetActive(false);
    }

    private void Start()
    {
        if (initialPanel != null) ShowOnly(initialPanel);
        if (pauseMenu != null) pauseMenu.SetActive(false);

        SetTimeScale(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!IsAnyBlockingPanelOpen())
            {
                if (!gamePaused) Pause();

                else Resume();
            }
        }

        bool shouldLock = IsAnyUiOpen();
        IsUiMovementLocked = shouldLock;

        if (shouldLock && !uiLockActive) EngageUiLock();

        else if (!shouldLock && uiLockActive) ReleaseUiLock();

        if (lockFacing)
        {
            ForceFacing();
            ZeroOutHorizontalVelocity();
        }
    }

    public void ShowPanel(GameObject target)
    {
        if (target == null) return;

        if (target == current)
        {
            if (!current.activeInHierarchy) current.SetActive(true);
            return;
        }

        if (current != null)
        {
            current.SetActive(false);
            history.Push(current);
        }

        current = target;
        current.SetActive(true);
    }

    public void Back()
    {
        if (history.Count == 0) return;

        if (current != null) current.SetActive(false);

        current = history.Pop();
        if (current != null) current.SetActive(true);
    }

    public void GoHome()
    {
        if (current != null) current.SetActive(false);
        history.Clear();

        if (initialPanel != null)
        {
            current = initialPanel;
            current.SetActive(true);
        }

        else
        {
            current = null;
        }
    }

    private void ShowOnly(GameObject target)
    {
        if (target == null) return;

        var parent = target.transform.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
                if (child != null) child.gameObject.SetActive(false);
        }

        target.SetActive(true);
        current = target;
        history.Clear();
    }

    public void Pause()
    {
        if (gamePaused) return;
        gamePaused = true;
        SetTimeScale(true);

        if (pauseMenu != null) ShowPanel(pauseMenu);

        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(true);
    }

    public void Resume()
    {
        if (!gamePaused) return;
        gamePaused = false;
        SetTimeScale(false);

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            if (current == pauseMenu) current = null;
        }

        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(false);
    }

    public void ExitToHome()
    {
        gamePaused = false;
        SetTimeScale(false);

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (current == pauseMenu) current = null;

        GoHome();

        if (SoundManager.instance != null)
            SoundManager.instance.MuteAll(false);
    }

    public void OpenPauseSubPanel(GameObject targetPanel)
    {
        if (!gamePaused) Pause();
        if (targetPanel == null) return;
        ShowPanel(targetPanel);
    }

    public void Continue() => Resume();
    public void EnterGameplay() => current = null;

    private bool IsAnyBlockingPanelOpen()
    {
        if (disallowPauseWhenActive == null) return false;
        foreach (var go in disallowPauseWhenActive)
            if (go != null && go.activeInHierarchy) return true;

        return false;
    }

    private bool IsAnyUiOpen()
    {
        if (disallowPauseWhenActive != null)
        {
            foreach (var go in disallowPauseWhenActive)
                if (go != null && go.activeInHierarchy) return true;
        }

        return false;
    }

    private void SetTimeScale(bool paused) => Time.timeScale = paused ? 0 : 1;

    private void EngageUiLock()
    {
        uiLockActive = true;

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
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            savedConstraintsForFacing = playerRb.constraints;
        }

        if (playerui != null)
        {
            savedPlayerUiState = playerui.activeSelf;
            if (playerui.activeSelf) playerui.SetActive(false);
        }

        if (mobileui != null && isMobileUiAllowed)
        {
            savedMobileUiState = mobileui.activeSelf;
            if (mobileui.activeSelf) mobileui.SetActive(false);
        }

        CaptureFacing();
        ApplyFacingLock(true);
    }

    private void ReleaseUiLock()
    {
        uiLockActive = false;
        IsUiMovementLocked = false;

        ApplyFacingLock(false);

        if (playerRb != null)
            playerRb.constraints = originalConstraints;

        if (playerInput != null && inputWasEnabled)
            playerInput.enabled = true;

        if (playerControl != null && controlWasEnabled)
            playerControl.enabled = true;

        if (playerui != null)
            playerui.SetActive(savedPlayerUiState);

        if (mobileui != null && isMobileUiAllowed)
            mobileui.SetActive(savedMobileUiState);

        else if (mobileui != null && !isMobileUiAllowed)
            mobileui.SetActive(false);
    }

    private void CaptureFacing()
    {
        if (playerControl != null)
        {
            var t = playerControl.transform;
            savedScaleX = t.localScale.x;
        }

        if (spriteRenderers == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        }

        savedFlipX.Clear();
        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers)
                if (sr != null) savedFlipX[sr] = sr.flipX;
        }
    }

    private void ApplyFacingLock(bool enable)
    {
        lockFacing = enable;

        if (playerRb != null)
        {
            if (enable)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }

            else
            {
                playerRb.constraints = savedConstraintsForFacing;
                RestoreFacingOnce();
            }
        }

        else if (!enable)
        {
            RestoreFacingOnce();
        }
    }

    private void ForceFacing()
    {
        if (spriteRenderers != null)
        {
            foreach (var kv in savedFlipX)
                if (kv.Key != null) kv.Key.flipX = kv.Value;
        }

        if (playerControl != null)
        {
            var t = playerControl.transform;
            var s = t.localScale;
            if (!Mathf.Approximately(s.x, savedScaleX))
                t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void RestoreFacingOnce()
    {
        if (spriteRenderers != null)
        {
            foreach (var kv in savedFlipX)
                if (kv.Key != null) kv.Key.flipX = kv.Value;
        }

        if (playerControl != null)
        {
            var t = playerControl.transform;
            var s = t.localScale;
            t.localScale = new Vector3(savedScaleX, s.y, s.z);
        }
    }

    private void ZeroOutHorizontalVelocity()
    {
        if (playerRb == null) return;
        if (!Mathf.Approximately(playerRb.velocity.x, 0f))
            playerRb.velocity = new Vector2(0f, playerRb.velocity.y);
    }

    private bool IsMobileBrowser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return IsMobile(); }
        catch { /* เผื่อ template ไม่มีฟังก์ชัน */ }
#endif
        bool heuristic =
            Application.isMobilePlatform ||
            Input.touchSupported ||
            Mathf.Min(Screen.width, Screen.height) <= 900;

        return heuristic;
    }
}