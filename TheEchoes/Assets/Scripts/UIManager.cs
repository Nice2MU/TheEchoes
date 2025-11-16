using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject initialPanel;
    [SerializeField] private GameObject pauseMenu;

    [Header("Control Scheme Selector")]
    [SerializeField] private GameObject controlSchemePanel;
    [SerializeField] private bool showControlSelectorOnStart = true;
    [SerializeField] private bool pauseWhileSelectingScheme = true;

    [Header("In-game UI")]
    [SerializeField] private GameObject playerui;
    [FormerlySerializedAs("extraUi")]
    [SerializeField] private GameObject mobileui;

    [SerializeField] private GameObject[] disallowPauseWhenActive;

    [Header("Pause Block Zones")]
    [SerializeField] private bool usePauseBlockZones = false;
    [SerializeField] private Transform pauseBlockCheckPoint;
    [SerializeField] private float pauseBlockCheckRadius = 0.3f;
    [SerializeField] private string pauseBlockTag = "PauseBlocked";

    [Header("UI Navigation (First Selected)")]
    [SerializeField] private GameObject initialFirstSelected;
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject controlSchemeFirstSelected;

    [Header("UI Input Actions")]
    [SerializeField] private InputActionReference uiCancelAction;

    private static AudioSource uiPointSource;
    private static AudioSource uiClickSource;
    private static bool uiMixerLinked = false;

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

    private bool wantMobileUi = false;
    private bool inGameplay = false;

    private bool isInPauseBlockedZone = false;
    public bool IsPauseBlockedByZone => isInPauseBlockedZone;

    private void Awake()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerRb = player.GetComponent<Rigidbody2D>();
            playerControl = player.GetComponent<PlayerControl>();
            spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);

            if (pauseBlockCheckPoint == null)
                pauseBlockCheckPoint = player.transform;
        }

        if (playerRb != null)
        {
            originalConstraints = playerRb.constraints;
            savedConstraintsForFacing = originalConstraints;
        }

        bool shouldLock = IsAnyUiOpen();
        IsUiMovementLocked = shouldLock;
        if (shouldLock) EngageUiLock();
    }

    private void OnEnable()
    {
        if (uiCancelAction != null)
        {
            uiCancelAction.action.performed += OnUiCancel;
        }
    }

    private void OnDisable()
    {
        if (uiCancelAction != null)
        {
            uiCancelAction.action.performed -= OnUiCancel;
        }
    }

    private void Start()
    {
        inGameplay = false;

        if (showControlSelectorOnStart && controlSchemePanel != null)
        {
            ShowOnly(controlSchemePanel);
            if (pauseWhileSelectingScheme) SetTimeScale(true);

            SafeSetActive(playerui, false);
            SafeSetActive(mobileui, false);

            SetSelected(controlSchemeFirstSelected);
        }
        else
        {
            if (initialPanel != null)
            {
                ShowOnly(initialPanel);
                SetSelected(initialFirstSelected);
            }

            SafeSetActive(mobileui, false);
        }

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (!pauseWhileSelectingScheme) SetTimeScale(false);
    }

    private void Update()
    {
        UpdatePauseBlockZoneState();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gamePaused)
            {
                Resume();
            }
            else
            {
                if (!IsAnyBlockingPanelOpen() && !isInPauseBlockedZone)
                {
                    Pause();
                }
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

    public void SelectMouseKeyboard() => ApplyControlScheme("Keyboard&Mouse");
    public void SelectGamepad() => ApplyControlScheme("Gamepad");
    public void SelectMobile() => ApplyControlScheme("Touch");

    private void ApplyControlScheme(string schemeName)
    {
        if (playerInput != null)
        {
            InputDevice[] devices = null;
            switch (schemeName)
            {
                case "Gamepad":
                    if (Gamepad.current != null) devices = new InputDevice[] { Gamepad.current };
                    break;
                case "Keyboard&Mouse":
                    var k = Keyboard.current; var m = Mouse.current;
                    if (k != null && m != null) devices = new InputDevice[] { k, m };
                    break;
                case "Touch":
                    if (Touchscreen.current != null) devices = new InputDevice[] { Touchscreen.current };
                    break;
            }

            if (devices != null && devices.Length > 0)
                playerInput.SwitchCurrentControlScheme(schemeName, devices);
            else
                playerInput.SwitchCurrentControlScheme(schemeName);
        }

        wantMobileUi = (schemeName == "Touch");
        SafeSetActive(mobileui, false);

        SafeSetActive(controlSchemePanel, false);
        if (pauseWhileSelectingScheme) SetTimeScale(false);

        if (initialPanel != null)
        {
            ShowOnly(initialPanel);
            SetSelected(initialFirstSelected);
        }
        else
        {
            EnterGameplay();
        }
    }

    public void ShowPanel(GameObject target)
    {
        if (target == null) return;

        if (current != null && current != target)
        {
            current.SetActive(false);
            history.Push(current);
        }

        current = target;
        current.SetActive(true);

        SetSelected(current);
    }

    public void Back()
    {
        if (history.Count == 0) return;

        if (current != null) current.SetActive(false);

        current = history.Pop();
        if (current != null) current.SetActive(true);

        SetSelected(current);
    }

    public void GoHome()
    {
        if (current != null) current.SetActive(false);
        history.Clear();

        if (initialPanel != null)
        {
            current = initialPanel;
            current.SetActive(true);
            SetSelected(initialFirstSelected);
        }
        else
        {
            current = null;
        }

        inGameplay = false;
    }

    private void ShowOnly(GameObject target)
    {
        if (target == null) return;

        var parent = target.transform.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                if (child == null) continue;
                child.gameObject.SetActive(false);
            }
        }

        target.SetActive(true);
        current = target;
        history.Clear();
    }

    public void Pause()
    {
        if (gamePaused)
        {
            Resume();
            return;
        }

        if (isInPauseBlockedZone) return;

        gamePaused = true;
        SetTimeScale(true);

        if (pauseMenu != null)
        {
            ShowPanel(pauseMenu);
            SetSelected(pauseFirstSelected);
        }

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

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ExitToHome()
    {
        gamePaused = false;
        SetTimeScale(false);

        SafeSetActive(pauseMenu, false);
        if (current == pauseMenu) current = null;

        GoHome();

        SetHudVisible(false);
        SetMobileUiVisible(false);

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

    public void EnterGameplay()
    {
        inGameplay = true;

        SafeSetActive(controlSchemePanel, false);
        SafeSetActive(pauseMenu, false);
        SafeSetActive(initialPanel, false);
        SafeSetActive(current, false);
        history.Clear();
        current = null;

        SetHudVisible(true);
        SetMobileUiVisible(wantMobileUi);

        SetTimeScale(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private bool IsAnyBlockingPanelOpen()
    {
        if (controlSchemePanel != null && controlSchemePanel.activeInHierarchy) return true;
        if (initialPanel != null && initialPanel.activeInHierarchy) return true;

        if (disallowPauseWhenActive != null)
        {
            foreach (var go in disallowPauseWhenActive)
            {
                if (go == null) continue;
                if (playerui != null && go == playerui) continue;
                if (mobileui != null && go == mobileui) continue;
                if (go.activeInHierarchy) return true;
            }
        }

        return false;
    }

    private bool IsAnyUiOpen()
    {
        if (controlSchemePanel != null && controlSchemePanel.activeInHierarchy) return true;
        if (pauseMenu != null && pauseMenu.activeInHierarchy) return true;
        if (initialPanel != null && initialPanel.activeInHierarchy) return true;

        if (disallowPauseWhenActive != null)
        {
            foreach (var go in disallowPauseWhenActive)
            {
                if (go == null) continue;
                if (playerui != null && go == playerui) continue;
                if (mobileui != null && go == mobileui) continue;
                if (go.activeInHierarchy) return true;
            }
        }

        return false;
    }

    private void SetTimeScale(bool paused) => Time.timeScale = paused ? 0 : 1;

    private static void SafeSetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

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
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            savedConstraintsForFacing = playerRb.constraints;
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
                playerRb.linearVelocity = Vector2.zero;
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
        if (!Mathf.Approximately(playerRb.linearVelocity.x, 0f))
            playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);
    }

    private void SetHudVisible(bool visible)
    {
        if (playerui == null) return;

        if (visible)
        {
            var t = playerui.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
            playerui.SetActive(true);
        }
        else
        {
            playerui.SetActive(false);
        }
    }

    private void SetMobileUiVisible(bool visible)
    {
        if (mobileui == null) return;

        if (visible)
        {
            if (!inGameplay) { mobileui.SetActive(false); return; }

            var t = mobileui.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
            mobileui.SetActive(true);
        }
        else
        {
            mobileui.SetActive(false);
        }
    }

    private void UpdatePauseBlockZoneState()
    {
        if (!usePauseBlockZones)
        {
            isInPauseBlockedZone = false;
            return;
        }

        if (pauseBlockCheckPoint == null)
        {
            isInPauseBlockedZone = false;
            return;
        }

        var hits = Physics2D.OverlapCircleAll(pauseBlockCheckPoint.position, pauseBlockCheckRadius);
        bool blocked = false;

        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.CompareTag(pauseBlockTag))
            {
                blocked = true;
                break;
            }
        }

        isInPauseBlockedZone = blocked;
    }

    private void OnDrawGizmosSelected()
    {
        if (!usePauseBlockZones) return;

        Transform checkPoint = pauseBlockCheckPoint;
#if UNITY_EDITOR
        if (checkPoint == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) checkPoint = player.transform;
        }
#endif
        if (checkPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(checkPoint.position, pauseBlockCheckRadius);
    }

    private void SetSelected(GameObject go)
    {
        if (EventSystem.current == null) return;

        GameObject target = null;

        if (go != null)
        {
            var selfSelectable = go.GetComponent<Selectable>();
            if (selfSelectable != null)
            {
                target = selfSelectable.gameObject;
            }
            else
            {
                var selectables = go.GetComponentsInChildren<Selectable>(true);
                if (selectables != null && selectables.Length > 0)
                {
                    target = selectables[0].gameObject;
                }
            }
        }

        if (target == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    private void OnUiCancel(InputAction.CallbackContext ctx)
    {
        if (!IsAnyUiOpen())
            return;

        if (gamePaused)
        {
            if (current != null && current != pauseMenu && history.Count > 0)
            {
                Back();
            }
            else
            {
                return;
            }
        }
        else
        {
            if (history.Count > 0)
            {
                Back();
            }
            else
            {
                GoHome();
            }
        }
    }

    private static void EnsureUiSoundSources(SoundManager sm)
    {
        if (uiPointSource != null && uiClickSource != null) return;

        var holder = GameObject.Find("__ButtonSoundPool__");
        if (holder == null)
        {
            holder = new GameObject("__ButtonSoundPool__");
            Object.DontDestroyOnLoad(holder);
        }

        if (uiPointSource == null)
        {
            uiPointSource = holder.AddComponent<AudioSource>();
            uiPointSource.playOnAwake = false;
        }

        if (uiClickSource == null)
        {
            uiClickSource = holder.AddComponent<AudioSource>();
            uiClickSource.playOnAwake = false;
        }

        if (!uiMixerLinked && sm != null && sm.uiSource != null)
        {
            var group = sm.uiSource.outputAudioMixerGroup;
            if (group != null)
            {
                uiPointSource.outputAudioMixerGroup = group;
                uiClickSource.outputAudioMixerGroup = group;
            }
            uiMixerLinked = true;
        }
    }

    public static void PlayUiPoint()
    {
        var sm = SoundManager.instance;
        if (sm == null || sm.point == null) return;

        EnsureUiSoundSources(sm);

        uiPointSource.Stop();
        uiPointSource.PlayOneShot(sm.point);
    }

    public static void PlayUiClick()
    {
        var sm = SoundManager.instance;
        if (sm == null || sm.click == null) return;

        EnsureUiSoundSources(sm);

        uiClickSource.Stop();
        uiClickSource.PlayOneShot(sm.click);
    }
}