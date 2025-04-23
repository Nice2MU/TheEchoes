using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsumeControl : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer playerSprite;
    public float consumeRange = 1.0f;
    public LayerMask targetLayer;
    public float morphDuration = 60f;

    public Slider pawerBar;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask waterLayer;

    [System.Serializable]
    public class MorphData
    {
        public string tag;
        public RuntimeAnimatorController animatorController;
        public GameObject abilityPrefab;
    }

    public List<MorphData> morphMappings;

    private RuntimeAnimatorController originalController;
    private RuntimeAnimatorController currentMorphController;
    private GameObject currentAbilityInstance;

    public bool isMorphing { get; private set; } = false;
    private Coroutine morphTimerCoroutine;
    private bool isFacingRight = true;
    private bool isConsuming = false;

    private bool isInWater = false;

    private Rigidbody2D rb;

    private List<GameObject> consumedObjects = new List<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalController = animator.runtimeAnimatorController;

        if (pawerBar != null)
        {
            pawerBar.maxValue = morphDuration;
            pawerBar.value = 0f;
        }
    }

    void Update()
    {
        HandleDirection();

        isInWater = IsInWater();

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isMorphing && !isInWater)
            {
                RevertToOriginalForm();
            }
            else if (isMorphing && isInWater)
            {

            }
            else if (!isConsuming)
            {
                TryConsumeTarget();
            }
        }
    }

    void HandleDirection()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(moveInput) > 0.01f || Mathf.Abs(rb.velocity.x) > 0.01f)
        {
            bool newFacingRight = moveInput > 0 || rb.velocity.x > 0;

            if (newFacingRight != isFacingRight)
            {
                isFacingRight = newFacingRight;
                playerSprite.flipX = !isFacingRight;
            }
        }
    }

    void TryConsumeTarget()
    {
        if (isConsuming) return;

        Vector2 facingDir = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 frontPos = (Vector2)transform.position + facingDir * consumeRange * 0.75f;

        Collider2D target = Physics2D.OverlapCircle(frontPos, consumeRange * 0.5f, targetLayer);

        if (target != null)
        {
            MorphData data = GetMorphData(target.tag);

            if (data != null && data.animatorController != null)
            {
                currentMorphController = data.animatorController;

                if (data.abilityPrefab != null)
                {
                    currentAbilityInstance = Instantiate(data.abilityPrefab);
                    foreach (IMorphAbility ability in currentAbilityInstance.GetComponents<IMorphAbility>())
                    {
                        ability.OnMorphEnter(gameObject);
                    }
                }

                isMorphing = true;
                isConsuming = true;
                animator.Play("consume-slime");

                target.gameObject.SetActive(false);
                consumedObjects.Add(target.gameObject);

                Invoke("MorphIntoTarget", 0.5f);
            }
        }
    }

    MorphData GetMorphData(string tag)
    {
        foreach (var data in morphMappings)
        {
            if (data.tag == tag)
                return data;
        }
        return null;
    }

    void MorphIntoTarget()
    {
        if (animator != null && currentMorphController != null)
        {
            animator.runtimeAnimatorController = currentMorphController;
            animator.Rebind();
            animator.Update(0f);
        }

        if (pawerBar != null)
        {
            pawerBar.maxValue = morphDuration;
            pawerBar.value = morphDuration;
        }

        morphTimerCoroutine = StartCoroutine(MorphCountdown());
        isConsuming = false;
    }

    IEnumerator MorphCountdown()
    {
        float timeLeft = morphDuration;

        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;

            if (pawerBar != null)
            {
                pawerBar.value = timeLeft;
            }

            yield return null;
        }

        RevertToOriginalForm();
    }

    public void RevertToOriginalForm()
    {
        animator.runtimeAnimatorController = originalController;
        animator.Rebind();
        animator.Update(0f);
        isMorphing = false;

        if (pawerBar != null)
        {
            pawerBar.value = 0f;
        }

        if (morphTimerCoroutine != null)
        {
            StopCoroutine(morphTimerCoroutine);
            morphTimerCoroutine = null;
        }

        if (currentAbilityInstance != null)
        {
            foreach (IMorphAbility ability in currentAbilityInstance.GetComponents<IMorphAbility>())
            {
                ability.OnMorphExit(gameObject);
            }

            Destroy(currentAbilityInstance);
            currentAbilityInstance = null;
        }
    }

    bool IsGrounded()
    {
        Vector2 boxSize = new Vector2(0.5f, 0.1f);
        Vector2 boxOrigin = groundCheck.position;
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, 0.05f, groundLayer);
        return hit.collider != null;
    }

    bool IsInWater()
    {
        return Physics2D.OverlapCircle(transform.position, 0.2f, waterLayer);
    }

    public void RestoreConsumedObjects()
    {
        foreach (GameObject consumedObj in consumedObjects)
        {
            if (consumedObj != null)
            {
                consumedObj.SetActive(true);
                ObjectReset resetScript = consumedObj.GetComponent<ObjectReset>();
                if (resetScript != null)
                {
                    resetScript.ResetState();
                }
            }
        }

        consumedObjects.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 facingDir = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 frontPos = (Vector2)transform.position + facingDir * consumeRange * 0.75f;
        Gizmos.DrawWireSphere(frontPos, consumeRange * 0.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}