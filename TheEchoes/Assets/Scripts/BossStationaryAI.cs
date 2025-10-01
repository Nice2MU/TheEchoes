using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossStationaryAI : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private BoxCollider2D roomBounds;

    // -------- Start / Triggering --------
    [Header("Start / Triggering")]
    [Tooltip("ถ้าเปิดไว้ = จะรอคำสั่ง Trigger ถึงเริ่ม (เหมาะกับกดปุ่ม/จบไดอาลอก)")]
    [SerializeField] private bool startByButton = true;

    [Tooltip("ดีเลย์เริ่ม (วินาที) เมื่อตั้งค่าเริ่มด้วยปุ่ม/ไดอาลอก")]
    [SerializeField] private float manualStartDelay = 0f;

    [Tooltip("อนุญาตปุ่มทดสอบใน Editor/Debug (เช่นกด F เพื่อเริ่ม)")]
    [SerializeField] private bool allowDebugKeyTrigger = false;
    [SerializeField] private KeyCode debugTriggerKey = KeyCode.F;

    [Header("Spawn / Intro Animation")]
    [Tooltip("เล่นอนิเมชันเปิดตัวเมื่อ 'เริ่มบอส' (ทั้งกรณีกดปุ่มหรืออัตโนมัติ)")]
    [SerializeField] private bool playSpawnOnStart = true;
    [Tooltip("Trigger ชื่ออะไรใน Animator สำหรับอนิเมชันเริ่ม")]
    [SerializeField] private string spawnTriggerName = "Spawn";
    [Tooltip("ถ้าเปิดไว้ จะรอ Animation Event เรียก OnSpawnIntroFinished() ค่อยเริ่ม AI")]
    [SerializeField] private bool waitForSpawnEvent = false;
    [Tooltip("ถ้าไม่รอ Event จะหน่วงเวลาหลัง Trigger Spawn เท่านี้ก่อนเริ่ม AI")]
    [SerializeField] private float startDelayAfterSpawn = 0f;

    // -------- Attack timing --------
    [Header("Attack timing")]
    [SerializeField] private float minAttackInterval = 5f;
    [SerializeField] private float maxAttackInterval = 15f;

    // -------- Zones --------
    [Header("Zone split")]
    [Range(0.05f, 0.95f)] public float leftSplit = 0.33f;
    [Range(0.05f, 0.95f)] public float rightSplit = 0.66f;
    [Tooltip("เส้นระดับพื้น (world Y)")]
    public float groundY = 0f;
    [Tooltip("เผื่อความใกล้พื้น")]
    public float groundTolerance = 0.3f;

    // -------- Attacks --------
    [Header("Attack definitions")]
    [SerializeField] private AttackEntry[] attackTable;

    // -------- Hit check --------
    [Header("Hit check")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 hitOffset = Vector2.zero;
    [SerializeField] private float hitRadius = 0.6f;

    // -------- Health / Death --------
    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool invulnerable = false;
    [SerializeField] private bool destroyAfterDeath = false;
    [SerializeField] private float destroyDelay = 2f;

    [Header("Death Animation")]
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private bool setDeadBool = false;
    [SerializeField] private string deadBoolName = "Dead";

    // -------- Objects toggling --------
    [Header("Objects On Start (apply when boss STARTS)")]
    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;

    [Header("Objects On Death")]
    [SerializeField] private GameObject[] objectsToEnableOnDeath;
    [SerializeField] private GameObject[] objectsToDisableOnDeath;

    // -------- Events --------
    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<int> onDamaged;
    public UnityEvent onStarted; // ยิงเมื่อบอสเริ่มทำงานจริง

    // -------- Runtime --------
    private bool running;
    private Coroutine loopCo;
    private AttackEntry lastChosen;
    private int currentHealth;
    private bool isDead;

    private bool hasBeenTriggered = false;   // ป้องกันกดซ้ำ
    private float pendingDelay = 0f;         // ดีเลย์ที่จะใช้หลังจบอินโทร (ถ้ารอ Event)

    // ---------- Enums ----------
    public enum HZone { Left, Mid, Right }
    public enum VZone { Ground, Air }

    [System.Flags] public enum HMask { None = 0, Left = 1, Mid = 2, Right = 4, Any = Left | Mid | Right }
    [System.Flags] public enum VMask { None = 0, Ground = 1, Air = 2, Any = Ground | Air }

    [System.Serializable]
    public class AttackEntry
    {
        [Tooltip("Trigger ใน Animator ของท่านี้")]
        public string triggerName = "Attack";
        [Tooltip("ท่านี้ใช้ได้ในโซนแนวนอนใดบ้าง")]
        public HMask horizontal = HMask.Any;
        [Tooltip("ท่านี้ใช้ได้ในโซนแนวตั้งใดบ้าง")]
        public VMask vertical = VMask.Any;
        [Tooltip("น้ำหนักสุ่ม (ยิ่งมากยิ่งถูกเลือกบ่อย)")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("ดาเมจของท่านี้ (ใช้ใน OnAttackHit)")]
        public int damage = 1;
    }

    private void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (animator == null) animator = GetComponentInChildren<Animator>();

        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;

        // ถ้าไม่ใช่โหมดกดปุ่ม ก็เริ่มอัตโนมัติ (คงพฤติกรรมเก่า)
        if (!startByButton)
        {
            InternalTriggerStart(delayOverride: 0f);
        }
        // ถ้าเป็นโหมดกดปุ่ม: จะรอให้เรียก TriggerBossStart(...)/ปุ่ม debug
    }

    private void Update()
    {
        if (allowDebugKeyTrigger && !hasBeenTriggered && !isDead && Input.GetKeyDown(debugTriggerKey))
        {
            TriggerBossStartWithDelay(manualStartDelay);
        }
    }

    // =======================
    //       PUBLIC API
    // =======================

    /// <summary>
    /// เรียกจากปุ่ม/ไดอาลอก: เริ่มบอสหลังหน่วงตาม manualStartDelay
    /// </summary>
    public void TriggerBossStart()
    {
        TriggerBossStartWithDelay(manualStartDelay);
    }

    /// <summary>
    /// เรียกจากปุ่ม/ไดอาลอก: เริ่มบอสหลังหน่วง seconds วินาที
    /// </summary>
    public void TriggerBossStartWithDelay(float seconds)
    {
        InternalTriggerStart(delayOverride: Mathf.Max(0f, seconds));
    }

    public bool IsStarted => running || hasBeenTriggered;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // =======================
    //    INTERNAL START FLOW
    // =======================
    private void InternalTriggerStart(float delayOverride)
    {
        if (hasBeenTriggered || isDead) return;
        hasBeenTriggered = true;

        // จัดการอ็อบเจ็กต์ตอนเริ่ม (ตอนนี้เพิ่งเริ่มจริง)
        foreach (var go in objectsToEnableOnStart) if (go) go.SetActive(true);
        foreach (var go in objectsToDisableOnStart) if (go) go.SetActive(false);

        // เล่นอนิเมชันเริ่ม (ถ้าเปิด)
        if (playSpawnOnStart && animator != null && !string.IsNullOrEmpty(spawnTriggerName))
        {
            animator.SetTrigger(spawnTriggerName);

            if (waitForSpawnEvent)
            {
                // รอ Animation Event เรียก OnSpawnIntroFinished() แล้วค่อยหน่วง delayOverride
                pendingDelay = delayOverride;
                return;
            }
            else
            {
                // ไม่รอ Event: รวมดีเลย์ทั้งหมด = delayOverride + startDelayAfterSpawn
                float totalDelay = Mathf.Max(0f, delayOverride) + Mathf.Max(0f, startDelayAfterSpawn);
                StartCoroutine(StartAfterDelay(totalDelay));
                return;
            }
        }

        // ไม่มีอนิเมชันเริ่ม: หน่วง delayOverride แล้วเริ่ม AI
        StartCoroutine(StartAfterDelay(Mathf.Max(0f, delayOverride)));
    }

    private IEnumerator StartAfterDelay(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        StartAI();
    }

    /// <summary>
    /// เรียกจาก Animation Event ตอนท้ายอนิเมชันเปิดตัว
    /// </summary>
    public void OnSpawnIntroFinished()
    {
        if (!hasBeenTriggered || isDead) return; // ยังไม่ถูกสั่งเริ่ม หรือบอสตายไปแล้ว
        if (!running) StartCoroutine(StartAfterDelay(Mathf.Max(0f, pendingDelay)));
    }

    // =======================
    //        AI CORE
    // =======================
    public void StartAI()
    {
        if (running || isDead) return;
        running = true;

        onStarted?.Invoke(); // แจ้งคนอื่นว่าบอสเริ่มแล้ว
        loopCo = StartCoroutine(AttackLoop());
    }

    public void StopAI()
    {
        if (!running) return;
        running = false;
        if (loopCo != null) StopCoroutine(loopCo);
    }

    private IEnumerator AttackLoop()
    {
        while (running && !isDead)
        {
            float wait = Random.Range(minAttackInterval, maxAttackInterval);
            yield return new WaitForSeconds(wait);

            if (!running || isDead || player == null) continue;

            var hz = GetHorizontalZone();
            var vz = GetVerticalZone();

            var choice = ChooseAttack(hz, vz);
            if (choice == null) continue;

            lastChosen = choice;
            animator?.SetTrigger(choice.triggerName);
        }
    }

    // =======================
    //       ZONE / CHOOSER
    // =======================
    private HZone GetHorizontalZone()
    {
        if (roomBounds == null || player == null)
            return (player.position.x < transform.position.x) ? HZone.Left : HZone.Right;

        Bounds b = roomBounds.bounds;
        float t = Mathf.InverseLerp(b.min.x, b.max.x, player.position.x);
        if (t < leftSplit) return HZone.Left;
        if (t > rightSplit) return HZone.Right;
        return HZone.Mid;
    }

    private VZone GetVerticalZone()
    {
        if (player == null) return VZone.Ground;
        float dy = player.position.y - groundY;
        return (dy <= groundTolerance) ? VZone.Ground : VZone.Air;
    }

    private AttackEntry ChooseAttack(HZone hz, VZone vz)
    {
        if (attackTable == null || attackTable.Length == 0) return null;
        List<AttackEntry> pool = new List<AttackEntry>();

        foreach (var a in attackTable)
        {
            if (a == null || a.weight <= 0f) continue;

            bool hOK = (a.horizontal & ToMask(hz)) != HMask.None;
            bool vOK = (a.vertical & ToMask(vz)) != VMask.None;

            if (hOK && vOK) pool.Add(a);
        }

        if (pool.Count == 0) return null;
        if (pool.Count == 1) return pool[0];

        float sum = 0f;
        foreach (var a in pool) sum += Mathf.Max(0f, a.weight);
        float r = Random.value * sum;
        foreach (var a in pool)
        {
            r -= Mathf.Max(0f, a.weight);
            if (r <= 0f) return a;
        }
        return pool[pool.Count - 1];
    }

    private HMask ToMask(HZone z) => (z == HZone.Left) ? HMask.Left : (z == HZone.Mid ? HMask.Mid : HMask.Right);
    private VMask ToMask(VZone z) => (z == VZone.Ground) ? VMask.Ground : VMask.Air;

    // =======================
    //     ATTACK HIT EVENT
    // =======================
    public void OnAttackHit()
    {
        if (isDead) return;

        Vector2 worldPos = (Vector2)transform.position + hitOffset;
        var hit = Physics2D.OverlapCircle(worldPos, hitRadius, playerLayer);
        if (!hit) return;

        var dmg = hit.GetComponent<IDamageable>();
        int amount = (lastChosen != null ? lastChosen.damage : 1);

        if (dmg != null) dmg.TakeDamage(amount);
        else
        {
            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.SendMessage("TakeDamage", amount, SendMessageOptions.DontRequireReceiver);
        }
    }

    // =======================
    //     DAMAGE / DEATH
    // =======================
    public void TakeDamage(int amount)
    {
        if (isDead || invulnerable || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        onDamaged?.Invoke(currentHealth);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAI();

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deathTriggerName))
                animator.SetTrigger(deathTriggerName);
            if (setDeadBool && !string.IsNullOrEmpty(deadBoolName))
                animator.SetBool(deadBoolName, true);
        }

        foreach (var go in objectsToEnableOnDeath) if (go) go.SetActive(true);
        foreach (var go in objectsToDisableOnDeath) if (go) go.SetActive(false);

        onDeath?.Invoke();

        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = false;

        if (destroyAfterDeath) Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        else enabled = false;
    }

    // =======================
    //        GIZMOS
    // =======================
    private void OnDrawGizmosSelected()
    {
        if (roomBounds != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawCube(roomBounds.bounds.center, roomBounds.bounds.size);

            Bounds b = roomBounds.bounds;
            float xL = Mathf.Lerp(b.min.x, b.max.x, leftSplit);
            float xR = Mathf.Lerp(b.min.x, b.max.x, rightSplit);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(xL, b.min.y, 0), new Vector3(xL, b.max.y, 0));
            Gizmos.DrawLine(new Vector3(xR, b.min.y, 0), new Vector3(xR, b.max.y, 0));
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + hitOffset, hitRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-999f, groundY, 0), new Vector3(999f, groundY, 0));
    }
}

public interface IDamageable { void TakeDamage(int amount); }
