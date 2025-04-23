using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LumerinAbility : MonoBehaviour, IMorphAbility
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float groundMoveSpeed = 0.5f;
    public float sinkSpeed = 1f;
    public float riseSpeed = 3f;
    public float autoJumpForce = 1.5f;

    [Header("Swim Boost")]
    public float boostSpeedMultiplier = 2f;
    public float boostDuration = 2f;

    [Header("Boost UI")]
    public Slider boostBar;
    public float maxBoost = 100f;
    public float boostConsumptionRate = 30f;
    public float boostRecoveryRate = 10f;

    [Header("Ground and Water Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask waterLayer;

    private GameObject player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isRising = false;
    private bool isGrounded = false;
    private bool isInWater = false;
    private bool isBoosting = false;
    private bool isAutoJumping = false;
    private float currentBoost;

    private PlayerControl originalControl;

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

        currentBoost = maxBoost;
        if (boostBar != null)
        {
            boostBar.gameObject.SetActive(true);
            boostBar.maxValue = maxBoost;
            boostBar.value = currentBoost;
        }
    }

    public void OnMorphExit(GameObject playerObj)
    {
        PlayRevertAnimation(playerObj);

        if (originalControl != null)
        {
            originalControl.enabled = true;
        }

        if (boostBar != null)
        {
            boostBar.gameObject.SetActive(false);
        }
    }

    public void PlayRevertAnimation(GameObject playerObj)
    {
        Animator revertAnimator = playerObj.GetComponent<Animator>();
        if (revertAnimator != null)
        {
            revertAnimator.SetTrigger("shake-slime");
        }
    }

    void Update()
    {
        if (player == null || rb == null) return;

        HandleGroundCheck();
        HandleWaterCheck();
        HandleSwimBoost();
        HandleMovement();
        HandleDiveRise();
        HandleAutoJump();
        HandleBoostRecovery();
    }

    void HandleGroundCheck()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
            animator.SetBool("onground", isGrounded);

            if (isGrounded)
            {
                isInWater = false;
                animator.SetBool("swim", false);
            }
        }
    }

    void HandleWaterCheck()
    {
        if (!isGrounded)
        {
            if (groundCheck != null)
            {
                isInWater = Physics2D.OverlapCircle(groundCheck.position, 0.1f, waterLayer);
                animator.SetBool("swim", isInWater);
            }
        }
    }

    void HandleSwimBoost()
    {
        if (isInWater)
        {
            if (Input.GetKey(KeyCode.E) && currentBoost > 0f)
            {
                isBoosting = true;
                currentBoost -= boostConsumptionRate * Time.deltaTime;
                currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);
                animator.SetBool("swimboost", true);
            }
            else
            {
                isBoosting = false;
                animator.SetBool("swimboost", false);
            }
        }
        else
        {
            isBoosting = false;
            animator.SetBool("swimboost", false);
        }

        if (boostBar != null)
        {
            boostBar.value = currentBoost;
        }
    }

    void HandleMovement()
    {
        if (isInWater)
        {
            if (!isBoosting)
            {
                float moveInput = Input.GetAxisRaw("Horizontal");
                float speed = moveSpeed;
                rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);
                animator.SetBool("swim", moveInput != 0);

                if (moveInput != 0)
                {
                    spriteRenderer.flipX = moveInput < 0;
                }
            }
            else
            {
                float moveInput = spriteRenderer.flipX ? -1f : 1f;
                float speed = moveSpeed * boostSpeedMultiplier;
                rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);
            }
        }
        else
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            float speed = groundMoveSpeed;
            rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);

            if (moveInput != 0)
            {
                spriteRenderer.flipX = moveInput < 0;
            }

            animator.SetFloat("Speed", Mathf.Abs(moveInput));

            animator.SetBool("swim", false);
            animator.SetBool("swimboost", false);
            animator.SetBool("onground", true);
        }
    }

    void HandleDiveRise()
    {
        if (isInWater)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                isRising = true;
                rb.velocity = new Vector2(rb.velocity.x, riseSpeed);
            }
            else
            {
                isRising = false;
                rb.velocity = new Vector2(rb.velocity.x, -sinkSpeed);
            }

            animator.SetBool("isRising", isRising);
        }
        else
        {
            isRising = false;
            animator.SetBool("isRising", false);
        }
    }

    void HandleAutoJump()
    {
        if (isGrounded && !isInWater && !isAutoJumping)
        {
            AutoJump();
        }
    }

    void AutoJump()
    {
        isAutoJumping = true;
        rb.velocity = new Vector2(rb.velocity.x, autoJumpForce);
        animator.SetTrigger("jump");

        StartCoroutine(ResetAutoJump());
    }

    IEnumerator ResetAutoJump()
    {
        yield return new WaitForSeconds(0.1f);
        isAutoJumping = false;
    }

    void HandleBoostRecovery()
    {
        if (!isBoosting && currentBoost < maxBoost)
        {
            currentBoost += boostRecoveryRate * Time.deltaTime;
            currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);
        }
    }

    public bool IsSinking()
    {
        return !isRising;
    }
}