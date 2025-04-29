using UnityEngine;
using UnityEngine.UI;

public class RollingPlayer : MonoBehaviour
{
    public float slideSpeed = 50f;
    public string slideTag = "SlideArea";
    public float smoothTime = 0.1f;
    public float maxSpeed = 4f;
    public float dragMultiplier = 0.1f;

    public float minJumpForce = 1f;
    public float maxJumpForce = 7f;
    public float chargeTime = 1f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    public Slider jumpChargeBar;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isSliding = false;
    private float currentSpeed = 0f;
    private bool isGrounded = false;

    private float originalDrag;
    private bool wasSliding = false;

    private float chargeDuration = 0f;
    private bool isChargingJump = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        originalDrag = dragMultiplier;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.drag = originalDrag;
        rb.angularDrag = originalDrag;

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
        }
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isSliding)
        {
            rb.drag = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, slideSpeed, smoothTime * Time.fixedDeltaTime * slideSpeed);
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            rb.velocity = new Vector2(currentSpeed * Mathf.Sign(transform.localScale.x), rb.velocity.y);

            animator.SetBool("isSliding", true);

            if (Input.GetButton("Jump") && isGrounded)
            {
                chargeDuration += Time.fixedDeltaTime * chargeTime;
                chargeDuration = Mathf.Min(chargeDuration, 1f);
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);

                if (jumpChargeBar != null)
                {
                    jumpChargeBar.value = chargeDuration;
                }
            }
            else if (Input.GetButtonDown("Jump") && isGrounded)
            {
                chargeDuration = 0f;
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);
            }

            if (Input.GetButtonUp("Jump") && isChargingJump)
            {
                Jump();
                chargeDuration = 0f;
                isChargingJump = false;
                animator.SetBool("isChargingJump", false);

                if (jumpChargeBar != null)
                {
                    jumpChargeBar.value = 0f;
                }
            }
        }
        else
        {
            rb.drag = originalDrag;
            animator.SetBool("isSliding", false);
            currentSpeed = 0f;
        }
        wasSliding = isSliding;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(slideTag))
        {
            isSliding = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(slideTag))
        {
            isSliding = false;
        }
    }

    private void Jump()
    {
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargeDuration);

        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetTrigger("Jump");
    }
}