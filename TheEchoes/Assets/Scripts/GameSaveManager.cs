using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    public Transform player;

    public float saveCooldownSeconds = 0.75f;

    [Header("Preview Capture")]
    public int previewWidth = 130;
    public int previewHeight = 80;

    double sessionPlaySeconds = 0;
    float sessionStartTime;
    float lastSaveTime = -999f;

    private void OnEnable() { sessionStartTime = Time.unscaledTime; }
    private void OnDisable() { sessionPlaySeconds += (Time.unscaledTime - sessionStartTime); }

    private void Start() { ApplyFromCurrentSlot(true); }

    public bool ApplyFromCurrentSlot(bool teleportPlayer)
    {
        int slot = GlobalGame.CurrentSlot;
        var data = SaveDataSystem.Load(slot);
        if (data == null) return false;

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
            if (lumerin != null && data.lumerin != null) lumerin.SetCurrentBoost(data.lumerin.currentBoost);

            var ramble = player.GetComponentInChildren<RambleAbility>(true);
            if (ramble != null && data.ramble != null) ramble.ApplySave(data.ramble);

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
            if (lumerin != null) data.lumerin = new LumerinSave { currentBoost = lumerin.GetCurrentBoost() };

            var ramble = player.GetComponentInChildren<RambleAbility>(true);
            if (ramble != null) data.ramble = ramble.BuildSave();

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
        catch { return null; }
    }

    private Texture2D CaptureDownscaled(int w, int h)
    {
        Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
        if (!full) return null;

        var small = new Texture2D(w, h, TextureFormat.RGB24, false);
        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            int srcY = Mathf.RoundToInt(v * (full.height - 1));
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                int srcX = Mathf.RoundToInt(u * (full.width - 1));
                small.SetPixel(x, y, full.GetPixel(srcX, srcY));
            }
        }
        small.Apply();
        Destroy(full);
        return small;
    }
}