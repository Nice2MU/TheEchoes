using UnityEngine;
using UnityEngine.UI;

public class RambleAbility : MonoBehaviour, AbilityManager
{
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

    private PlayerControl originalControl;

    public DashManager dashManager;

    private AudioSource audioSource;
    private bool hasPlayedFall = false;

    public void OnAbilityEnter(GameObject playerObj)
    {
        player = playerObj;
        rb = player.GetComponent<Rigidbody2D>();
        spriteRenderer = player.GetComponent<SpriteRenderer>();
        animator = player.GetComponent<Animator>();

        originalControl = player.GetComponent<PlayerControl>();

        if (originalControl != null)
        {
            originalControl.enabled = false;
        }

        if (groundCheck == null)
            groundCheck = player.transform.Find("GroundCheck");

        audioSource = player.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = player.AddComponent<AudioSource>();
    }

    public void OnAbilityExit(GameObject playerObj)
    {
        isDashing = false;
        isChargingJump = false;
        chargeTimer = 0f;

        if (originalControl != null)
        {
            originalControl.enabled = true;
        }
    }

    public void PlayRevertAnimation(GameObject playerObj) { }

    void Update()
    {
        if (player == null || rb == null) return;

        GroundCheck();
        Movement();
        Dash();
        ChargeJump();
        JumpChargeUI();
    }

    void GroundCheck()
    {
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
        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = isChargingJump ? moveSpeed * 0.5f : moveSpeed;
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        animator.SetBool("chargeJump", isChargingJump);
        animator.SetBool("walk", moveInput != 0);

        if (moveInput != 0)
        {
            spriteRenderer.flipX = moveInput < 0;

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
        if (Input.GetKeyDown(KeyCode.E))
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

        if (Input.GetKeyUp(KeyCode.E))
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
            rb.velocity = new Vector2(dashDir.x * dashSpeed, rb.velocity.y);

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
        if (Input.GetKey(KeyCode.Space) && isGrounded)
        {
            isChargingJump = true;
            chargeTimer += Time.deltaTime / chargeTime;
            chargeTimer = Mathf.Clamp01(chargeTimer);
        }

        if (Input.GetKeyUp(KeyCode.Space) && isChargingJump && isGrounded)
        {
            float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, chargeTimer);
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
            animator.SetTrigger("jump");

            SoundManager.instance.PlaySFX("JumpRamble");

            isChargingJump = false;
            chargeTimer = 0f;
        }
    }

    void JumpChargeUI()
    {
        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = isChargingJump ? chargeTimer : 0f;
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}