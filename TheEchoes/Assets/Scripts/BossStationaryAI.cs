using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStationaryAI : MonoBehaviour
{
    [Header("Boss ID")]
    public string bossId = "boss_1";

    [Header("Player / Trigger")]
    public Transform player;
    public Collider2D triggerZone;
    public bool startWhenPlayerEnterZone = true;
    public float startDelayAfterEnter = 0.5f;

    [Header("Arms")]
    public Transform[] arms;

    [Header("Idle Motion")]
    public float idleAmplitude = 25f;
    public float idleSpeed = 1.2f;
    public float phaseOffset = Mathf.PI / 3f;

    [Header("Reveal Arms")]
    public bool revealArmsSequentially = true;
    public float revealStartDelay = 1.0f;
    public float revealInterval = 0.6f;

    [Header("Group 1: TopDown Slam")]
    public bool enableGroup1_Slam = true;
    public float hoverOffsetY = 2.5f;
    public float slamRiseTime = 0.35f;
    public float hoverDuration = 0.4f;
    public float hoverHoldBeforeSlam = 0.15f;
    public float hoverFollowSpeed = 4f;
    public float slamDownTime = 0.2f;
    public float slamReturnTime = 0.35f;
    public float slamArmAngleLeft = -90f;
    public float slamArmAngleRight = 90f;

    [Header("Group 2: Horizontal Sweep")]
    public bool enableGroup2 = true;
    public float g2_sweepHold = 0.15f;
    public float g2_sweepSpeed = 8f;
    public float g2_sweepYOffset = 0f;
    public float g2_returnTime = 0.35f;
    public float g2_sidePadding = 0.3f;

    [Header("Group 3: Diagonal Slam")]
    public bool enableGroup3 = true;
    public float g3_offsetX = 2.5f;
    public float g3_offsetY = 2.0f;
    public float g3_riseTime = 0.35f;
    public float g3_followDuration = 0.4f;
    public float g3_followSpeed = 3.5f;
    public float g3_holdBeforeSlam = 0.12f;
    public float g3_slamTime = 0.25f;
    public float g3_returnTime = 0.4f;
    public float g3_angleLeft = -45f;
    public float g3_angleRight = 45f;

    [Header("Hit / Bounds")]
    public LayerMask playerLayer;
    public float zoneTopPadding = 0.4f;
    public float zoneBottomPadding = 0.2f;
    public float slamHitRadius = 0.75f;
    public int slamDamage = 1;
    public float g2_hitRadius = 0.6f;
    public int g2_damage = 1;
    public float g3_hitRadius = 0.7f;
    public int g3_damage = 1;

    [Header("Attack Selector")]
    public Vector2 attackSelectInterval = new Vector2(10f, 20f);

    [Header("Arm Destroy")]
    public bool disableArmObjectOnDestroy = true;

    [Header("Boss Start / End Objects")]
    public GameObject[] activateOnStart;
    public GameObject[] deactivateOnStart;
    public GameObject[] activateOnDeath;
    public GameObject[] deactivateOnDeath;

    [Header("Camera Control")]
    public Camera mainCamera;
    public Camera bossCamera;
    public bool switchCameraOnStart = true;
    public bool switchBackOnDeath = true;

    [Header("UI Canvas Switching")]
    public Canvas uiCanvas;
    public bool switchUICanvasWithCamera = true;

    [Header("Canvas Plane Distance")]
    public float mainCanvasPlaneDistance = 100f;
    public float bossCanvasPlaneDistance = 200f;

    public event Action OnBossRevealStart;
    public event Action OnBossArmDestroyed;
    public event Action OnBossDied;

    private bool aiRunning = false;
    private bool hasTriggered = false;
    private bool isDead = false;
    private HashSet<Transform> armsInAction = new HashSet<Transform>();
    private bool[] armAlive;

    private Transform[] armOriginalParents;
    private Vector3[] armOriginalLocalPos;
    private Quaternion[] armOriginalLocalRot;

    private bool suppressAutoStartOnce = false;

    private bool[] init_activateOnStartStates;
    private bool[] init_deactivateOnStartStates;
    private bool[] init_activateOnDeathStates;
    private bool[] init_deactivateOnDeathStates;

    public bool IsDead => isDead;

    void Awake()
    {
        if (string.IsNullOrEmpty(bossId))
            bossId = gameObject.name;

        PlayerHealth.OnPlayerRespawned += HandlePlayerRespawned;
    }

    void OnDestroy()
    {
        PlayerHealth.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (arms != null && arms.Length > 0)
        {
            armAlive = new bool[arms.Length];
            armOriginalParents = new Transform[arms.Length];
            armOriginalLocalPos = new Vector3[arms.Length];
            armOriginalLocalRot = new Quaternion[arms.Length];

            for (int i = 0; i < arms.Length; i++)
            {
                armAlive[i] = (arms[i] != null);
                if (arms[i] != null)
                {
                    armOriginalParents[i] = arms[i].parent;
                    armOriginalLocalPos[i] = arms[i].localPosition;
                    armOriginalLocalRot[i] = arms[i].localRotation;
                }
            }
        }

        init_activateOnStartStates = CaptureStates(activateOnStart);
        init_deactivateOnStartStates = CaptureStates(deactivateOnStart);
        init_activateOnDeathStates = CaptureStates(activateOnDeath);
        init_deactivateOnDeathStates = CaptureStates(deactivateOnDeath);

        if (!startWhenPlayerEnterZone && !suppressAutoStartOnce)
        {
            StartBoss();
        }

        suppressAutoStartOnce = false;
    }

    void Update()
    {
        if (startWhenPlayerEnterZone && !hasTriggered && !isDead && triggerZone != null && player != null)
        {
            if (triggerZone.bounds.Contains(player.position))
            {
                hasTriggered = true;
                StartCoroutine(StartAfterDelay(startDelayAfterEnter));
            }
        }

        if (aiRunning && arms != null)
        {
            float t = Time.time;
            for (int i = 0; i < arms.Length; i++)
            {
                Transform arm = arms[i];
                if (arm == null) continue;
                if (armsInAction.Contains(arm)) continue;
                if (revealArmsSequentially && !IsArmVisible(arm)) continue;

                float angle = Mathf.Sin(t * idleSpeed + i * phaseOffset) * idleAmplitude;
                arm.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    void HandlePlayerRespawned()
    {
        HardResetToBeforeStart();
    }

    public void HardResetToBeforeStart()
    {
        StopAllCoroutines();

        aiRunning = false;
        hasTriggered = false;
        isDead = false;
        armsInAction.Clear();
        suppressAutoStartOnce = false;

        ResetArmsToOriginal(true);

        ApplyStates(activateOnStart, init_activateOnStartStates);
        ApplyStates(deactivateOnStart, init_deactivateOnStartStates);
        ApplyStates(activateOnDeath, init_activateOnDeathStates);
        ApplyStates(deactivateOnDeath, init_deactivateOnDeathStates);

        SwitchToMainCamera();

        if (!startWhenPlayerEnterZone)
            StartBoss();
    }

    public void ResetToPreBossForLoad()
    {
        StopAllCoroutines();

        aiRunning = false;
        hasTriggered = false;
        isDead = false;
        armsInAction.Clear();

        suppressAutoStartOnce = true;

        ResetArmsToOriginal(true);

        ApplyStates(activateOnStart, init_activateOnStartStates);
        ApplyStates(deactivateOnStart, init_deactivateOnStartStates);
        ApplyStates(activateOnDeath, init_activateOnDeathStates);
        ApplyStates(deactivateOnDeath, init_deactivateOnDeathStates);

        SwitchToMainCamera();
    }

    void ResetArmsToOriginal(bool respectReveal)
    {
        if (arms == null) return;
        for (int i = 0; i < arms.Length; i++)
        {
            Transform a = arms[i];
            if (a == null) continue;

            if (armOriginalParents != null && armOriginalParents[i] != null)
                a.SetParent(armOriginalParents[i], false);

            if (armOriginalLocalPos != null) a.localPosition = armOriginalLocalPos[i];
            if (armOriginalLocalRot != null) a.localRotation = armOriginalLocalRot[i];

            a.gameObject.SetActive(true);

            if (respectReveal)
            {
                if (revealArmsSequentially)
                    SetArmVisible(a, false);
                else
                    SetArmVisible(a, true);
            }

            if (armAlive != null) armAlive[i] = true;
        }
    }

    IEnumerator StartAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        StartBoss();
    }

    void StartBoss()
    {
        if (aiRunning || isDead) return;
        aiRunning = true;
        hasTriggered = true;

        if (switchCameraOnStart)
        {
            SwitchToBossCamera();
        }

        ToggleObjects(activateOnStart, true);
        ToggleObjects(deactivateOnStart, false);

        if (revealArmsSequentially)
        {
            HideAllArms();
            StartCoroutine(RevealArmsThenStartAttack());
        }
        else
        {
            ShowAllArms();
            StartCoroutine(AIAttackSelector());
        }
    }

    void HideAllArms()
    {
        if (arms == null) return;
        foreach (var arm in arms)
            if (arm != null)
                SetArmVisible(arm, false);
    }

    void ShowAllArms()
    {
        if (arms == null) return;
        foreach (var arm in arms)
            if (arm != null)
                SetArmVisible(arm, true);
    }

    void SetArmVisible(Transform arm, bool visible)
    {
        arm.gameObject.SetActive(true);
        var srs = arm.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
            sr.enabled = visible;
    }

    bool IsArmVisible(Transform arm)
    {
        var sr = arm.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null && sr.enabled;
    }

    IEnumerator RevealArmsThenStartAttack()
    {
        if (revealStartDelay > 0f)
            yield return new WaitForSeconds(revealStartDelay);

        OnBossRevealStart?.Invoke();

        if (arms != null)
        {
            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i] != null)
                {
                    SetArmVisible(arms[i], true);
                    PlaySFX("BossArmSpawn");
                }

                if (revealInterval > 0f)
                    yield return new WaitForSeconds(revealInterval);
            }
        }

        StartCoroutine(AIAttackSelector());
    }

    IEnumerator AIAttackSelector()
    {
        while (aiRunning && !isDead)
        {
            float wait = UnityEngine.Random.Range(attackSelectInterval.x, attackSelectInterval.y);
            yield return new WaitForSeconds(wait);

            while (armsInAction.Count > 0 && aiRunning && !isDead)
                yield return null;

            List<int> availableGroups = new List<int>();
            if (enableGroup1_Slam && GroupHasUsableArm(0, 1)) availableGroups.Add(1);
            if (enableGroup2 && GroupHasUsableArm(2, 3)) availableGroups.Add(2);
            if (enableGroup3 && GroupHasUsableArm(4, 5)) availableGroups.Add(3);

            if (availableGroups.Count == 0) continue;

            int chosen = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
            switch (chosen)
            {
                case 1: AttackGroup1(); break;
                case 2: AttackGroup2(); break;
                case 3: AttackGroup3(); break;
            }
        }
    }

    bool GroupHasUsableArm(int start, int end)
    {
        if (arms == null || armAlive == null) return false;
        for (int i = start; i <= end; i++)
        {
            if (i < 0 || i >= arms.Length) continue;
            if (arms[i] == null) continue;
            if (!armAlive[i]) continue;
            if (armsInAction.Contains(arms[i])) continue;
            if (revealArmsSequentially && !IsArmVisible(arms[i])) continue;
            return true;
        }
        return false;
    }

    void AttackGroup1()
    {
        List<int> usable = new List<int>();
        for (int i = 0; i <= 1; i++)
            if (GroupHasUsableArm(i, i)) usable.Add(i);
        if (usable.Count == 0) return;
        int idx = usable[UnityEngine.Random.Range(0, usable.Count)];
        StartCoroutine(TopDownSlamAttack(arms[idx], player, idx));
    }

    void AttackGroup2()
    {
        List<int> usable = new List<int>();
        for (int i = 2; i <= 3; i++)
            if (GroupHasUsableArm(i, i)) usable.Add(i);
        if (usable.Count == 0) return;
        int idx = usable[UnityEngine.Random.Range(0, usable.Count)];
        bool startFromLeft = (idx == 2);
        StartCoroutine(HorizontalSweepAttack(arms[idx], player, startFromLeft));
    }

    void AttackGroup3()
    {
        List<int> usable = new List<int>();
        for (int i = 4; i <= 5; i++)
            if (GroupHasUsableArm(i, i)) usable.Add(i);
        if (usable.Count == 0) return;
        int idx = usable[UnityEngine.Random.Range(0, usable.Count)];
        bool fromLeft = (idx == 4);
        StartCoroutine(DiagonalSlamAttack(arms[idx], player, fromLeft));
    }

    IEnumerator TopDownSlamAttack(Transform arm, Transform target, int armIndex)
    {
        armsInAction.Add(arm);

        Transform originalParent = arm.parent;
        Vector3 originalPos = arm.position;
        Quaternion originalRot = arm.rotation;
        arm.SetParent(null, true);

        PlaySFX("BossArmMove");

        var b = triggerZone.bounds;
        float zoneTop = b.max.y - zoneTopPadding;
        float zoneBottom = b.min.y + zoneBottomPadding;
        float z = arm.position.z;

        float armAngle = (armIndex == 0) ? slamArmAngleLeft : slamArmAngleRight;

        float desiredHoverY = target.position.y + hoverOffsetY;
        float hoverY = Mathf.Clamp(desiredHoverY, zoneBottom, zoneTop);
        Vector3 startPos = arm.position;
        Vector3 hoverPos = new Vector3(target.position.x, hoverY, z);

        float t = 0f;
        while (t < slamRiseTime)
        {
            t += Time.deltaTime;
            float p = t / slamRiseTime;
            arm.position = Vector3.Lerp(startPos, hoverPos, p);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        float h = 0f;
        while (h < hoverDuration)
        {
            h += Time.deltaTime;
            Vector3 follow = new Vector3(target.position.x, hoverY, z);
            arm.position = Vector3.Lerp(arm.position, follow, Time.deltaTime * hoverFollowSpeed);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        float hold = hoverHoldBeforeSlam;
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        Vector3 slamPos = new Vector3(arm.position.x, zoneBottom, z);
        t = 0f;
        Vector3 pre = arm.position;
        while (t < slamDownTime)
        {
            t += Time.deltaTime;
            float p = t / slamDownTime;
            arm.position = Vector3.Lerp(pre, slamPos, p);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        PlaySFX("BossArmSlam");
        DoDamageToPlayerAt(arm.position, slamHitRadius, slamDamage);

        t = 0f;
        while (t < slamReturnTime)
        {
            t += Time.deltaTime;
            float p = t / slamReturnTime;
            arm.position = Vector3.Lerp(slamPos, originalPos, p);
            arm.rotation = Quaternion.Slerp(arm.rotation, originalRot, p);
            yield return null;
        }

        arm.SetParent(originalParent, true);
        arm.position = originalPos;
        arm.rotation = originalRot;
        armsInAction.Remove(arm);

        PlaySFX("BossArmMove");
    }

    IEnumerator HorizontalSweepAttack(Transform arm, Transform target, bool startFromLeft)
    {
        armsInAction.Add(arm);

        Transform originalParent = arm.parent;
        Vector3 originalPos = arm.position;
        Quaternion originalRot = arm.rotation;
        arm.SetParent(null, true);

        PlaySFX("BossArmMove");

        var b = triggerZone.bounds;
        float leftX = b.min.x + g2_sidePadding;
        float rightX = b.max.x - g2_sidePadding;
        float zoneTop = b.max.y - zoneTopPadding;
        float zoneBottom = b.min.y + zoneBottomPadding;
        float z = arm.position.z;

        float desiredY = target.position.y + g2_sweepYOffset;
        float sweepY = Mathf.Clamp(desiredY, zoneBottom, zoneTop);

        Vector3 startPos = startFromLeft ? new Vector3(leftX, sweepY, z) : new Vector3(rightX, sweepY, z);
        Vector3 endPos = startFromLeft ? new Vector3(rightX, sweepY, z) : new Vector3(leftX, sweepY, z);

        float t = 0f;
        float moveTime = 0.25f;
        Vector3 curStart = arm.position;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float p = t / moveTime;
            arm.position = Vector3.Lerp(curStart, startPos, p);
            yield return null;
        }

        float hold = g2_sweepHold;
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            yield return null;
        }

        PlaySFX("BossArmSweep");

        float dist = Vector3.Distance(startPos, endPos);
        float traveled = 0f;
        while (traveled < dist)
        {
            float step = g2_sweepSpeed * Time.deltaTime;
            traveled += step;
            float p = Mathf.Clamp01(traveled / dist);
            arm.position = Vector3.Lerp(startPos, endPos, p);

            Collider2D hit = Physics2D.OverlapCircle(arm.position, g2_hitRadius, playerLayer);
            if (hit != null)
            {
                var ph = hit.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeHit(g2_damage);
            }

            yield return null;
        }

        t = 0f;
        while (t < g2_returnTime)
        {
            t += Time.deltaTime;
            float p = t / g2_returnTime;
            arm.position = Vector3.Lerp(endPos, originalPos, p);
            arm.rotation = Quaternion.Slerp(arm.rotation, originalRot, p);
            yield return null;
        }

        arm.SetParent(originalParent, true);
        arm.position = originalPos;
        arm.rotation = originalRot;
        armsInAction.Remove(arm);

        PlaySFX("BossArmMove");
    }

    IEnumerator DiagonalSlamAttack(Transform arm, Transform target, bool fromLeft)
    {
        armsInAction.Add(arm);

        Transform originalParent = arm.parent;
        Vector3 originalPos = arm.position;
        Quaternion originalRot = arm.rotation;
        arm.SetParent(null, true);

        PlaySFX("BossArmMove");

        var b = triggerZone.bounds;
        float zoneTop = b.max.y - zoneTopPadding;
        float zoneBottom = b.min.y + zoneBottomPadding;
        float z = arm.position.z;

        float startX = target.position.x + (fromLeft ? -g3_offsetX : g3_offsetX);
        float startY = Mathf.Clamp(target.position.y + g3_offsetY, zoneBottom, zoneTop);
        Vector3 hover = new Vector3(startX, startY, z);

        float armAngle = fromLeft ? g3_angleLeft : g3_angleRight;

        float t = 0f;
        Vector3 startPos = arm.position;
        while (t < g3_riseTime)
        {
            t += Time.deltaTime;
            float p = t / g3_riseTime;
            arm.position = Vector3.Lerp(startPos, hover, p);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        float ft = 0f;
        while (ft < g3_followDuration)
        {
            ft += Time.deltaTime;

            float fx = target.position.x + (fromLeft ? -g3_offsetX : g3_offsetX);
            float fy = Mathf.Clamp(target.position.y + g3_offsetY, zoneBottom, zoneTop);
            Vector3 follow = new Vector3(fx, fy, z);

            arm.position = Vector3.Lerp(arm.position, follow, Time.deltaTime * g3_followSpeed);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        Vector3 finalHover = arm.position;

        float hold = g3_holdBeforeSlam;
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            arm.position = finalHover;
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        float verticalDrop = finalHover.y - zoneBottom;
        float dx = fromLeft ? verticalDrop : -verticalDrop;
        Vector3 slamTarget = new Vector3(finalHover.x + dx, zoneBottom, z);

        t = 0f;
        Vector3 pre = arm.position;
        while (t < g3_slamTime)
        {
            t += Time.deltaTime;
            float p = t / g3_slamTime;
            arm.position = Vector3.Lerp(pre, slamTarget, p);
            arm.rotation = Quaternion.Euler(0, 0, armAngle);
            yield return null;
        }

        PlaySFX("BossArmSlam");
        DoDamageToPlayerAt(arm.position, g3_hitRadius, g3_damage);

        t = 0f;
        while (t < g3_returnTime)
        {
            t += Time.deltaTime;
            float p = t / g3_returnTime;
            arm.position = Vector3.Lerp(slamTarget, originalPos, p);
            arm.rotation = Quaternion.Slerp(arm.rotation, originalRot, p);
            yield return null;
        }

        arm.SetParent(originalParent, true);
        arm.position = originalPos;
        arm.rotation = originalRot;
        armsInAction.Remove(arm);

        PlaySFX("BossArmMove");
    }

    void DoDamageToPlayerAt(Vector3 pos, float radius, int dmg)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, radius, playerLayer);
        if (hit != null)
        {
            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeHit(dmg);
        }
    }

    void PlaySFX(string name)
    {
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFX(name);
    }

    void ToggleObjects(GameObject[] list, bool active)
    {
        if (list == null) return;
        foreach (var go in list)
        {
            if (go != null)
                go.SetActive(active);
        }
    }

    public int GetRandomAliveArmIndex()
    {
        if (arms == null || armAlive == null) return -1;
        List<int> list = new List<int>();
        for (int i = 0; i < arms.Length; i++)
            if (IsArmStillAlive(i)) list.Add(i);
        if (list.Count == 0) return -1;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public Transform GetArmTransform(int index)
    {
        if (arms == null) return null;
        if (index < 0 || index >= arms.Length) return null;
        return arms[index];
    }

    public void DestroyArm(int index)
    {
        if (arms == null || armAlive == null) return;
        if (index < 0 || index >= arms.Length) return;
        if (!armAlive[index]) return;

        armAlive[index] = false;

        if (armsInAction.Contains(arms[index]))
            armsInAction.Remove(arms[index]);

        if (disableArmObjectOnDestroy && arms[index] != null)
            arms[index].gameObject.SetActive(false);

        OnBossArmDestroyed?.Invoke();

        if (CountAliveArms() == 0)
            Die();
    }

    bool IsArmStillAlive(int index)
    {
        if (armAlive == null) return false;
        if (index < 0 || index >= armAlive.Length) return false;
        if (!armAlive[index]) return false;
        if (arms[index] == null) return false;
        return true;
    }

    int CountAliveArms()
    {
        if (armAlive == null) return 0;
        int c = 0;
        for (int i = 0; i < armAlive.Length; i++)
            if (armAlive[i]) c++;
        return c;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        aiRunning = false;
        PlaySFX("BossDie");

        if (switchBackOnDeath)
        {
            SwitchToMainCamera();
        }

        ToggleObjects(activateOnDeath, true);
        ToggleObjects(deactivateOnDeath, false);

        OnBossDied?.Invoke();
    }

    public string GetId() => bossId;

    public BossSaveEntry BuildSave()
    {
        var entry = new BossSaveEntry();
        entry.id = bossId;
        entry.isDead = isDead;

        if (isDead)
        {
            entry.activateOnStartStates = CaptureStates(activateOnStart);
            entry.deactivateOnStartStates = CaptureStates(deactivateOnStart);
            entry.activateOnDeathStates = CaptureStates(activateOnDeath);
            entry.deactivateOnDeathStates = CaptureStates(deactivateOnDeath);
        }

        return entry;
    }

    public void ApplyBossSave(BossSaveEntry entry)
    {
        if (entry != null && entry.isDead)
        {
            StopAllCoroutines();
            isDead = true;
            aiRunning = false;
            hasTriggered = false;
            suppressAutoStartOnce = true;

            ApplyStates(activateOnStart, entry.activateOnStartStates);
            ApplyStates(deactivateOnStart, entry.deactivateOnStartStates);
            ApplyStates(activateOnDeath, entry.activateOnDeathStates);
            ApplyStates(deactivateOnDeath, entry.deactivateOnDeathStates);

            SwitchToMainCamera();
            return;
        }

        ResetToPreBossForLoad();
    }

    bool[] CaptureStates(GameObject[] list)
    {
        if (list == null) return null;
        bool[] r = new bool[list.Length];
        for (int i = 0; i < list.Length; i++)
            r[i] = list[i] != null && list[i].activeSelf;
        return r;
    }

    void ApplyStates(GameObject[] list, bool[] states)
    {
        if (list == null || states == null) return;
        for (int i = 0; i < list.Length && i < states.Length; i++)
        {
            if (list[i] != null)
                list[i].SetActive(states[i]);
        }
    }

    void SwitchToBossCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (bossCamera != null) bossCamera.gameObject.SetActive(true);

        if (switchUICanvasWithCamera && uiCanvas != null && bossCamera != null)
        {
            uiCanvas.worldCamera = bossCamera;
            uiCanvas.planeDistance = bossCanvasPlaneDistance;
        }
    }

    void SwitchToMainCamera()
    {
        if (bossCamera != null) bossCamera.gameObject.SetActive(false);
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);

        if (switchUICanvasWithCamera && uiCanvas != null && mainCamera != null)
        {
            uiCanvas.worldCamera = mainCamera;
            uiCanvas.planeDistance = mainCanvasPlaneDistance;
        }
    }
}