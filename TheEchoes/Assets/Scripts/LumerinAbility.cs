using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LumerinAbility : MonoBehaviour, AbilityManager
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

    private bool isPlayingOnGroundSound = false;
    private bool isPlayingSwimSound = false;
    private bool wasInWater = false;

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

        currentBoost = maxBoost;

        if (boostBar != null)
        {
            boostBar.gameObject.SetActive(true);
            boostBar.maxValue = maxBoost;
            boostBar.value = currentBoost;
        }
    }

    public void OnAbilityExit(GameObject playerObj)
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

        if (isPlayingSwimSound || isPlayingOnGroundSound)
        {
            SoundManager.instance.effectSource.Stop();
            isPlayingSwimSound = false;
            isPlayingOnGroundSound = false;
        }
    }

    public void PlayRevertAnimation(GameObject playerObj)
    {
        Animator revertAnimator = playerObj.GetComponent<Animator>();

        if (revertAnimator != null)
        {
            revertAnimator.SetTrigger("shake-slime");
            SoundManager.instance.PlaySFX("Shake");
        }
    }

    void Update()
    {
        if (player == null || rb == null) return;

        GroundCheck();
        WaterCheck();
        SwimBoost();
        Movement();
        DiveRise();
        AutoJump();
        BoostRecovery();
    }

    void GroundCheck()
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

    void WaterCheck()
    {
        if (!isGrounded)
        {
            if (groundCheck != null)
            {
                isInWater = Physics2D.OverlapCircle(groundCheck.position, 0.1f, waterLayer);
                animator.SetBool("swim", isInWater);

                if (isInWater && !wasInWater)
                {
                    SoundManager.instance.PlaySFX("Drown");
                    wasInWater = true;
                }

                else if (!isInWater)
                {
                    wasInWater = false;
                }
            }
        }
    }

    void SwimBoost()
    {
        if (isInWater)
        {
            if (Input.GetKey(KeyCode.E) && currentBoost > 0f)
            {
                isBoosting = true;
                currentBoost -= boostConsumptionRate * Time.deltaTime;
                currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);
                animator.SetBool("swimboost", true);

                if (!SoundManager.instance.effectSource.isPlaying || SoundManager.instance.effectSource.clip != SoundManager.instance.boostlumerin)
                {
                    SoundManager.instance.effectSource.loop = true;
                    SoundManager.instance.effectSource.clip = SoundManager.instance.boostlumerin;
                    SoundManager.instance.effectSource.Play();
                }
            }

            else
            {
                if (isBoosting)
                {
                    if (SoundManager.instance.effectSource.isPlaying && SoundManager.instance.effectSource.clip == SoundManager.instance.boostlumerin)
                    {
                        SoundManager.instance.effectSource.Stop();
                    }
                }

                isBoosting = false;
                animator.SetBool("swimboost", false);

                if (Mathf.Abs(rb.velocity.x) > 0 && (!SoundManager.instance.effectSource.isPlaying || SoundManager.instance.effectSource.clip != SoundManager.instance.swimlumerin))
                {
                    SoundManager.instance.effectSource.loop = true;
                    SoundManager.instance.effectSource.clip = SoundManager.instance.swimlumerin;
                    SoundManager.instance.effectSource.Play();
                }
            }
        }

        else
        {
            if (SoundManager.instance.effectSource.isPlaying &&
                (SoundManager.instance.effectSource.clip == SoundManager.instance.boostlumerin ||
                 SoundManager.instance.effectSource.clip == SoundManager.instance.swimlumerin))
            {
                SoundManager.instance.effectSource.Stop();
            }

            isBoosting = false;
            animator.SetBool("swimboost", false);
        }

        if (boostBar != null)
        {
            boostBar.value = currentBoost;
        }
    }

    void Movement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (isInWater)
        {
            float speed = isBoosting ? moveSpeed * boostSpeedMultiplier : moveSpeed;
            float direction = isBoosting ? (spriteRenderer.flipX ? -1f : 1f) : moveInput;

            rb.velocity = new Vector2(direction * speed, rb.velocity.y);
            animator.SetBool("swim", moveInput != 0);

            if (direction != 0)
            {
                spriteRenderer.flipX = direction < 0;
            }

            if ((moveInput != 0 || isRising || !isGrounded) && !isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.loop = true;
                SoundManager.instance.effectSource.clip = SoundManager.instance.swimlumerin;
                SoundManager.instance.effectSource.Play();
                isPlayingSwimSound = true;
            }

            else if (moveInput == 0 && !isRising && isGrounded && isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.Stop();
                isPlayingSwimSound = false;
            }
        }

        else
        {
            rb.velocity = new Vector2(moveInput * groundMoveSpeed, rb.velocity.y);

            if (moveInput != 0)
            {
                spriteRenderer.flipX = moveInput < 0;
            }

            animator.SetFloat("Speed", Mathf.Abs(moveInput));
            animator.SetBool("swim", false);
            animator.SetBool("swimboost", false);
            animator.SetBool("onground", true);

            if (isAutoJumping && !isPlayingOnGroundSound)
            {
                SoundManager.instance.effectSource.loop = true;
                SoundManager.instance.effectSource.clip = SoundManager.instance.ongroundlumerin;
                SoundManager.instance.effectSource.Play();
                isPlayingOnGroundSound = true;
            }

            else if (!isAutoJumping && isPlayingOnGroundSound)
            {
                SoundManager.instance.effectSource.Stop();
                isPlayingOnGroundSound = false;
            }
        }
    }

    void DiveRise()
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

            if (!isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.loop = true;
                SoundManager.instance.effectSource.clip = SoundManager.instance.swimlumerin;
                SoundManager.instance.effectSource.Play();
                isPlayingSwimSound = true;
            }
        }

        else
        {
            isRising = false;
            animator.SetBool("isRising", false);

            if (isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.Stop();
                isPlayingSwimSound = false;
            }
        }
    }

    void AutoJump()
    {
        if (isGrounded && !isInWater && !isAutoJumping)
        {
            AutoJumps();
        }
    }

    void AutoJumps()
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

    void BoostRecovery()
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