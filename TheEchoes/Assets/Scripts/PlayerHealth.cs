using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerRespawned;

    [Header("Health (5 hits)")]
    [SerializeField] private int maxHits = 5;
    [SerializeField] private int currentHits = 5;
    public List<Image> healthDots = new List<Image>(5);
    public Sprite onSprite;
    public Sprite offSprite;
    public bool hideSpentDots = false;

    [Header("Damage Settings")]
    public float damageCooldown = 0.35f;
    private float _lastDamageTime = -999f;

    [Header("Knockback on 'damage'")]
    public float knockbackForce = 5f;
    [Range(0f, 1f)] public float knockbackUpBias = 0.15f;

    [Header("Invulnerability Blink (i-frames)")]
    public bool enableBlinkOnCooldown = true;
    public float blinkInterval = 0.1f;
    [Range(0f, 1f)] public float blinkMinAlpha = 0.35f;

    [Header("Water Damage Over Time")]
    public LayerMask waterLayer;
    public float waterProbeRadius = 0.22f;
    public float waterDamageInterval = 0.5f;
    public int waterDamagePerTick = 1;
    public string[] lumerinKeys = new[] { "Lumerin" };
    private float _nextWaterTickTime = 0f;

    [Header("Respawn / Checkpoint")]
    public static Vector3 lastCheckpointPosition;

    [Header("Objects To Reset On Respawn")]
    public GameObject[] objectsToReset;

    [Header("Temporary Suicide Hotkey")]
    public bool enableSuicideHotkey = true;
    public KeyCode suicideKey = KeyCode.R;

    private Rigidbody2D rb2d;
    private ConsumeControl consumeControl;

    private SpriteRenderer selfSprite;
    private Color selfOriginalColor;
    private Coroutine _blinkCo;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        consumeControl = GetComponent<ConsumeControl>();

        selfSprite = GetComponent<SpriteRenderer>();
        if (selfSprite != null) selfOriginalColor = selfSprite.color;

        if (lastCheckpointPosition == Vector3.zero)
            lastCheckpointPosition = transform.position;

        currentHits = Mathf.Clamp(currentHits, 0, maxHits);
        RefreshUI();
    }

    private void Update()
    {
        HandleWaterDot();
        HandleSuicideHotkey();
    }

    public int GetHits() => Mathf.Clamp(currentHits, 0, maxHits);

    public void SetHits(int hits, bool refreshUI)
    {
        currentHits = Mathf.Clamp(hits, 0, maxHits);
        if (refreshUI) RefreshUI();
    }

    public static void SetLastCheckpoint(Vector3 pos) => lastCheckpointPosition = pos;

    public void TakeHit(int amount = 1)
    {
        if (Time.time - _lastDamageTime < damageCooldown) return;

        _lastDamageTime = Time.time;

        currentHits = Mathf.Max(0, currentHits - Mathf.Abs(amount));
        RefreshUI();

        if (currentHits <= 0)
        {
            StopBlinkIfAny();
            Respawn();
            RestoreFullHealth();

            OnPlayerRespawned?.Invoke();
        }
        else
        {
            if (enableBlinkOnCooldown) StartBlinkUntilCooldownEnds();
        }
    }

    public void RestoreFullHealth()
    {
        currentHits = maxHits;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (healthDots == null || healthDots.Count == 0) return;
        for (int i = 0; i < healthDots.Count; i++)
        {
            var img = healthDots[i];
            if (img == null) continue;
            bool isOn = i < currentHits;

            if (hideSpentDots) img.enabled = isOn;
            else
            {
                img.enabled = true;
                if (onSprite && offSprite) img.sprite = isOn ? onSprite : offSprite;
            }
        }
    }

    public void Respawn()
    {
        var pc = GetComponent<PlayerControl>();
        if (pc != null)
        {
            pc.ForceDetachFromVine(addJumpImpulse: false);
        }

        transform.position = lastCheckpointPosition;

        if (objectsToReset != null)
        {
            foreach (var obj in objectsToReset)
            {
                if (!obj) continue;
                var r = obj.GetComponent<ObjectReset>();
                if (r != null) r.ResetState();
            }
        }

        if (consumeControl != null)
        {
            try { consumeControl.RestoreConsumedObjects(); } catch { }
            try { consumeControl.RevertToOriginalForm(); } catch { }
            try { consumeControl.SetHasStoredMorph(false); } catch { }
            try { if (consumeControl.pawerBar) consumeControl.pawerBar.value = 0f; } catch { }

            PlayerPrefs.DeleteKey("LastMorphTag");
            PlayerPrefs.DeleteKey("LastMorphTimeLeft");
            PlayerPrefs.Save();
        }

        StopBlinkIfAny();
        RestoreOriginalColors();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Kill"))
        {
            StopBlinkIfAny();
            Respawn();
            RestoreFullHealth();
            OnPlayerRespawned?.Invoke();
            return;
        }

        if (other.CompareTag("Damage"))
        {
            TakeHit(1);
            ApplyKnockbackFrom(other.transform.position);
            return;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var col = collision.collider;

        if (col.CompareTag("Kill"))
        {
            StopBlinkIfAny();
            Respawn();
            RestoreFullHealth();
            OnPlayerRespawned?.Invoke();
            return;
        }

        if (col.CompareTag("Damage"))
        {
            TakeHit(1);
            Vector3 src = (collision.contactCount > 0)
                ? (Vector3)collision.GetContact(0).point
                : col.transform.position;
            ApplyKnockbackFrom(src);
        }
    }

    private void ApplyKnockbackFrom(Vector3 sourcePosition)
    {
        if (!rb2d) return;
        Vector2 dir = ((Vector2)transform.position - (Vector2)sourcePosition).normalized;
        dir.y = Mathf.Clamp01(dir.y + knockbackUpBias);
        rb2d.linearVelocity = Vector2.zero;
        rb2d.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
    }

    private void StartBlinkUntilCooldownEnds()
    {
        if (!selfSprite) return;
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = StartCoroutine(BlinkCoroutine(_lastDamageTime + damageCooldown));
    }

    private IEnumerator BlinkCoroutine(float endTime)
    {
        float interval = Mathf.Max(0.02f, blinkInterval);
        bool dim = false;
        while (Time.time < endTime)
        {
            dim = !dim;
            SetSelfAlpha(dim ? blinkMinAlpha : 1f);
            yield return new WaitForSecondsRealtime(interval);
        }
        SetSelfAlpha(1f);
        _blinkCo = null;
    }

    private void StopBlinkIfAny()
    {
        if (_blinkCo != null)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }
    }

    private void RestoreOriginalColors()
    {
        if (selfSprite) selfSprite.color = selfOriginalColor;
    }

    private void SetSelfAlpha(float a)
    {
        if (!selfSprite) return;
        var c = selfSprite.color;
        c.a = a;
        selfSprite.color = c;
    }

    private void HandleWaterDot()
    {
        if (IsInLumerinForm()) return;

        bool inWater = Physics2D.OverlapCircle(transform.position, waterProbeRadius, waterLayer);
        if (!inWater) return;

        if (Time.time >= _nextWaterTickTime)
        {
            _nextWaterTickTime = Time.time + Mathf.Max(0.05f, waterDamageInterval);
            TakeHit(Mathf.Max(1, waterDamagePerTick));
        }
    }

    private bool IsInLumerinForm()
    {
        if (consumeControl == null) consumeControl = GetComponent<ConsumeControl>();
        if (consumeControl == null) return false;

        var snap = consumeControl.BuildSnapshot();
        if (!snap.isMorphing) return false;

        string tag = snap.morphTag ?? "";
        string ctrl = snap.controllerName ?? "";

        for (int i = 0; i < (lumerinKeys?.Length ?? 0); i++)
        {
            var key = lumerinKeys[i];
            if (string.IsNullOrEmpty(key)) continue;

            if (!string.IsNullOrEmpty(tag) && tag.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!string.IsNullOrEmpty(ctrl) && ctrl.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void HandleSuicideHotkey()
    {
        if (!enableSuicideHotkey) return;

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            pressed = true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(suicideKey))
            pressed = true;
#endif

        if (!pressed) return;

        StopBlinkIfAny();
        Respawn();
        RestoreFullHealth();
        OnPlayerRespawned?.Invoke();
    }
}