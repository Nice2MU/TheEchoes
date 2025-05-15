using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerControl : MonoBehaviour
{
    [Header("Setting Movement")]
    public float speed = 1.6f;
    public float minJumpForce = 0.1f;
    public float maxJumpForce = 8f;
    public float chargeTime = 1.5f;

    public Transform groundCheck;
    public Transform ceilingCheck;
    public Transform waterCheck;
    public LayerMask groundLayer;
    public LayerMask ceilingLayer;
    public LayerMask waterLayer;

    public Slider jumpChargeBar;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private bool isGrounded;
    private bool isSticking;
    private bool isInWater;
    private float jumpCharge;

    public bool canMove = true;

    private float walkTime = 0f;
    private bool isWalking = false;

    [Header("Slime Movement")]
    public float walkDelay = 0.35f;
    public bool enableWalkingEffect = true;

    private bool hasPlayedWalkSFX = false;
    private bool hasPlayedClimbSFX = false;
    private bool wasClimbingBeforeFall = false;
    private bool hasPlayedDrownSFX = false;

    [Header("Rolling Movement")]
    public float slideSpeed = 50f;
    public string slideTag = "SlideArea";
    public float smoothTime = 0.1f;
    public float maxSpeed = 3.5f;
    public float dragMultiplier = 0.1f;

    public float groundCheckRadius = 0.2f;
    private bool isSliding = false;
    private float currentSpeed = 0f;
    private float originalDrag;
    private float chargeDuration = 0f;
    private bool isChargingJump = false;
    private bool hasPlayedRollSFX = false;

    [Header("Climbing Settings")]
    public float jumpForce = 6f;
    public float climbSpeed = 0.3f;
    private float climbPosition;
    private float minHeight = -0.5f;
    private float maxHeight = 0.5f;

    private float vineGrabCooldown = 0.5f;

    private bool isClimbingSoundPlaying = false;
    private bool isOnVine = false;
    private bool canGrabVine = true;
    private Transform grabZone;
    private Rigidbody2D vineRigidbody;
    private DistanceJoint2D joint;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        joint = GetComponent<DistanceJoint2D>();
        joint.enabled = false;

        originalDrag = dragMultiplier;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.drag = originalDrag;
        rb.angularDrag = originalDrag;

        if (jumpChargeBar != null)
            jumpChargeBar.value = 0;
    }

    void Update()
    {
        if (!canMove) return;

        float moveX = Input.GetAxis("Horizontal");

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isInWater = Physics2D.OverlapPoint(waterCheck.position, waterLayer);

        if (isInWater && !hasPlayedDrownSFX)
        {
            SoundManager.instance?.PlaySFX("Drown");
            hasPlayedDrownSFX = true;
        }

        if (!isInWater)
        {
            hasPlayedDrownSFX = false;
        }

        if (isGrounded && !wasGrounded)
        {
            SoundManager.instance?.PlaySFX("Fall");
            wasClimbingBeforeFall = false;
        }

        if (isGrounded)
        {
            if (moveX != 0 && !Input.GetButton("Jump") && !isSliding)
            {
                if (enableWalkingEffect)
                {
                    walkTime += Time.deltaTime;
                    if (walkTime >= walkDelay)
                    {
                        isWalking = !isWalking;
                        walkTime = 0f;

                        if (isWalking && !hasPlayedWalkSFX)
                        {
                            SoundManager.instance?.PlaySFX("Walk");
                            hasPlayedWalkSFX = true;
                            Invoke(nameof(ResetWalkSFX), 0.3f);
                        }
                    }
                }

                else
                {
                    isWalking = true;

                    if (!hasPlayedWalkSFX)
                    {
                        SoundManager.instance?.PlaySFX("Walk");
                        hasPlayedWalkSFX = true;
                        Invoke(nameof(ResetWalkSFX), 0.3f);
                    }
                }
            }

            else
            {
                isWalking = false;
                walkTime = 0f;
            }
        }

        else
        {
            isWalking = true;
        }

        if (!isSticking && isWalking && !isSliding)
        {
            if (!Input.GetButton("Jump") || !isGrounded)
                rb.velocity = new Vector2(moveX * speed, rb.velocity.y);

            else
                rb.velocity = new Vector2(0, rb.velocity.y);
        }

        else if (isWalking && !isSliding)
        {
            rb.velocity = new Vector2(moveX * speed, 0f);

            if (!playerCollider.IsTouchingLayers(ceilingLayer))
            {
                isSticking = false;
                rb.gravityScale = 1f;
                animator.SetBool("stick", false);
                wasClimbingBeforeFall = true;
            }
        }

        if (moveX != 0)
            spriteRenderer.flipX = moveX < 0;

        animator.SetBool("walk", moveX != 0);
        animator.SetBool("isGrounded", isGrounded);

        if (isSliding)
        {
            rb.drag = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, slideSpeed, smoothTime * Time.deltaTime * slideSpeed);
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            rb.velocity = new Vector2(currentSpeed * Mathf.Sign(transform.localScale.x), rb.velocity.y);
            animator.SetBool("isSliding", true);

            if (!hasPlayedRollSFX)
            {
                SoundManager.instance?.PlaySFX("Roll");
                hasPlayedRollSFX = true;
                Invoke(nameof(ResetRollSFX), 0.4f);
            }

            if (Input.GetButton("Jump") && isGrounded)
            {
                chargeDuration += Time.deltaTime * chargeTime;
                chargeDuration = Mathf.Min(chargeDuration, 1f);
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);

                if (jumpChargeBar != null)
                    jumpChargeBar.value = chargeDuration;
            }

            else if (Input.GetButtonDown("Jump") && isGrounded)
            {
                chargeDuration = 0f;
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);
            }

            if (Input.GetButtonUp("Jump") && isChargingJump)
            {
                float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, chargeDuration);
                rb.velocity = new Vector2(rb.velocity.x, jumpPower);
                animator.SetTrigger("jump");

                if (chargeDuration <= 0.3f)
                    SoundManager.instance?.PlaySFX("SJump");

                else
                    SoundManager.instance?.PlaySFX("Jump");

                chargeDuration = 0f;
                isChargingJump = false;
                animator.SetBool("isChargingJump", false);

                if (jumpChargeBar != null)
                    jumpChargeBar.value = 0f;
            }
        }

        else
        {
            rb.drag = originalDrag;
            animator.SetBool("isSliding", false);
            currentSpeed = 0f;

            if (Input.GetButton("Jump") && isGrounded && !isSticking)
            {
                jumpCharge += Time.deltaTime / chargeTime;
                jumpCharge = Mathf.Clamp(jumpCharge, 0f, 1f);

                if (jumpChargeBar != null) jumpChargeBar.value = jumpCharge;
            }

            if (Input.GetButtonUp("Jump") && isGrounded && !isSticking)
            {
                float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCharge);
                rb.velocity = new Vector2(moveX * speed, jumpPower);
                animator.SetTrigger("jump");

                if (jumpCharge <= 0.3f)
                    SoundManager.instance?.PlaySFX("SJump");

                else
                    SoundManager.instance?.PlaySFX("Jump");

                jumpCharge = 0;

                if (jumpChargeBar != null) jumpChargeBar.value = 0;
            }
        }

        if (!isSticking && !isGrounded && playerCollider.IsTouchingLayers(ceilingLayer))
        {
            isSticking = true;
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            animator.SetBool("stick", true);
        }

        if (isSticking && moveX != 0)
        {
            if (Physics2D.OverlapCircle(ceilingCheck.position, 0.1f, ceilingLayer) && !hasPlayedClimbSFX)
            {
                SoundManager.instance?.PlaySFX("Climb");
                hasPlayedClimbSFX = true;
                Invoke(nameof(ResetClimbSFX), 0.4f);
            }
        }

        if (isSticking && Input.GetButtonDown("Jump"))
        {
            isSticking = false;
            rb.gravityScale = 1f;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            animator.SetBool("stick", false);

            wasClimbingBeforeFall = true;
        }

        if (!isGrounded && !isSticking)
        {
            rb.gravityScale = 1f;
        }

        if (isOnVine)
        {
            HandleClimbInput();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ReleaseVine();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(slideTag))
        {
            isSliding = true;
        }

        if (canGrabVine && other.CompareTag("GrabZone"))
        {
            grabZone = other.transform;
            vineRigidbody = grabZone.GetComponentInParent<Rigidbody2D>();

            joint.connectedBody = vineRigidbody;
            joint.autoConfigureConnectedAnchor = false;
            joint.autoConfigureDistance = false;

            Vector2 localHitPos = grabZone.InverseTransformPoint(transform.position);
            climbPosition = Mathf.Clamp(localHitPos.y, minHeight, maxHeight);

            Vector2 localAnchorInGrabZone = new Vector2(0, climbPosition);
            Vector2 localAnchorInVine = vineRigidbody.transform.InverseTransformPoint(grabZone.TransformPoint(localAnchorInGrabZone));

            joint.connectedAnchor = localAnchorInVine;
            joint.distance = 0f;

            joint.enabled = true;
            isOnVine = true;

            SoundManager.instance.PlaySFX("Grab");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(slideTag))
        {
            isSliding = false;
        }
    }

    private void HandleClimbInput()
    {
        float input = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(input) > 0.01f)
        {
            climbPosition += input * climbSpeed * Time.deltaTime;
            climbPosition = Mathf.Clamp(climbPosition, minHeight, maxHeight);

            Vector2 localAnchorInGrabZone = new Vector2(0, climbPosition);
            Vector2 localAnchorInVine = vineRigidbody.transform.InverseTransformPoint(grabZone.TransformPoint(localAnchorInGrabZone));

            joint.connectedAnchor = localAnchorInVine;

            if (!isClimbingSoundPlaying)
            {
                SoundManager.instance.PlaySFX("Climb");
                isClimbingSoundPlaying = true;
            }
        }

        else
        {
            if (isClimbingSoundPlaying)
            {
                isClimbingSoundPlaying = false;
            }
        }
    }

    private void ReleaseVine()
    {
        joint.enabled = false;
        isOnVine = false;
        grabZone = null;
        vineRigidbody = null;

        StartCoroutine(VineGrabCooldownRoutine());

        rb.velocity = new Vector2(rb.velocity.x, 0);

        SoundManager.instance.PlaySFX("Jump");

        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    private IEnumerator VineGrabCooldownRoutine()
    {
        canGrabVine = false;
        yield return new WaitForSeconds(vineGrabCooldown);
        canGrabVine = true;
    }

    private void ResetWalkSFX() => hasPlayedWalkSFX = false;
    private void ResetClimbSFX() => hasPlayedClimbSFX = false;
    private void ResetRollSFX() => hasPlayedRollSFX = false;
}