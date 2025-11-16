using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class CutsceneTrigger_Generic : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Cutscene ID")]
    public string customId = "";

    [SerializeField, HideInInspector] private string cutsceneIdAuto = null;

    [Header("Play Condition")]
    public bool playOnlyOncePerSave = true;

    [Header("Player Control Lock")]
    public GameObject playerRoot;
    public List<string> controlScriptNames = new() { "PlayerControl" };

    [Header("Skip (optional)")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;

    [Header("Cutscene Sets (เล่นต่อกัน 1→2→3)")]
    public List<CutsceneSet> sets = new();

    [Header("Camera (Optional Override)")]
    public Camera mainCameraOverride;

    [Header("Safety")]
    public float maxSecondsPerPlay = 30f;

    private bool playing;
    private GameObject mainCameraGO;
    private Rigidbody2D rb;
    private PlayerInput input;
    private Animator anim;
    private bool animWasEnabled;
    private RigidbodyConstraints2D rbConstraintsCache;
    private readonly List<Behaviour> disabledScripts = new();

    private const string EARTHQUAKE_SFX = "Earthquake";
    private const float EARTHQUAKE_REPEAT_INTERVAL = 0.8f;
    private Coroutine earthquakeLoopCo;

    public enum ShakeWhen { OnStart, OnEndBeforeSwitchBack, OnEndAfterSwitchBack }
    public enum ShakeTarget { ActiveCamera, MainCamera, CutsceneCamera, Both }

    [Serializable]
    public class CutsceneSet
    {
        [Header("Timeline & Camera")]
        public PlayableDirector timeline;
        public GameObject cutsceneCamera;

        [Header("Camera Use (Per Set)")]
        public bool useCutsceneCamera = true;

        [Header("Camera Follow Target (Per Set)")]
        public bool followTarget = false;
        public Transform followObject;
        public Vector3 followOffset = Vector3.zero;
        public bool restorePositionAfterFollow = true;

        [Header("Timing")]
        public float preDelay = 0f;

        [Header("Lock & Loop")]
        public float lockDurationSeconds = 10f;
        public int loopCount = 1;

        [Header("Objects Activation - On Start")]
        public List<GameObject> enableOnStart = new();
        public List<GameObject> disableOnStart = new();

        [Header("Objects Activation - On End")]
        public List<GameObject> enableOnEnd = new();
        public List<GameObject> disableOnEnd = new();

        [Header("Camera Shake (Optional)")]
        public bool shakeOnStart = false;
        public bool shakeOnEnd = false;
        public bool shakeWhilePlaying = false;
        public ShakeWhen shakeWhen = ShakeWhen.OnEndBeforeSwitchBack;
        public ShakeTarget shakeTarget = ShakeTarget.ActiveCamera;
        public float shakeDuration = 1f;
        public float shakeMagnitude = 0.2f;
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        RefreshCutsceneId();
    }

    void Start()
    {
        var mainCam = mainCameraOverride ? mainCameraOverride : FindMainCameraEvenIfInactive();
        if (mainCam) mainCameraGO = mainCam.gameObject;

        foreach (var s in sets)
            if (s != null && s.cutsceneCamera)
                s.cutsceneCamera.SetActive(false);
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RefreshCutsceneId();
        }
    }

    private void RefreshCutsceneId()
    {
        if (!string.IsNullOrEmpty(customId))
        {
            cutsceneIdAuto = customId;
        }
        else
        {
            cutsceneIdAuto = BuildAutoId();
        }
    }

    void OnDisable()
    {
        StopEarthquakeLoop();
    }

    void OnDestroy()
    {
        StopEarthquakeLoop();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!enabled) return;
        if (!col.CompareTag(playerTag)) return;

        if (playOnlyOncePerSave &&
            CutsceneStateManager.I &&
            CutsceneStateManager.I.HasSeenOrPending(cutsceneIdAuto))
            return;

        StartCoroutine(RunAllSets());
    }

    private IEnumerator RunAllSets()
    {
        if (playing) yield break;
        playing = true;

        if (!playerRoot)
            playerRoot = GameObject.FindGameObjectWithTag(playerTag);

        if (playerRoot)
        {
            rb = playerRoot.GetComponent<Rigidbody2D>();
            input = playerRoot.GetComponent<PlayerInput>();
            anim = playerRoot.GetComponent<Animator>();
        }

        if (playOnlyOncePerSave && CutsceneStateManager.I)
            CutsceneStateManager.I.FlagPending(cutsceneIdAuto);

        foreach (var set in sets)
        {
            if (set == null || set.timeline == null) continue;
            yield return RunOneSet(set);
        }

        EnsurePlayerControlOn();

        playing = false;
    }

    private IEnumerator RunOneSet(CutsceneSet set)
    {
        ApplyActivation(set.enableOnStart, true);
        ApplyActivation(set.disableOnStart, false);

        if (set.preDelay > 0f)
            yield return new WaitForSeconds(set.preDelay);

        SwitchToCutscene(set.cutsceneCamera, set.useCutsceneCamera);

        if (set.shakeOnStart)
            yield return ShakeWithEarthquake(set);

        StartCoroutine(LockPlayerForSecondsAndReturnCamera(set.lockDurationSeconds));

        Coroutine followCo = null;
        if (set.followTarget && set.followObject != null)
            followCo = StartCoroutine(FollowTargetWhileSetActive(set));

        Coroutine whileShakeCo = null;
        if (!set.followTarget && set.shakeWhilePlaying && set.timeline != null)
            whileShakeCo = StartCoroutine(ShakeWhileTimelinePlays(set, set.timeline));

        yield return LoopTimeline(set.timeline, Mathf.Max(1, set.loopCount));

        if (followCo != null) StopCoroutine(followCo);
        if (whileShakeCo != null) StopCoroutine(whileShakeCo);

        if (set.shakeOnEnd && set.shakeWhen == ShakeWhen.OnEndBeforeSwitchBack)
        {
            if (set.shakeTarget == ShakeTarget.MainCamera)
                SwitchToMain();
            else
                SwitchToCutscene(set.cutsceneCamera, set.useCutsceneCamera);

            yield return ShakeWithEarthquake(set);
            SwitchToMain();
        }
        else if (set.shakeOnEnd && set.shakeWhen == ShakeWhen.OnEndAfterSwitchBack)
        {
            SwitchToMain();
            yield return ShakeWithEarthquake(set);
        }
        else
        {
            SwitchToMain();
        }

        ApplyActivation(set.enableOnEnd, true);
        ApplyActivation(set.disableOnEnd, false);
    }

    private IEnumerator LockPlayerForSecondsAndReturnCamera(float seconds)
    {
        if (input) input.enabled = false;

        if (rb)
        {
            rbConstraintsCache = rb.constraints;
            rb.WakeUp();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (anim)
        {
            animWasEnabled = anim.enabled;
            anim.enabled = false;
        }

        disabledScripts.Clear();
        if (playerRoot && controlScriptNames != null)
        {
            var all = playerRoot.GetComponentsInChildren<Behaviour>(true);
            foreach (var name in controlScriptNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var b in all)
                {
                    if (b && b.enabled && b.GetType().Name == name)
                    {
                        b.enabled = false;
                        disabledScripts.Add(b);
                    }
                }
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, seconds));

        if (rb)
        {
            rb.constraints = rbConstraintsCache;
            rb.WakeUp();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (input) input.enabled = true;
        if (anim) anim.enabled = animWasEnabled;

        foreach (var b in disabledScripts)
            if (b) b.enabled = true;
        disabledScripts.Clear();

        SwitchToMain();
    }

    private IEnumerator LoopTimeline(PlayableDirector director, int loopCount)
    {
        if (director == null || loopCount <= 0) yield break;

        yield return PlayOnce(director);
        for (int i = 1; i < loopCount; i++)
            yield return PlayOnce(director);
    }

    private IEnumerator PlayOnce(PlayableDirector director)
    {
        if (director == null) yield break;

        bool finished = false;
        void OnStopped(PlayableDirector d) { finished = true; }

        var originalWrap = director.extrapolationMode;
        director.extrapolationMode = DirectorWrapMode.Hold;

        director.stopped += OnStopped;
        director.time = 0.0;
        director.Play();

        double duration = director.duration;
        bool hasDuration = duration > 0.0 && !double.IsInfinity(duration);
        float elapsed = 0f;
        float cap = Mathf.Max(1f, maxSecondsPerPlay);

        while (!finished)
        {
            if (allowSkip && Input.GetKeyDown(skipKey))
            {
                director.time = duration;
                director.Evaluate();
                director.Stop();
                finished = true;
                break;
            }

            if (hasDuration && director.time >= duration - 0.0001f)
            {
                director.Stop();
                finished = true;
                break;
            }

            elapsed += Time.deltaTime;
            if (!hasDuration && elapsed >= cap)
            {
                director.Stop();
                finished = true;
                break;
            }

            if (!director.playableGraph.IsValid() || director.state != PlayState.Playing)
            {
                finished = true;
                break;
            }

            yield return null;
        }

        director.stopped -= OnStopped;
        director.extrapolationMode = originalWrap;
    }

    private void ApplyActivation(List<GameObject> list, bool active)
    {
        if (list == null) return;
        foreach (var go in list)
            if (go) go.SetActive(active);
    }

    private Camera FindMainCameraEvenIfInactive()
    {
        if (Camera.main) return Camera.main;

        var all = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (var c in all)
        {
            if (c && c.CompareTag("MainCamera"))
                return c;
        }

        if (all != null && all.Length > 0) return all[0];
        return null;
    }

    private void SwitchToCutscene(GameObject cutsceneCamGO, bool useCutsceneCam)
    {
        if (!useCutsceneCam || cutsceneCamGO == null)
        {
            if (mainCameraGO) mainCameraGO.SetActive(true);
            return;
        }

        cutsceneCamGO.SetActive(true);
        if (mainCameraGO) mainCameraGO.SetActive(false);
    }

    private void SwitchToMain()
    {
        if (mainCameraGO) mainCameraGO.SetActive(true);

        foreach (var s in sets)
            if (s != null && s.cutsceneCamera)
                s.cutsceneCamera.SetActive(false);

        StopEarthquakeLoop();
    }

    private Transform GetCameraTransformForSet(CutsceneSet set)
    {
        if (set.useCutsceneCamera && set.cutsceneCamera)
            return set.cutsceneCamera.transform;
        if (mainCameraGO)
            return mainCameraGO.transform;
        return null;
    }

    private IEnumerator FollowTargetWhileSetActive(CutsceneSet set)
    {
        if (!set.followTarget || set.followObject == null)
            yield break;

        Transform cam = GetCameraTransformForSet(set);
        if (!cam) yield break;

        Vector3 originalPos = cam.position;

        while (true)
        {
            if (!set.followTarget || set.followObject == null)
                break;

            cam.position = set.followObject.position + set.followOffset;
            yield return null;
        }

        if (set.restorePositionAfterFollow)
            cam.position = originalPos;
    }

    private List<Transform> GetShakeTargets(CutsceneSet set)
    {
        List<Transform> targets = new();

        switch (set.shakeTarget)
        {
            case ShakeTarget.ActiveCamera:
                {
                    Transform active =
                        (set.useCutsceneCamera && set.cutsceneCamera && set.cutsceneCamera.activeInHierarchy)
                        ? set.cutsceneCamera.transform
                        : (mainCameraGO ? mainCameraGO.transform : null);
                    if (active) targets.Add(active);
                }
                break;

            case ShakeTarget.MainCamera:
                if (mainCameraGO) targets.Add(mainCameraGO.transform);
                break;

            case ShakeTarget.CutsceneCamera:
                if (set.useCutsceneCamera && set.cutsceneCamera)
                    targets.Add(set.cutsceneCamera.transform);
                break;

            case ShakeTarget.Both:
                if (set.useCutsceneCamera && set.cutsceneCamera)
                    targets.Add(set.cutsceneCamera.transform);
                if (mainCameraGO)
                    targets.Add(mainCameraGO.transform);
                break;
        }

        return targets;
    }

    private IEnumerator ShakeWithEarthquake(CutsceneSet set)
    {
        var targets = GetShakeTargets(set);

        bool running = true;
        earthquakeLoopCo = StartCoroutine(PlayEarthquakeLoop(() => running));

        foreach (var t in targets)
        {
            if (t) yield return ShakeTransform(t, set.shakeDuration, set.shakeMagnitude);
        }

        running = false;
        StopEarthquakeLoop();
    }

    private IEnumerator ShakeWhileTimelinePlays(CutsceneSet set, PlayableDirector director)
    {
        if (director == null) yield break;

        var targets = GetShakeTargets(set);
        if (targets == null || targets.Count == 0) yield break;

        bool running = true;

        if (earthquakeLoopCo == null)
            earthquakeLoopCo = StartCoroutine(PlayEarthquakeLoop(() => running));

        while (running)
        {
            foreach (var t in targets)
            {
                if (!t) continue;
                yield return ShakeTransform(t, set.shakeDuration, set.shakeMagnitude);
            }

            if (!director.playableGraph.IsValid() || director.state != PlayState.Playing)
                running = false;
        }

        StopEarthquakeLoop();
    }

    private IEnumerator PlayEarthquakeLoop(Func<bool> isRunning)
    {
        float elapsed = 0f;

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFX(EARTHQUAKE_SFX);

        while (isRunning())
        {
            elapsed += Time.deltaTime;

            if (elapsed >= EARTHQUAKE_REPEAT_INTERVAL)
            {
                elapsed = 0f;
                if (SoundManager.instance != null)
                    SoundManager.instance.PlaySFX(EARTHQUAKE_SFX);
            }

            yield return null;
        }
    }

    private void StopEarthquakeLoop()
    {
        if (earthquakeLoopCo != null)
        {
            StopCoroutine(earthquakeLoopCo);
            earthquakeLoopCo = null;
        }
    }

    private IEnumerator ShakeTransform(Transform cam, float duration, float magnitude)
    {
        if (!cam) yield break;

        Vector3 originalPos = cam.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float offsetY = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            cam.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = originalPos;
    }

    private string BuildAutoId()
    {
        var scene = SceneManager.GetActiveScene().name;
        var path = GetHierarchyPath(transform);
        return $"{scene}/{path}";
    }

    private static string GetHierarchyPath(Transform t)
    {
        List<string> names = new();
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private void EnsurePlayerControlOn()
    {
        if (!playerRoot)
            playerRoot = GameObject.FindGameObjectWithTag(playerTag);
        if (!playerRoot) return;

        if (!input) input = playerRoot.GetComponent<PlayerInput>();
        if (!anim) anim = playerRoot.GetComponent<Animator>();
        if (!rb) rb = playerRoot.GetComponent<Rigidbody2D>();

        if (input) input.enabled = true;
        if (anim) anim.enabled = true;

        if (controlScriptNames != null && controlScriptNames.Count > 0)
        {
            var all = playerRoot.GetComponentsInChildren<Behaviour>(true);
            foreach (var name in controlScriptNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var b in all)
                {
                    if (b != null && b.GetType().Name == name)
                        b.enabled = true;
                }
            }
        }
    }
}