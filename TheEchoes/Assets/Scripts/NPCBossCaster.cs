using System.Collections;
using UnityEngine;
using TMPro;

public class NPCBossCaster : MonoBehaviour
{
    public BossStationaryAI boss;

    [Header("Magic Settings")]
    public GameObject magicBallPrefab;
    public Transform castPoint;
    public float ballSpeed = 8f;
    public float ballArriveDistance = 0.2f;

    [Header("Timing")]
    public float firstDelay = 3f;
    public Vector2 castInterval = new Vector2(5f, 8f);
    public float castWindup = 1.2f;

    [Header("Dialogue (multi lines)")]
    public string[] linesBossSpawn = {};
    public string[] linesBeforeCast = {};
    public string[] linesArmDestroyed = {};
    public string[] linesBossDead = {};

    [Header("Dialogue UI")]
    public GameObject speechRoot;
    public TMP_Text speechText;
    public float charDelay = 0.04f;
    public float stayTime = 0.6f;
    public float lineGap = 0.25f;

    private Coroutine speakCo;
    private Coroutine castLoopCo;
    private bool hasStartedCast = false;

    void Awake()
    {
        PlayerHealth.OnPlayerRespawned += HandlePlayerRespawned;
    }

    void OnDestroy()
    {
        PlayerHealth.OnPlayerRespawned -= HandlePlayerRespawned;

        if (boss != null)
        {
            boss.OnBossRevealStart -= OnBossRevealStart;
            boss.OnBossArmDestroyed -= OnBossArmDestroyed;
            boss.OnBossDied -= OnBossDied;
        }
    }

    void Start()
    {
        if (boss != null)
        {
            boss.OnBossRevealStart += OnBossRevealStart;
            boss.OnBossArmDestroyed += OnBossArmDestroyed;
            boss.OnBossDied += OnBossDied;
        }

        if (speechRoot != null)
            speechRoot.SetActive(false);
    }

    void HandlePlayerRespawned()
    {
        StopAllCoroutines();
        castLoopCo = null;
        hasStartedCast = false;

        if (speechRoot != null)
            speechRoot.SetActive(false);

        StopTypingSound();

        if (boss != null)
        {
            boss.OnBossRevealStart -= OnBossRevealStart;
            boss.OnBossArmDestroyed -= OnBossArmDestroyed;
            boss.OnBossDied -= OnBossDied;

            boss.OnBossRevealStart += OnBossRevealStart;
            boss.OnBossArmDestroyed += OnBossArmDestroyed;
            boss.OnBossDied += OnBossDied;
        }
    }

    void OnBossRevealStart()
    {
        SpeakLines(linesBossSpawn);

        if (!hasStartedCast && boss != null && !boss.IsDead)
        {
            hasStartedCast = true;
            castLoopCo = StartCoroutine(CastLoop());
        }
    }

    void OnBossArmDestroyed()
    {
        SpeakLines(linesArmDestroyed);
    }

    void OnBossDied()
    {
        SpeakLines(linesBossDead);

        if (castLoopCo != null)
        {
            StopCoroutine(castLoopCo);
            castLoopCo = null;
        }
    }

    IEnumerator CastLoop()
    {
        if (firstDelay > 0)
            yield return new WaitForSeconds(firstDelay);

        while (boss != null && !boss.IsDead)
        {
            SpeakLines(linesBeforeCast);

            if (castWindup > 0)
                yield return new WaitForSeconds(castWindup);

            int armIndex = boss.GetRandomAliveArmIndex();
            if (armIndex != -1)
            {
                Transform armTarget = boss.GetArmTransform(armIndex);
                if (armTarget != null)
                {
                    PlaySFX("BossMagicFly");
                    yield return StartCoroutine(ShootBallToArm(armTarget, armIndex));
                }
            }

            if (boss == null || boss.IsDead) break;

            float wait = Random.Range(castInterval.x, castInterval.y);
            yield return new WaitForSeconds(wait);
        }
    }

    IEnumerator ShootBallToArm(Transform armTarget, int armIndex)
    {
        Vector3 spawnPos = castPoint ? castPoint.position : transform.position;
        GameObject ballObj = null;

        if (magicBallPrefab != null)
            ballObj = Instantiate(magicBallPrefab, spawnPos, Quaternion.identity);

        Vector3 currentPos = spawnPos;

        while (true)
        {
            if (boss == null || boss.IsDead) break;
            if (armTarget == null) break;

            Vector3 dir = armTarget.position - currentPos;
            float dist = dir.magnitude;
            if (dist <= ballArriveDistance)
            {
                PlaySFX("BossMagicImpact");
                boss.DestroyArm(armIndex);
                break;
            }

            dir.Normalize();
            currentPos += dir * ballSpeed * Time.deltaTime;

            if (ballObj != null)
            {
                ballObj.transform.position = currentPos;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                ballObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            yield return null;
        }

        if (ballObj != null)
            Destroy(ballObj);
    }

    public void SpeakLines(string[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        if (speakCo != null) StopCoroutine(speakCo);
        speakCo = StartCoroutine(SpeakLinesRoutine(lines));
    }

    IEnumerator SpeakLinesRoutine(string[] lines)
    {
        if (speechRoot != null)
            speechRoot.SetActive(true);

        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            if (speechText != null)
                speechText.text = "";

            StartTypingSound();

            for (int i = 0; i < line.Length; i++)
            {
                if (speechText != null)
                    speechText.text += line[i];
                yield return new WaitForSeconds(charDelay);
            }

            StopTypingSound();
            yield return new WaitForSeconds(stayTime);

            if (li < lines.Length - 1 && lineGap > 0f)
                yield return new WaitForSeconds(lineGap);
        }

        if (speechRoot != null)
            speechRoot.SetActive(false);

        speakCo = null;
    }

    void StartTypingSound()
    {
        if (SoundManager.instance == null) return;
        if (SoundManager.instance.effectSource == null) return;
        if (SoundManager.instance.typing == null) return;

        var src = SoundManager.instance.effectSource;
        src.loop = true;
        src.clip = SoundManager.instance.typing;
        src.Play();
    }

    void StopTypingSound()
    {
        if (SoundManager.instance == null) return;
        if (SoundManager.instance.effectSource == null) return;
        var src = SoundManager.instance.effectSource;
        if (src.isPlaying)
            src.Stop();
        src.loop = false;
        src.clip = null;
    }

    void PlaySFX(string name)
    {
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFX(name);
    }
}