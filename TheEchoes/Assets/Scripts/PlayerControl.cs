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

    private float walkTime = 0f;
    private bool isWalking = false;

    [Header("Slime Movement")]
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

    // ---------- Wall Slide / Wall Jump ----------
    [Header("Wall Jump")]
    public Transform wallCheck;
    public LayerMask wallLayer;

    // เร่งความเร็วไหลให้ชัด
    public float wallSlideSpeed = 10f;        // ความเร็วดิ่งลงเป้าหมาย
    public float wallJumpTime = 0.2f;         // หน้าต่างอนุญาตวอลจัมป์
    public float wallJumpDuration = 0.4f;     // ระยะเวลาล็อกบางอินพุตหลังวอลจัมป์

    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private float wallJumpCounter = 0f;

    [Header("Wall Slide Tuning")]
    [SerializeField] private bool requireInputTowardWall = false; // ต้องกดเข้าหากำแพงไหม
    [SerializeField] private float slideStartKick = -2f;          // เตะความเร็วลงให้เริ่มไหลไว
    [SerializeField] private float wallSlideAccel = 60f;          // อัตราเร่งลงสู่ความเร็วเป้าหมาย
    [SerializeField] private float wallGravityMultiplier = 1.6f;  // คูณแรงโน้มถ่วงตอนสไลด์

    [Header("Wall Slide Delay")]
    [SerializeField] private float wallSlideDelay = 0.20f;        // เวลาเกาะก่อนเริ่มไหล
    private float wallSlideDelayTimer = 0f;
    private int _lastWallSide = 0; // -1 ซ้าย, +1 ขวา, 0 ไม่มี
    private bool _isWallHang = false;

    [Header("Wall Jump Charge")]
    [SerializeField] private float wallMinHorizSpeed = 4f;   // แนวนอนขั้นต่ำ
    [SerializeField] private float wallMaxHorizSpeed = 10f;  // แนวนอนสูงสุด
    [SerializeField] private float wallMinVerticalPower = 5f;  // แนวตั้งขั้นต่ำ
    [SerializeField] private float wallMaxVerticalPower = 14f; // แนวตั้งสูงสุด
    private float wallJumpCharge = 0f;  // 0..1
    private bool wallCharging = false;

    [Header("Wall Probe (left/right)")]
    [SerializeField] private float wallCheckSideOffset = 0.12f; // ระยะเลื่อนซ้าย/ขวาจาก wallCheck
    [SerializeField] private float wallCheckProbeRadius = 0.1f; // รัศมีวงตรวจชนซ้าย/ขวา

    // ---------- WallCheck follow facing ----------
    [Header("Wall Check Follow Facing")]
    [SerializeField] private bool mirrorWallCheckByFacing = true;
    [SerializeField] private Vector2 wallCheckLocalOffset = new Vector2(0.2f, 0f);
    private Vector3 _wallCheckInitialLocalPos;

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
        joint.enabled = false;

        originalDrag = dragMultiplier;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalDrag;

        if (jumpChargeBar != null)
        {
            jumpChargeBar.value = 0f;
            jumpChargeBar.gameObject.SetActive(false);
        }

        if (wallCheck != null)
            _wallCheckInitialLocalPos = wallCheck.localPosition;
    }

    void Update()
    {
        if (!canMove) { jumpDownThisFrame = false; jumpReleased = false; return; }

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

        // ---------- Wall Slide / Wall Jump: Detect & Handle (delay -> fast slide -> away jump) ----------
        int wallSide = GetWallSide();          // -1 = wall left, +1 = wall right, 0 = none
        bool isWalled = wallSide != 0;

        // ต้องมีอินพุตเข้าหากำแพงหรือไม่
        bool allowByInput = !requireInputTowardWall || Mathf.Abs(moveX) > 0.01f;

        // เงื่อนไขไม่ได้สไลด์กำแพง
        if (!isWalled || isGrounded || isSticking || isOnVine || !allowByInput)
        {
            isWallSliding = false;
            _isWallHang = false;
            wallJumpCounter -= Time.deltaTime;

            // คืนค่า default
            rb.linearDamping = originalDrag;
            rb.gravityScale = 1f;
        }
        else
        {
            // แตะกำแพงครั้งแรก หรือสลับด้าน -> รีสตาร์ท delay
            if (wallSide != _lastWallSide || (!_isWallHang && !isWallSliding))
            {
                wallSlideDelayTimer = wallSlideDelay;
            }

            // เปิดหน้าต่างวอลจัมป์ + ชาร์จตอนเกาะกำแพง
            wallJumpCounter = wallJumpTime;
            CancelInvoke(nameof(StopWallJumping));

            if (jumpHeld)
            {
                wallCharging = true;
                wallJumpCharge += Time.deltaTime / chargeTime;
                wallJumpCharge = Mathf.Clamp01(wallJumpCharge);
            }

            // ระยะ "แขวน" (หน่วงก่อนเริ่มไหล)
            if (wallSlideDelayTimer > 0f)
            {
                _isWallHang = true;
                isWallSliding = false;

                wallSlideDelayTimer -= Time.deltaTime;

                // ค้างนิ่ง ๆ ขณะหน่วง
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.9f, Mathf.Min(0f, rb.linearVelocity.y));
            }
            else
            {
                // เริ่มไหลจริง
                _isWallHang = false;
                isWallSliding = true;

                rb.gravityScale = wallGravityMultiplier;
                rb.linearDamping = 0f;

                float vy = rb.linearVelocity.y;
                if (vy > slideStartKick) vy = slideStartKick; // ให้ติดลบทันทีถ้ายังขึ้น/นิ่ง
                vy = Mathf.MoveTowards(vy, -wallSlideSpeed, wallSlideAccel * Time.deltaTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            }
        }

        // ปล่อยปุ่มกระโดดภายในหน้าต่าง -> วอลจัมป์ "ออกจากกำแพงเสมอ"
        if (jumpReleased && wallJumpCounter > 0f && isWalled)
        {
            isWallJumping = true;
            wallJumpCounter = 0f;

            float dirAway = -Mathf.Sign(wallSide); // +1(กำแพงขวา)=>-1 ออกซ้าย, -1(กำแพงซ้าย)=>+1 ออกขวา

            float t = Mathf.Clamp01(wallJumpCharge);
            float vx = Mathf.Lerp(wallMinHorizSpeed, wallMaxHorizSpeed, t) * dirAway;
            float vy2 = Mathf.Lerp(wallMinVerticalPower, wallMaxVerticalPower, t);

            rb.linearVelocity = new Vector2(vx, vy2);

            if (spriteRenderer != null)
                spriteRenderer.flipX = vx < 0f;

            animator?.SetTrigger("jump");
            SoundManager.instance?.PlaySFX("Jump");

            wallJumpCharge = 0f;
            wallCharging = false;

            Invoke(nameof(StopWallJumping), wallJumpDuration);
        }
        // --------------------------------------------------------------------

        // ====== โค้ดเดิมอื่น ๆ คงไว้ ======
        if (isGrounded)
        {
            if (moveX != 0 && !jumpHeld && !isSliding)
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

        // กันการทับความเร็วช่วงกำลังกระโดดกำแพง
        if (!isSticking && isWalking && !isSliding && !isWallJumping)
        {
            if (!jumpHeld || !isGrounded)
                rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else if (isWalking && !isSliding && !isWallJumping)
        {
            rb.linearVelocity = new Vector2(moveX * speed, 0f);

            if (!playerCollider.IsTouchingLayers(ceilingLayer))
            {
                isSticking = false;
                rb.gravityScale = 1f;
                animator.SetBool("stick", false);
            }
        }

        if (moveX != 0)
            spriteRenderer.flipX = moveX < 0;

        animator.SetBool("walk", moveX != 0);
        animator.SetBool("isGrounded", isGrounded);

        if (isSliding)
        {
            rb.linearDamping = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, slideSpeed, smoothTime * Time.deltaTime * slideSpeed);
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            if (!isWallJumping)
                rb.linearVelocity = new Vector2(currentSpeed * Mathf.Sign(transform.localScale.x), rb.linearVelocity.y);

            animator.SetBool("isSliding", true);

            if (!hasPlayedRollSFX)
            {
                SoundManager.instance?.PlaySFX("Roll");
                hasPlayedRollSFX = true;
                Invoke(nameof(ResetRollSFX), 0.4f);
            }

            if (jumpHeld && isGrounded)
            {
                chargeDuration += Time.deltaTime * chargeTime;
                chargeDuration = Mathf.Min(chargeDuration, 1f);
                isChargingJump = true;
                animator.SetBool("isChargingJump", true);

                if (jumpChargeBar != null)
                    jumpChargeBar.value = chargeDuration;
            }
            else if (jumpDownThisFrame && isGrounded)
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
            if (!isWallSliding) rb.linearDamping = originalDrag;

            animator.SetBool("isSliding", false);
            currentSpeed = 0f;

            if (jumpHeld && isGrounded && !isSticking)
            {
                jumpCharge += Time.deltaTime / chargeTime;
                jumpCharge = Mathf.Clamp(jumpCharge, 0f, 1f);

                if (jumpChargeBar != null) jumpChargeBar.value = jumpCharge;
            }

            if (jumpReleased && isGrounded && !isSticking)
            {
                float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCharge);
                rb.linearVelocity = new Vector2(moveX * speed, jumpPower);
                animator.SetTrigger("jump");

                if (jumpCharge <= 0.3f)
                    SoundManager.instance?.PlaySFX("SJump");
                else
                    SoundManager.instance?.PlaySFX("Jump");

                jumpCharge = 0;

                if (jumpChargeBar != null) jumpChargeBar.value = 0;
            }
        }

        if (!isSticking && !isGrounded && playerCollider.IsTouchingLayers(ceilingLayer) && stickCooldownTimer <= 0f)
        {
            isSticking = true;
            rb.linearVelocity = Vector2.zero;
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

        if (isSticking && jumpDownThisFrame)
        {
            isSticking = false;
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            animator.SetBool("stick", false);

            stickCooldownTimer = stickCooldown;
        }

        if (!isGrounded && !isSticking)
            rb.gravityScale = 1f;

        if (stickCooldownTimer > 0f)
            stickCooldownTimer -= Time.deltaTime;

        if (isOnVine)
        {
            HandleClimbInput();

            if (jumpAction.action.triggered)
            {
                ReleaseVine();
            }
        }

        // ---- แสดงแถบชาร์จ: รวมโหมดกำแพงด้วย ----
        if (jumpChargeBar != null)
        {
            bool showBar = (isGrounded && (jumpHeld || isChargingJump)) && !isSticking;

            if ((isWallSliding || _isWallHang) && (jumpHeld || wallCharging)) showBar = true;

            jumpChargeBar.gameObject.SetActive(showBar);

            if (showBar)
            {
                float uiProgress = (isWallSliding || _isWallHang) ? wallJumpCharge : jumpCharge;
                jumpChargeBar.value = uiProgress;
            }
            else
            {
                jumpChargeBar.value = 0f;
            }
        }

        jumpDownThisFrame = false;
        jumpReleased = false;

        // เก็บด้านกำแพงล่าสุดเพื่อใช้กับ delay
        _lastWallSide = wallSide;
    }

    void LateUpdate()
    {
        SyncWallCheckToFacing();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(slideTag))
        {
            isSliding = true;
        }

        if (canGrabVine && other.CompareTag("Grab"))
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

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

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

    // ---------- Wall helpers ----------
    private bool IsWalled()
    {
        if (wallCheck == null) return false;
        return Physics2D.OverlapCircle(wallCheck.position, groundCheckRadius, wallLayer);
    }

    // -1 = wall on left, +1 = wall on right, 0 = none
    private int GetWallSide()
    {
        if (wallCheck == null) return 0;

        Vector2 basePos = wallCheck.position;
        bool right = Physics2D.OverlapCircle(basePos + Vector2.right * wallCheckSideOffset,
                                             wallCheckProbeRadius, wallLayer);
        bool left = Physics2D.OverlapCircle(basePos + Vector2.left * wallCheckSideOffset,
                                             wallCheckProbeRadius, wallLayer);

        if (right == left) return 0; // ไม่ชัดเจน
        return right ? +1 : -1;
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }
    // ----------------------------------

    // ---------- Sync wallCheck with facing ----------
    private void SyncWallCheckToFacing()
    {
        if (!mirrorWallCheckByFacing || wallCheck == null || spriteRenderer == null) return;

        float dir = spriteRenderer.flipX ? -1f : 1f;
        wallCheck.localPosition = new Vector3(Mathf.Abs(_wallCheckInitialLocalPos.x) * dir,
                                              _wallCheckInitialLocalPos.y,
                                              _wallCheckInitialLocalPos.z);

        // หรือใช้ offset กำหนดเอง:
        // wallCheck.localPosition = new Vector3(wallCheckLocalOffset.x * dir, wallCheckLocalOffset.y, wallCheck.localPosition.z);
    }
    // -------------------------------------------------
}
