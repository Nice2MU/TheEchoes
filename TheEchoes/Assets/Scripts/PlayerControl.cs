using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    public float speed = 2f;
    public float minJumpForce = 1f;
    public float maxJumpForce = 7f;
    public float chargeTime = 1f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask ceilingLayer;
    public Slider jumpChargeBar;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private bool isGrounded;
    private bool isSticking;
    private float jumpCharge;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (jumpChargeBar != null)
            jumpChargeBar.value = 0;
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        if (!isSticking)
        {
            if (!Input.GetButton("Jump") || !isGrounded)
            {
                rb.velocity = new Vector2(moveX * speed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        else
        {
            rb.velocity = new Vector2(moveX * speed, 0f);

            if (!playerCollider.IsTouchingLayers(ceilingLayer))
            {
                isSticking = false;
                rb.gravityScale = 1f;
                animator.SetBool("stick", false);
            }
        }

        animator.SetBool("walk", moveX != 0);
        animator.SetBool("isGrounded", isGrounded);

        if (Input.GetButton("Jump") && isGrounded && !isSticking)
        {
            jumpCharge += Time.deltaTime / chargeTime;
            jumpCharge = Mathf.Clamp(jumpCharge, 0f, 1f);

            if (jumpChargeBar != null)
                jumpChargeBar.value = jumpCharge;
        }

        if (Input.GetButtonUp("Jump") && isGrounded && !isSticking)
        {
            float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCharge);
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
            animator.SetTrigger("jump");

            jumpCharge = 0;
            if (jumpChargeBar != null)
                jumpChargeBar.value = 0;
        }

        if (!isSticking && !isGrounded && playerCollider.IsTouchingLayers(ceilingLayer))
        {
            isSticking = true;
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            animator.SetBool("stick", true);
        }

        if (isSticking && Input.GetButtonDown("Jump"))
        {
            isSticking = false;
            rb.gravityScale = 1f;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            animator.SetBool("stick", false);
        }

        if (!isGrounded && !isSticking)
        {
            rb.gravityScale = 1f;
        }
    }
}