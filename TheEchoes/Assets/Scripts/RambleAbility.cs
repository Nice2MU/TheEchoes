using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RambleAbility : MonoBehaviour, IMorphAbility
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float dashSpeed = 7f;

    [Header("Jump Charge")]
    public float minJumpForce = 1f;
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
    private bool isChargingJump = false;
    private float chargeTimer = 0f;
    private bool isGrounded = false;

    private PlayerControl originalControl;

    public DashHandler dashHandler;

    public void OnMorphEnter(GameObject playerObj)
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
    }

    public void OnMorphExit(GameObject playerObj)
    {
        isDashing = false;
        isChargingJump = false;
        chargeTimer = 0f;

        if (originalControl != null)
        {
            originalControl.enabled = true;
        }
    }

    public void PlayRevertAnimation(GameObject playerObj)
    {

    }

    void Update()
    {
        if (player == null || rb == null) return;

        HandleGroundCheck();
        HandleMovement();
        HandleDash();
        HandleChargeJump();
        HandleJumpChargeUI();
    }

    void HandleGroundCheck()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = isChargingJump ? moveSpeed * 0.5f : moveSpeed;
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        animator.SetBool("chargeJump", isChargingJump);
        animator.SetBool("walk", moveInput != 0);

        if (moveInput != 0)
        {
            spriteRenderer.flipX = moveInput < 0;
        }
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            isDashing = true;
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            isDashing = false;
        }

        animator.SetBool("roll", isDashing);

        if (isDashing)
        {
            bool isFacingRight = !spriteRenderer.flipX;
            Vector2 dashDir = isFacingRight ? Vector2.right : Vector2.left;
            rb.velocity = new Vector2(dashDir.x * dashSpeed, rb.velocity.y);

            dashHandler.HandleDash(isDashing);
        }
    }

    void HandleChargeJump()
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

            isChargingJump = false;
            chargeTimer = 0f;
        }
    }

    void HandleJumpChargeUI()
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