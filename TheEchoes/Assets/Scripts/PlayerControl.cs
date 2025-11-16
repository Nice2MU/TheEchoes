using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

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

    [Header("Slime Movement (unused)")]
    public float walkDelay = 0.35f;
    public bool enableWalkingEffect = true;

    private bool hasPlayedWalkSFX = false;
    private bool hasPlayedClimbSFX = false;
    private bool hasPlayedDrownSFX = false;

    [Header("Rolling Movement")]
    public float slideSpeed = 50f;
    public string slideTag = "Slide";
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
    public string rollTriggerTag = "RollTrigger";
    public float rollTriggerTimeWindow = 3f;

    private bool hasRollTrigger = false;
    private float rollTriggerTimer = 0f;
    private bool isInsideSlideZone = false;

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

    private float stickCooldown = 0.2f;
    private float stickCooldownTimer = 0f;

    private Transform grabZone;
    private Rigidbody2D vineRigidbody;
    private DistanceJoint2D joint;

    private Vector2 moveInput = Vector2.zero;
    private bool jumpHeld = false;
    private bool jumpReleased = false;
    private bool jumpDownThisFrame = false;

    [Header("Ceiling Stick")]
    [SerializeField] private float ceilingStickRadius = 0.22f;
    [SerializeField] private float ceilingCrawlSpeed = 1.6f;
    [SerializeField] private float ceilingCrawlAccel = 20f;
    [SerializeField] private float ceilingSnapMaxDistance = 0.35f;
    [SerializeField] private float ceilingSnapEpsilon = 0.01f;

    [Header("Jump Tuning")]
    [Range(0f, 3f)] public float jumpForwardMultiplier = 1.6f;

    public float airLaunchHoldTime = 0.15f;
    private float airLaunchTimer = 0f;

    [Header("Input")]
    public float inputDeadzone = 0.02f;

    private bool _externalMoveLock = false;

    [Header("Charge Jump Cooldown")]
    public float chargeJumpCooldown = 0.5f;
    private float chargeJumpCDTimer = 0f;

    public bool IsSticking => isSticking;
    public bool IsOnVine => isOnVine;
    public bool CanMove => canMove;

    public void SetExternalMovementLock(bool active) => _externalMoveLock = active;

    void Awake()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();

        moveAction.action.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.action.canceled += ctx => moveInput = Vector2.zero;

        jumpAction.action.performed += ctx =>
        {
            jumpHeld = true;
            jumpDownThisFrame = true;
        };

        jumpAction.action.canceled += ctx =>
        {
            jumpHeld = false;
            jumpReleased = true;
        };
    }

    void Start()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        joint = GetComponent<DistanceJoint2D>();
        if (joint != null) joint.enabled = false;

        originalDrag = dragMultiplier;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalDrag;

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!canMove)
        {
            jumpDownThisFrame = false;
            jumpReleased = false;
            return;
        }

        if (chargeJumpCDTimer > 0f)
            chargeJumpCDTimer -= Time.deltaTime;

        if (hasRollTrigger)
        {
            rollTriggerTimer -= Time.deltaTime;
            if (rollTriggerTimer <= 0f)
                hasRollTrigger = false;
        }

        float moveX = moveInput.x;

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isInWater = Physics2D.OverlapPoint(waterCheck.position, waterLayer);

        if (isInWater && !hasPlayedDrownSFX)
        {
            SoundManager.instance?.PlaySFX("Drown");
            hasPlayedDrownSFX = true;
        }
        if (!isInWater) hasPlayedDrownSFX = false;

        if (isGrounded && !wasGrounded)
            SoundManager.instance?.PlaySFX("Fall");

        if (Mathf.Abs(moveX) > inputDeadzone)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = moveX < 0;
        }

        animator.SetBool("isGrounded", isGrounded);

        bool canRollNow = hasRollTrigger && (rollTriggerTimer > 0f);
        if (isInsideSlideZone && canRollNow)
        {
            isSliding = true;
        }
        else
        {
            if (!isInsideSlideZone || !canRollNow)
                isSliding = false;
        }

        if (isSliding)
        {
            rb.linearDamping = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, slideSpeed, smoothTime * Time.deltaTime * slideSpeed);
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            rb.linearVelocity = new Vector2(currentSpeed * Mathf.Sign(transform.localScale.x), rb.linearVelocity.y);

            animator.SetBool("isSliding", true);

            if (!hasPlayedRollSFX)
            {
                SoundManager.instance?.PlaySFX("Roll");
                hasPlayedRollSFX = true;
                Invoke(nameof(ResetRollSFX), 0.4f);
            }

            if (jumpHeld && isGrounded && chargeJumpCDTimer <= 0f)
            {
                if (!isChargingJump)
                {
                    isChargingJump = true;
                    animator.SetBool("isChargingJump", true);
                }

                chargeDuration += Time.deltaTime * chargeTime;
                chargeDuration = Mathf.Min(chargeDuration, 1f);

                if (jumpChargeBar != null) jumpChargeBar.value = chargeDuration;
            }
            else if (jumpDownThisFrame && isGrounded && chargeJumpCDTimer <= 0f)
            {
                chargeDuration = 0f;
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);
            }

            if (jumpReleased && isChargingJump)
            {
                float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, chargeDuration);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                animator.SetTrigger("jump");

                if (chargeDuration <= 0.3f) SoundManager.instance?.PlaySFX("SJump");
                else SoundManager.instance?.PlaySFX("Jump");

                chargeDuration = 0f;
                isChargingJump = false;
                animator.SetBool("isChargingJump", false);
                if (jumpChargeBar != null) jumpChargeBar.value = 0f;

                chargeJumpCDTimer = chargeJumpCooldown;
            }
        }
        else
        {
            if (!isSticking)
                rb.linearDamping = originalDrag;

            animator.SetBool("isSliding", false);
            currentSpeed = 0f;

            if (isGrounded && !wasGrounded && jumpHeld && !isSticking && chargeJumpCDTimer <= 0f)
            {
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);
            }

            if (jumpHeld && isGrounded && !isSticking && chargeJumpCDTimer <= 0f)
            {
                if (!isChargingJump)
                {
                    isChargingJump = true;
                    animator.SetBool("isChargingJump", true);
                }

                jumpCharge += Time.deltaTime / chargeTime;
                jumpCharge = Mathf.Clamp(jumpCharge, 0f, 1f);

                if (jumpChargeBar != null)
                {
                    jumpChargeBar.value = jumpCharge;
                    jumpChargeBar.gameObject.SetActive(true);
                }
            }

            if (jumpReleased && isGrounded && !isSticking && isChargingJump)
            {
                float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCharge);
                float vx = Mathf.Abs(moveX) >= inputDeadzone ? moveX * speed * jumpForwardMultiplier : 0f;

                rb.linearVelocity = new Vector2(vx, jumpPower);
                airLaunchTimer = airLaunchHoldTime;

                animator.SetTrigger("jump");

                if (jumpCharge <= 0.3f) SoundManager.instance?.PlaySFX("SJump");
                else SoundManager.instance?.PlaySFX("Jump");

                jumpCharge = 0f;
                if (jumpChargeBar != null) jumpChargeBar.value = 0f;

                isChargingJump = false;
                animator.SetBool("isChargingJump", false);

                chargeJumpCDTimer = chargeJumpCooldown;
            }

            if (!jumpHeld && isGrounded && !isSticking && chargeJumpCDTimer > 0f)
            {
                isChargingJump = false;
                if (jumpChargeBar != null) jumpChargeBar.gameObject.SetActive(false);
            }
            else if (jumpDownThisFrame && isGrounded && !isSticking && chargeJumpCDTimer <= 0f)
            {
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);
            }
        }

        bool ceilingDetected = Physics2D.OverlapCircle(ceilingCheck.position, ceilingStickRadius, ceilingLayer);

        if (!isSticking && !isGrounded && ceilingDetected && stickCooldownTimer <= 0f)
        {
            isSticking = true;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.linearVelocity = new Vector2(0f, 0f);
            animator.SetBool("stick", true);

            SnapUpToCeilingImmediate();
        }
        else if (isSticking)
        {
            if (ceilingDetected)
            {
                SnapUpToCeilingImmediate();

                float targetX = moveInput.x * ceilingCrawlSpeed;
                float newVX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, ceilingCrawlAccel * Time.deltaTime);
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
                rb.linearVelocity = new Vector2(newVX, 0f);
            }
            else
            {
                isSticking = false;
                rb.gravityScale = 1f;
                rb.linearDamping = originalDrag;
                animator.SetBool("stick", false);
                stickCooldownTimer = stickCooldown;
            }
        }

        if (isSticking && Mathf.Abs(moveX) > inputDeadzone)
        {
            if (Physics2D.OverlapCircle(ceilingCheck.position, 0.1f, ceilingLayer) && !hasPlayedClimbSFX)
            {
                SoundManager.instance?.PlaySFX("Climb");
                hasPlayedClimbSFX = true;
                Invoke(nameof(ResetClimbSFX), 0.4f);
            }
        }

        if (isSticking && jumpDownThisFrame)
        {
            isSticking = false;
            rb.gravityScale = 1f;
            rb.linearDamping = originalDrag;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            animator.SetBool("stick", false);

            stickCooldownTimer = stickCooldown;
        }

        if (!isGrounded && !isSticking)
            rb.gravityScale = 1f;

        if (stickCooldownTimer > 0f)
            stickCooldownTimer -= Time.deltaTime;

        if (airLaunchTimer > 0f)
            airLaunchTimer -= Time.deltaTime;

        if (isOnVine)
        {
            HandleClimbInput();

            if (jumpAction.action.triggered)
            {
                ReleaseVine();
            }
        }

        if (jumpChargeBar != null)
        {
            bool showBar = (chargeJumpCDTimer <= 0f) && (isGrounded && (jumpHeld || isChargingJump)) && !isSticking;
            jumpChargeBar.gameObject.SetActive(showBar);
            if (showBar)
            {
                jumpChargeBar.value = isSliding ? chargeDuration : jumpCharge;
            }
            else if (chargeJumpCDTimer > 0f)
            {
                jumpChargeBar.value = 0f;
            }
        }

        if (!isGrounded && !isSticking && !isSliding)
        {
            if (isChargingJump || jumpCharge > 0f || chargeDuration > 0f)
            {
                isChargingJump = false;
                jumpCharge = 0f;
                chargeDuration = 0f;
                animator.SetBool("isChargingJump", false);

                if (jumpChargeBar != null)
                {
                    jumpChargeBar.value = 0f;
                    jumpChargeBar.gameObject.SetActive(false);
                }
            }
        }

        jumpDownThisFrame = false;
        jumpReleased = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(rollTriggerTag))
        {
            hasRollTrigger = true;
            rollTriggerTimer = rollTriggerTimeWindow;

            if (isInsideSlideZone)
                isSliding = true;
        }

        if (other.CompareTag(slideTag))
        {
            isInsideSlideZone = true;
        }

        if (canGrabVine && other.CompareTag("Grab"))
        {
            grabZone = other.transform;
            vineRigidbody = grabZone.GetComponentInParent<Rigidbody2D>();

            if (joint == null) joint = GetComponent<DistanceJoint2D>();
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

            SoundManager.instance?.PlaySFX("Grab");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(slideTag))
        {
            isInsideSlideZone = false;
            isSliding = false;
        }
    }

    private void HandleClimbInput()
    {
        float input = moveInput.y;

        if (Mathf.Abs(input) > 0.01f)
        {
            climbPosition += input * climbSpeed * Time.deltaTime;
            climbPosition = Mathf.Clamp(climbPosition, minHeight, maxHeight);

            Vector2 localAnchorInGrabZone = new Vector2(0, climbPosition);
            Vector2 localAnchorInVine = vineRigidbody.transform.InverseTransformPoint(grabZone.TransformPoint(localAnchorInGrabZone));

            joint.connectedAnchor = localAnchorInVine;

            if (!isClimbingSoundPlaying)
            {
                SoundManager.instance?.PlaySFX("Climb");
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

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        SoundManager.instance?.PlaySFX("Jump");

        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    public void ForceDetachFromVine(bool addJumpImpulse = false)
    {
        if (!isOnVine || joint == null) return;

        joint.enabled = false;
        isOnVine = false;
        grabZone = null;
        vineRigidbody = null;

        StartCoroutine(VineGrabCooldownRoutine());

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            if (addJumpImpulse)
            {
                SoundManager.instance?.PlaySFX("Jump");
                rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            }
        }
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

    private void SnapUpToCeilingImmediate()
    {
        if (playerCollider == null) return;

        Vector2 start = new Vector2(rb.position.x, playerCollider.bounds.max.y + 0.02f);
        RaycastHit2D hit = Physics2D.Raycast(start, Vector2.up, ceilingSnapMaxDistance, ceilingLayer);

        if (hit.collider != null)
        {
            float gap = hit.distance;
            float move = gap - ceilingSnapEpsilon;

            if (move > 0.0001f)
            {
                rb.MovePosition(rb.position + Vector2.up * move);
            }
            else if (move < -0.02f)
            {
                rb.MovePosition(rb.position + Vector2.down * (-move));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (ceilingCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingStickRadius);
        }
    }
}