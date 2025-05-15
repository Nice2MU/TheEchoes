using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsumeControl : MonoBehaviour
{
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

        LoadMorphData();
        UpdateMorphIcon();
    }

    void Update()
    {
        Direction();
        isInWater = IsInWater();

        if (Input.GetKeyDown(KeyCode.F))
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

        SaveMorphData(currentMorphController, morphDuration);

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
        ClearSavedMorphData();
    }

    private void UpdateMorphIcon()
    {
        if (morphIcon == null) return;

        if (isMorphing && lastMorphData != null)
        {
            morphIcon.sprite = lastMorphData.icon;
        }

        else if (hasStoredMorph && lastMorphData != null)
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

    private void SaveMorphData(RuntimeAnimatorController controller, float timeLeft)
    {
        if (controller != null)
        {
            PlayerPrefs.SetString("LastMorphTag", controller.name);
            PlayerPrefs.SetFloat("LastMorphTimeLeft", timeLeft);
            PlayerPrefs.Save();
        }
    }

    private void LoadMorphData()
    {
        string lastMorphTag = PlayerPrefs.GetString("LastMorphTag", "");
        if (!string.IsNullOrEmpty(lastMorphTag))
        {
            MorphData data = morphMappings.Find(m => m.animatorController.name == lastMorphTag);
            if (data != null)
            {
                lastMorphData = data;
                lastMorphTimeLeft = PlayerPrefs.GetFloat("LastMorphTimeLeft", morphDuration);
            }
        }
    }

    private void ClearSavedMorphData()
    {
        PlayerPrefs.DeleteKey("LastMorphTag");
        PlayerPrefs.DeleteKey("LastMorphTimeLeft");
        PlayerPrefs.Save();
    }
}