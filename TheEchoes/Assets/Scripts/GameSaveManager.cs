using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    public static event Action OnSlotApplied;

    [Header("Save Settings")]
    public Transform player;
    public float saveCooldownSeconds = 0.75f;

    [Header("Preview Capture")]
    public int previewWidth = 130;
    public int previewHeight = 80;

    double sessionPlaySeconds = 0;
    float sessionStartTime;
    float lastSaveTime = -999f;

    private void OnEnable()
    {
        sessionStartTime = Time.unscaledTime;
    }

    private void OnDisable()
    {
        sessionPlaySeconds += (Time.unscaledTime - sessionStartTime);
    }

    private void Start()
    {
        ApplyFromCurrentSlot(true);
    }

    public bool ApplyFromCurrentSlot(bool teleportPlayer)
    {
        int slot = GlobalGame.CurrentSlot;
        var data = SaveDataSystem.Load(slot);
        if (data == null) return false;

        if (DialogueRuntime.Instance)
        {
            DialogueRuntime.Instance.LoadFromSave(data.dialogue);
            DialogueRuntime.Instance.RefreshSceneForCurrentSlot();
        }

        var bossesInScene = FindObjectsOfType<BossStationaryAI>(true);
        if (bossesInScene != null)
        {
            var bossSave = data.bosses ?? new BossesSave();
            foreach (var b in bossesInScene)
            {
                if (b == null) continue;
                var entry = bossSave.Get(b.GetId());
                b.ApplyBossSave(entry);
            }
        }

        if (data.scene == SceneManager.GetActiveScene().name && teleportPlayer && player)
            player.position = new Vector3(data.posX, data.posY, data.posZ);

        if (player)
        {
            var consume = player.GetComponent<ConsumeControl>();
            if (consume != null && data.morph != null)
            {
                var snap = new ConsumeControl.ConsumeSnapshot
                {
                    controllerName = data.morph.controllerName,
                    morphTag = data.morph.morphTag,
                    timeLeft = data.morph.timeLeft,
                    isMorphing = data.morph.isMorphing,
                    hasStored = data.morph.hasStored
                };
                consume.ApplySnapshot(snap);
            }

            var lumerin = player.GetComponentInChildren<LumerinAbility>(true);
            if (lumerin != null && data.lumerin != null)
                lumerin.SetCurrentBoost(data.lumerin.currentBoost);

            var ramble = player.GetComponentInChildren<RambleAbility>(true);
            if (ramble != null && data.ramble != null)
                ramble.ApplySave(data.ramble);

            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (data.health != null)
                {
                    PlayerHealth.lastCheckpointPosition = new Vector3(
                        data.health.checkpointX, data.health.checkpointY, data.health.checkpointZ);
                    health.SetHits(Mathf.Clamp(data.health.hits, 0, 5), true);
                }
                else
                {
                    PlayerHealth.lastCheckpointPosition = player.position;
                    health.RestoreFullHealth();
                }
            }
        }

        sessionStartTime = Time.unscaledTime;

        OnSlotApplied?.Invoke();

        return true;
    }

    public void SaveNow()
    {
        if (Time.unscaledTime - lastSaveTime < saveCooldownSeconds) return;
        lastSaveTime = Time.unscaledTime;

        int slot = GlobalGame.CurrentSlot;
        var old = SaveDataSystem.Load(slot);
        double total = (old?.totalPlaySeconds ?? 0) + (Time.unscaledTime - sessionStartTime);

        var data = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            posX = player ? player.position.x : 0f,
            posY = player ? player.position.y : 0f,
            posZ = player ? player.position.z : 0f,
            lastSavedIso = DateTime.UtcNow.ToString("o"),
            totalPlaySeconds = total,
            previewImageBase64 = CapturePreviewBase64Safe()
        };

        if (player)
        {
            var consume = player.GetComponent<ConsumeControl>();
            if (consume != null)
            {
                var snap = consume.BuildSnapshot();
                data.morph = new MorphSave
                {
                    controllerName = snap.controllerName,
                    morphTag = snap.morphTag,
                    timeLeft = snap.timeLeft,
                    isMorphing = snap.isMorphing,
                    hasStored = snap.hasStored
                };
            }

            var lumerin = player.GetComponentInChildren<LumerinAbility>(true);
            if (lumerin != null)
                data.lumerin = new LumerinSave { currentBoost = lumerin.GetCurrentBoost() };

            var ramble = player.GetComponentInChildren<RambleAbility>(true);
            if (ramble != null)
                data.ramble = ramble.BuildSave();

            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                var cp = PlayerHealth.lastCheckpointPosition;
                data.health = new HealthSave
                {
                    hits = health.GetHits(),
                    checkpointX = cp.x,
                    checkpointY = cp.y,
                    checkpointZ = cp.z
                };
            }
        }

        DialogueSave composed = old?.dialogue ?? new DialogueSave();
        if (DialogueRuntime.Instance != null)
        {
            var fromRuntime = DialogueRuntime.Instance.BuildSave();
            composed.MergeFrom(fromRuntime);
        }
        data.dialogue = composed;

        data.bosses = new BossesSave();
        var bossesInScene = FindObjectsOfType<BossStationaryAI>(true);
        if (bossesInScene != null)
        {
            foreach (var b in bossesInScene)
            {
                if (b == null) continue;
                var entry = b.BuildSave();
                data.bosses.Upsert(entry);
            }
        }

        SaveDataSystem.Save(slot, data);
        sessionStartTime = Time.unscaledTime;
    }

    private string CapturePreviewBase64Safe()
    {
        try
        {
            var tex = CaptureDownscaled(previewWidth, previewHeight);
            if (!tex) return null;
            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }

    private Texture2D CaptureDownscaled(int w, int h)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        int sw = Screen.width;
        int sh = Screen.height;

        RenderTexture rt = new RenderTexture(sw, sh, 24);
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;

            Texture2D fullTex = new Texture2D(sw, sh, TextureFormat.RGB24, false);
            fullTex.ReadPixels(new Rect(0, 0, sw, sh), 0, 0);
            fullTex.Apply();

            if (sw == w && sh == h)
            {
                return fullTex;
            }

            Texture2D downTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    Color c = fullTex.GetPixelBilinear(u, v);
                    downTex.SetPixel(x, y, c);
                }
            }
            downTex.Apply();

            Destroy(fullTex);
            return downTex;
        }
        finally
        {
            cam.targetTexture = prevRT;
            RenderTexture.active = prevActive;
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
    }
}