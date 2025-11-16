using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RambleAbility : MonoBehaviour, AbilityManager
{
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference abilityAction;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float dashSpeed = 7f;

    [Header("Jump Charge")]
    public float minJumpForce = 0.1f;
    public float maxJumpForce = 8f;
    public float chargeTime = 1f;
    public Slider jumpChargeBar;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    private GameObject player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isDashing = false;
    private float dashDelayTimer = 0f;
    private float dashDelayDuration = 0.45f;
    private bool dashQueued = false;

    private bool isChargingJump = false;
    private float chargeTimer = 0f;

    private bool isGrounded = false;
    private bool wasGrounded = false;

    private PlayerControl originalControl;
    public DashManager dashManager;

    private AudioSource audioSource;
    private bool hasPlayedFall = false;

    private bool ControlsLocked()
    {
        bool dialogueLock = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        bool tutorialLock = TutorialManager.IsTutorialLockActive;
        bool uiLock = UIManager.IsUiMovementLocked;
        return dialogueLock || tutorialLock || uiLock;
    }

    private void HaltMotionAndFacing()
    {
        if (rb != null)
        {
            var v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
        }

        isDashing = false;
        dashQueued = false;

        isChargingJump = false;
        chargeTimer = 0f;

        if (dashManager != null) dashManager.Dash(false);

        if (animator != null)
        {
            animator.SetBool("walk", false);
            animator.SetBool("roll", false);
            animator.SetBool("chargeJump", false);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }

    public void OnAbilityEnter(GameObject playerObj)
    {
        player = playerObj;
        rb = player.GetComponent<Rigidbody2D>();
        spriteRenderer = player.GetComponent<SpriteRenderer>();
        animator = player.GetComponent<Animator>();

        originalControl = player.GetComponent<PlayerControl>();
        if (originalControl != null) originalControl.enabled = false;

        if (groundCheck == null)
            groundCheck = player.transform.Find("GroundCheck");

        audioSource = player.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = player.AddComponent<AudioSource>();

        moveAction.action.Enable();
        jumpAction.action.Enable();
        abilityAction.action.Enable();

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }

    public void OnAbilityExit(GameObject playerObj)
    {
        isDashing = false;

        isChargingJump = false;
        chargeTimer = 0f;

        if (originalControl != null)
            originalControl.enabled = true;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        abilityAction.action.Disable();

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }

    public void PlayRevertAnimation(GameObject playerObj) { }

    void Update()
    {
        if (player == null || rb == null) return;

        if (ControlsLocked())
        {
            HaltMotionAndFacing();
            return;
        }

        GroundCheck();
        Movement();
        Dash();
        ChargeJump();
        JumpChargeUI();
    }

    void GroundCheck()
    {
        wasGrounded = isGrounded;

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
            animator.SetBool("isGrounded", isGrounded);

            if (isGrounded && !hasPlayedFall)
            {
                SoundManager.instance.PlaySFX("FallRamble");
                hasPlayedFall = true;
            }
            else if (!isGrounded)
            {
                hasPlayedFall = false;
            }
        }
    }

    void Movement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float moveX = moveInput.x;
        float currentSpeed = isChargingJump ? moveSpeed * 0.5f : moveSpeed;

        var vel = rb.linearVelocity;
        vel.x = moveX * currentSpeed;
        rb.linearVelocity = vel;

        animator.SetBool("chargeJump", isChargingJump);
        animator.SetBool("walk", moveX != 0);

        if (moveX != 0)
        {
            if (!ControlsLocked())
            {
                spriteRenderer.flipX = moveX < 0;
            }

            if (!isDashing && isGrounded)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = SoundManager.instance.walkramble;
                    audioSource.loop = false;
                    audioSource.pitch = isChargingJump ? 0.8f : 1f;
                    audioSource.Play();
                }
                else
                {
                    audioSource.pitch = isChargingJump ? 0.8f : 1f;
                }
            }
        }
        else
        {
            if (audioSource.isPlaying && audioSource.clip == SoundManager.instance.walkramble)
            {
                audioSource.Stop();
            }
        }
    }

    void Dash()
    {
        if (ControlsLocked())
        {
            isDashing = false;
            dashQueued = false;
            animator.SetBool("roll", false);
            if (audioSource.isPlaying && audioSource.clip == SoundManager.instance.rollramble)
                audioSource.Stop();
            if (dashManager != null) dashManager.Dash(false);
            return;
        }

        if (abilityAction.action.triggered && !dashQueued)
        {
            isDashing = false;
            dashQueued = true;
            dashDelayTimer = dashDelayDuration;

            animator.SetBool("roll", true);

            if (audioSource.clip != SoundManager.instance.rollramble)
            {
                audioSource.clip = SoundManager.instance.rollramble;
                audioSource.loop = true;
                audioSource.pitch = 1f;
            }
        }

        if (!abilityAction.action.IsPressed())
        {
            dashQueued = false;
            isDashing = false;
            animator.SetBool("roll", false);

            if (audioSource.isPlaying && audioSource.clip == SoundManager.instance.rollramble)
                audioSource.Stop();
        }

        if (dashQueued)
        {
            dashDelayTimer -= Time.deltaTime;
            if (dashDelayTimer <= 0f)
            {
                isDashing = true;
            }
        }

        if (isDashing)
        {
            bool isFacingRight = !spriteRenderer.flipX;
            Vector2 dashDir = isFacingRight ? Vector2.right : Vector2.left;

            var vel = rb.linearVelocity;
            vel.x = dashDir.x * dashSpeed;
            rb.linearVelocity = vel;

            dashManager.Dash(true);

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            dashManager.Dash(false);
        }
    }

    void ChargeJump()
    {
        if (ControlsLocked())
        {
            isChargingJump = false;
            chargeTimer = 0f;
            return;
        }

        if (!isGrounded)
        {
            if (isChargingJump || chargeTimer > 0f)
            {
                isChargingJump = false;
                chargeTimer = 0f;
                if (jumpChargeBar != null)
                {
                    jumpChargeBar.value = 0f;
                    jumpChargeBar.gameObject.SetActive(false);
                }
            }
        }

        if (isGrounded && !wasGrounded && jumpAction.action.IsPressed())
        {
            isChargingJump = true;
            chargeTimer = 0f;
        }

        if (jumpAction.action.IsPressed() && isGrounded)
        {
            if (!isChargingJump)
                isChargingJump = true;

            chargeTimer += Time.deltaTime / chargeTime;
            chargeTimer = Mathf.Clamp01(chargeTimer);
        }

        if (!jumpAction.action.IsPressed() && isChargingJump && isGrounded)
        {
            float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, chargeTimer);

            var vel = rb.linearVelocity;
            vel.y = jumpPower;
            rb.linearVelocity = vel;

            animator.SetTrigger("jump");
            SoundManager.instance.PlaySFX("JumpRamble");

            isChargingJump = false;
            chargeTimer = 0f;

            if (jumpChargeBar != null)
            {
                jumpChargeBar.value = 0f;
                jumpChargeBar.gameObject.SetActive(false);
            }
        }
    }

    void JumpChargeUI()
    {
        if (jumpChargeBar == null) return;

        if (isChargingJump && isGrounded)
        {
            jumpChargeBar.gameObject.SetActive(true);
            jumpChargeBar.value = chargeTimer;
        }
        else
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }

    public bool IsDashing() => isDashing;

    public RambleSave BuildSave() => new RambleSave();

    public void ApplySave(RambleSave s)
    {
        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }
}