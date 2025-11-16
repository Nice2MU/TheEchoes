using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LumerinAbility : MonoBehaviour, AbilityManager
{
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference abilityAction;

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

    [Header("Swim Vertical Control")]
    public float verticalDeadzone = 0.2f;
    public float downSinkMultiplier = 1.75f;
    public bool useAbsoluteDownSpeed = true;
    public float downSinkSpeed = 2.5f;

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

    private bool isActiveAbility = false;

    private const float BOOST_EPSILON = 0.01f;

    private bool hasReleasedAfterEmpty = false;

    private bool ControlsLocked()
        => (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
           || TutorialManager.IsTutorialLockActive
           || UIManager.IsUiMovementLocked;

    private void Awake()
    {
        ForceHideBoostBarAtBoot();
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        abilityAction?.action.Enable();
        ForceHideBoostBarAtBoot();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        abilityAction?.action.Disable();
    }

    private void ForceHideBoostBarAtBoot()
    {
        isActiveAbility = false;
        isBoosting = false;
        isRising = false;

        if (boostBar != null)
        {
            boostBar.value = 0f;
            if (boostBar.gameObject.activeSelf)
                boostBar.gameObject.SetActive(false);
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

        isActiveAbility = true;
        currentBoost = maxBoost;

        UpdateBoostBarUI();
    }

    public void OnAbilityExit(GameObject playerObj)
    {
        PlayRevertAnimation(playerObj);

        if (originalControl != null) originalControl.enabled = true;

        isActiveAbility = false;
        isBoosting = false;

        if (boostBar != null)
        {
            boostBar.value = 0f;
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
        if (!isGrounded && groundCheck != null)
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

    void Movement()
    {
        float moveInput = ControlsLocked() ? 0f :
            (moveAction != null ? moveAction.action.ReadValue<Vector2>().x : 0f);

        if (isInWater)
        {
            float speed = isBoosting ? moveSpeed * boostSpeedMultiplier : moveSpeed;
            float horizontal = isBoosting
                ? (spriteRenderer.flipX ? -1f : 1f)
                : moveInput;

            rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
            animator.SetBool("swim", Mathf.Abs(horizontal) > 0.001f);

            if (horizontal != 0 && !ControlsLocked())
                spriteRenderer.flipX = horizontal < 0;

            if ((horizontal != 0 || isRising || !isGrounded) && !isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.loop = true;
                SoundManager.instance.effectSource.clip = SoundManager.instance.swimlumerin;
                SoundManager.instance.effectSource.Play();
                isPlayingSwimSound = true;
            }
            else if (horizontal == 0 && !isRising && isGrounded && isPlayingSwimSound)
            {
                SoundManager.instance.effectSource.Stop();
                isPlayingSwimSound = false;
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * groundMoveSpeed, rb.linearVelocity.y);

            if (moveInput != 0 && !ControlsLocked())
                spriteRenderer.flipX = moveInput < 0;

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
        float vertical = ControlsLocked() ? 0f :
            (moveAction != null ? moveAction.action.ReadValue<Vector2>().y : 0f);

        bool upPressed = !ControlsLocked() &&
                         (
                             (jumpAction != null && jumpAction.action.ReadValue<float>() > 0f)
                             || (vertical > verticalDeadzone)
                         );

        bool downPressed = !ControlsLocked() && (vertical < -verticalDeadzone);

        if (isInWater)
        {
            if (upPressed)
            {
                isRising = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, riseSpeed);
            }
            else if (downPressed)
            {
                isRising = false;
                float vy = useAbsoluteDownSpeed ? -downSinkSpeed : -sinkSpeed * downSinkMultiplier;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            }
            else
            {
                isRising = false;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -sinkSpeed);
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

    void SwimBoost()
    {
        if (isInWater)
        {
            bool boostingInput = !ControlsLocked() && abilityAction != null && abilityAction.action.ReadValue<float>() > 0f;

            if (!boostingInput)
                hasReleasedAfterEmpty = false;

            if (boostingInput && currentBoost > 0f && !hasReleasedAfterEmpty)
            {
                isBoosting = true;
                currentBoost -= boostConsumptionRate * Time.deltaTime;
                currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);
                animator.SetBool("swimboost", true);

                if (currentBoost <= 0f)
                {
                    hasReleasedAfterEmpty = true;
                }

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

                if (Mathf.Abs(rb.linearVelocity.x) > 0 &&
                    (!SoundManager.instance.effectSource.isPlaying || SoundManager.instance.effectSource.clip != SoundManager.instance.swimlumerin))
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

        UpdateBoostBarUI();
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
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, autoJumpForce);
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

        UpdateBoostBarUI();
    }

    private void UpdateBoostBarUI()
    {
        if (boostBar == null) return;

        boostBar.maxValue = maxBoost;
        boostBar.value = currentBoost;

        bool shouldShow = isActiveAbility && ((currentBoost < maxBoost - BOOST_EPSILON) || isBoosting);
        if (boostBar.gameObject.activeSelf != shouldShow)
            boostBar.gameObject.SetActive(shouldShow);
    }

    public bool IsSinking() => !isRising;

    public float GetCurrentBoost() => currentBoost;

    public void SetCurrentBoost(float value)
    {
        currentBoost = Mathf.Clamp(value, 0f, maxBoost);
        UpdateBoostBarUI();
    }
}