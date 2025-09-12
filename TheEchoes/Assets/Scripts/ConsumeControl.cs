using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ConsumeControl : MonoBehaviour
{
    public InputActionReference moveAction;
    public InputActionReference consumeAction;

    [Header("Setting Consume")]
    public Animator animator;
    public SpriteRenderer playerSprite;
    public Sprite defaultMorphIcon;
    public Image morphIcon;
    public Slider pawerBar;

    public float consumeRange = 1f;
    public float morphDuration = 60f;
    public float consumeDelay = 1f;

    public LayerMask targetLayer;
    public LayerMask waterLayer;

    [System.Serializable]
    public class MorphData
    {
        public string tag;
        public Sprite icon;
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

    private PlayerControl playerControl;

    private MorphData lastMorphData;
    private float lastMorphTimeLeft;
    private bool hasStoredMorph = false;

    [System.Serializable]
    public class ConsumeSnapshot
    {
        public string controllerName;
        public string morphTag;
        public float timeLeft;
        public bool isMorphing;
        public bool hasStored;
    }

    public ConsumeSnapshot BuildSnapshot()
    {
        var snap = new ConsumeSnapshot();

        RuntimeAnimatorController ctrl = isMorphing
            ? (animator != null ? animator.runtimeAnimatorController : null)
            : (lastMorphData != null ? lastMorphData.animatorController : null);

        snap.controllerName = ctrl != null ? ctrl.name : "";
        snap.morphTag = lastMorphData != null ? lastMorphData.tag : "";
        snap.isMorphing = isMorphing;
        snap.hasStored = hasStoredMorph;

        float remain = 0f;
        if (isMorphing && pawerBar != null) remain = pawerBar.value;
        else if (!isMorphing && hasStoredMorph) remain = lastMorphTimeLeft;

        snap.timeLeft = remain;
        return snap;
    }

    public void ApplySnapshot(ConsumeSnapshot s)
    {
        if (s == null) return;

        MorphData data = null;
        if (!string.IsNullOrEmpty(s.controllerName))
            data = morphMappings.Find(m => m.animatorController != null && m.animatorController.name == s.controllerName);
        if (data == null && !string.IsNullOrEmpty(s.morphTag))
            data = morphMappings.Find(m => m.tag == s.morphTag);

        lastMorphData = data;
        lastMorphTimeLeft = s.timeLeft;
        hasStoredMorph = s.hasStored;

        if (s.isMorphing && data != null && data.animatorController != null)
        {
            currentMorphController = data.animatorController;

            if (data.abilityPrefab != null)
            {
                currentAbilityInstance = Instantiate(data.abilityPrefab);
                foreach (AbilityManager ability in currentAbilityInstance.GetComponents<AbilityManager>())
                {
                    ability.OnAbilityEnter(gameObject);
                }
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = currentMorphController;
                animator.Rebind();
                animator.Update(0f);
            }

            if (pawerBar != null)
            {
                pawerBar.maxValue = morphDuration;
                pawerBar.value = Mathf.Clamp(s.timeLeft, 0f, morphDuration);
            }

            if (morphTimerCoroutine != null)
            {
                StopCoroutine(morphTimerCoroutine);
                morphTimerCoroutine = null;
            }
            float start = (pawerBar != null ? pawerBar.value : Mathf.Clamp(s.timeLeft, 0f, morphDuration));
            morphTimerCoroutine = StartCoroutine(MorphCountdown(start));

            isMorphing = true;
            isConsuming = false;

            if (morphIcon != null && data.icon != null)
                morphIcon.sprite = data.icon;
        }

        else
        {
            isMorphing = false;
            if (animator != null && originalController != null)
            {
                animator.runtimeAnimatorController = originalController;
                animator.Rebind();
                animator.Update(0f);
            }
            UpdateMorphIcon();
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerControl = GetComponent<PlayerControl>();
        originalController = animator.runtimeAnimatorController;

        if (pawerBar != null)
        {
            pawerBar.maxValue = morphDuration;
            pawerBar.value = 0f;
        }

        UpdateMorphIcon();
    }

    void OnEnable()
    {
        if (consumeAction != null)
            consumeAction.action.Enable();

        if (moveAction != null)
            moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (consumeAction != null)
            consumeAction.action.Disable();

        if (moveAction != null)
            moveAction.action.Disable();
    }

    void Update()
    {
        Direction();

        isInWater = IsInWater();

        if (consumeAction != null && consumeAction.action.triggered)
        {
            if (isMorphing && hasStoredMorph && !isInWater)
            {
                RevertToOriginalForm();
            }

            else if (isMorphing && !isInWater)
            {
                RevertToOriginalForm();
            }

            else if (!isMorphing && !isConsuming && !isInWater)
            {
                if (IsTargetInFront())
                {
                    TryConsumeTarget();
                }

                else if (hasStoredMorph && !isMorphing)
                {
                    MorphBackToLastForm();
                }
            }
        }
    }

    public void SetHasStoredMorph(bool value)
    {
        hasStoredMorph = value;
    }

    void Direction()
    {
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

            if (Mathf.Abs(moveInput.x) > 0.01f || Mathf.Abs(rb.linearVelocity.x) > 0.01f)
            {
                bool newFacingRight = moveInput.x > 0 || rb.linearVelocity.x > 0;

                if (newFacingRight != isFacingRight)
                {
                    isFacingRight = newFacingRight;
                    playerSprite.flipX = !isFacingRight;
                }
            }
        }
    }

    bool IsTargetInFront()
    {
        Vector2 facingDir = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 frontPos = (Vector2)transform.position + facingDir * consumeRange * 0.75f;
        Collider2D target = Physics2D.OverlapCircle(frontPos, consumeRange * 0.5f, targetLayer);
        return target != null;
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
                    foreach (AbilityManager ability in currentAbilityInstance.GetComponents<AbilityManager>())
                    {
                        ability.OnAbilityEnter(gameObject);
                    }
                }

                SoundManager.instance?.PlaySFX("Consume");

                isMorphing = true;
                isConsuming = true;
                animator.Play("consume-slime");

                target.gameObject.SetActive(false);
                consumedObjects.Add(target.gameObject);

                StartCoroutine(DelayBeforeMorph());
            }
        }
    }

    IEnumerator DelayBeforeMorph()
    {
        yield return new WaitForSeconds(consumeDelay);
        MorphIntoTarget();
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

    MorphData GetMorphDataByController(RuntimeAnimatorController controller)
    {
        foreach (var data in morphMappings)
        {
            if (data.animatorController == controller)
                return data;
        }

        return null;
    }

    void MorphIntoTarget()
    {
        SoundManager.instance?.PlaySFX("Transform");

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

        morphTimerCoroutine = StartCoroutine(MorphCountdown(morphDuration));
        isConsuming = false;

        if (morphIcon != null && lastMorphData != null)
        {
            morphIcon.sprite = lastMorphData.icon;
        }

        if (morphIcon != null && currentMorphController != null)
        {
            MorphData newMorphData = GetMorphDataByController(currentMorphController);
            if (newMorphData != null)
            {
                morphIcon.sprite = newMorphData.icon;
            }
        }
    }

    IEnumerator MorphCountdown(float timeLeft)
    {
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
        if (isMorphing)
        {
            if (pawerBar != null && pawerBar.value > 0)
            {
                lastMorphData = GetMorphDataByController(animator.runtimeAnimatorController);
                lastMorphTimeLeft = pawerBar.value;
                hasStoredMorph = true;
            }

            else
            {
                lastMorphData = null;
                lastMorphTimeLeft = 0f;
                hasStoredMorph = false;
            }
        }

        isMorphing = false;

        SoundManager.instance?.PlaySFX("Transform");

        animator.runtimeAnimatorController = originalController;
        animator.Rebind();
        animator.Update(0f);

        if (morphTimerCoroutine != null)
        {
            StopCoroutine(morphTimerCoroutine);
            morphTimerCoroutine = null;
        }

        if (currentAbilityInstance != null)
        {
            foreach (AbilityManager ability in currentAbilityInstance.GetComponents<AbilityManager>())
            {
                ability.OnAbilityExit(gameObject);
            }

            if (currentAbilityInstance.GetComponent<LumerinAbility>() != null)
            {
                StartCoroutine(DisablePlayerControlForSeconds(1.5f));
            }

            Destroy(currentAbilityInstance);
            currentAbilityInstance = null;
        }

        UpdateMorphIcon();

    }

    private void UpdateMorphIcon()
    {
        if (morphIcon == null) return;

        if ((isMorphing && lastMorphData != null) || (hasStoredMorph && lastMorphData != null))
        {
            morphIcon.sprite = lastMorphData.icon;
        }

        else
        {
            morphIcon.sprite = defaultMorphIcon;
        }
    }

    void MorphBackToLastForm()
    {
        if (lastMorphData == null || lastMorphData.animatorController == null)
            return;

        SoundManager.instance?.PlaySFX("Transform");

        animator.runtimeAnimatorController = lastMorphData.animatorController;
        animator.Rebind();
        animator.Update(0f);

        if (lastMorphData.abilityPrefab != null)
        {
            currentAbilityInstance = Instantiate(lastMorphData.abilityPrefab);
            foreach (AbilityManager ability in currentAbilityInstance.GetComponents<AbilityManager>())
            {
                ability.OnAbilityEnter(gameObject);
            }
        }

        if (pawerBar != null)
        {
            pawerBar.maxValue = morphDuration;
            pawerBar.value = lastMorphTimeLeft;
        }

        morphTimerCoroutine = StartCoroutine(MorphCountdown(lastMorphTimeLeft));
        isMorphing = true;
        hasStoredMorph = false;

        if (morphIcon != null && lastMorphData != null)
        {
            morphIcon.sprite = lastMorphData.icon;
        }
    }

    IEnumerator DisablePlayerControlForSeconds(float duration)
    {
        if (playerControl != null)
            playerControl.enabled = false;

        yield return new WaitForSeconds(duration);

        if (playerControl != null)
            playerControl.enabled = true;
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