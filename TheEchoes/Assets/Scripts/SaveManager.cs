using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    [Header("Slot Widgets (1-4)")]
    public SlotWidgets slot1, slot2, slot3, slot4;

    [Header("UI Control")]
    public GameObject mainMenuUI;
    public GameObject gameUI;

    [Header("Delete Mode")]
    public Button deleteModeButton;
    public TextMeshProUGUI deleteModeHint;

    [Header("Initial Save Defaults")]
    public Vector3 startPosition = Vector3.zero;

    private bool deleteMode = false;

    [Serializable]
    public class SlotWidgets
    {
        [Range(1, 4)] public int slotIndex = 1;
        public Button slotButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;
        public RawImage previewImage;
    }

    private void Awake()
    {
        if (deleteModeButton)
        {
            deleteModeButton.onClick.RemoveAllListeners();
            deleteModeButton.onClick.AddListener(ToggleDeleteMode);
        }
    }

    private void OnEnable()
    {
        WireSlot(slot1); WireSlot(slot2); WireSlot(slot3); WireSlot(slot4);
        RefreshAll();
        UpdateDeleteHint();
    }

    private void WireSlot(SlotWidgets w)
    {
        if (w?.slotButton == null) return;
        w.slotButton.onClick.RemoveAllListeners();
        w.slotButton.onClick.AddListener(() => OnClickSlot(w.slotIndex));
    }

    public void RefreshAll()
    {
        Refresh(slot1); Refresh(slot2); Refresh(slot3); Refresh(slot4);
    }

    private void Refresh(SlotWidgets w)
    {
        if (w == null) return;

        if (!SaveDataSystem.Has(w.slotIndex))
        {
            if (w.titleText) w.titleText.text = $"Slot {w.slotIndex} — Empty";
            if (w.subtitleText) w.subtitleText.text = deleteMode ? "Tap to delete (empty)" : "Start a new game";
            if (w.previewImage) w.previewImage.texture = null;
            return;
        }

        var d = SaveDataSystem.Load(w.slotIndex);
        if (d == null)
        {
            if (w.titleText) w.titleText.text = $"Slot {w.slotIndex} — (invalid)";
            if (w.subtitleText) w.subtitleText.text = "Corrupted save";
            if (w.previewImage) w.previewImage.texture = null;
            return;
        }

        if (w.titleText) w.titleText.text = $"Slot {w.slotIndex} — {d.scene}";

        if (w.subtitleText)
        {
            string savedLocal = "-";
            try { savedLocal = DateTime.Parse(d.lastSavedIso).ToLocalTime().ToString("yyyy-MM-dd HH:mm"); } catch { }
            var t = TimeSpan.FromSeconds(Math.Max(0, d.totalPlaySeconds));
            var info = $"Saved {savedLocal} • Playtime {t:hh\\:mm\\:ss}";
            w.subtitleText.text = deleteMode ? $"{info}  (tap to delete)" : info;
        }

        if (w.previewImage)
        {
            if (!string.IsNullOrEmpty(d.previewImageBase64))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(d.previewImageBase64);
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    w.previewImage.texture = tex;
                }
                catch { w.previewImage.texture = null; }
            }

            else w.previewImage.texture = null;
        }
    }

    private void OnClickSlot(int slot)
    {
        if (deleteMode)
        {
            SaveDataSystem.Delete(slot);
            if (GlobalGame.CurrentSlot == slot) GlobalGame.CurrentSlot = 1;
            ToggleDeleteMode();
            RefreshAll();
            return;
        }

        GlobalGame.CurrentSlot = slot;

        if (!SaveDataSystem.Has(slot))
        {
            var data = new SaveData
            {
                scene = SceneManager.GetActiveScene().name,
                posX = startPosition.x,
                posY = startPosition.y,
                posZ = startPosition.z,
                lastSavedIso = DateTime.UtcNow.ToString("o"),
                totalPlaySeconds = 0,
                previewImageBase64 = null,
                health = new HealthSave
                {
                    hits = 5,
                    checkpointX = startPosition.x,
                    checkpointY = startPosition.y,
                    checkpointZ = startPosition.z
                }
            };

            SaveDataSystem.Save(slot, data);
            PlayerHealth.lastCheckpointPosition = startPosition;
            RefreshAll();
        }

        var gsm = FindObjectOfType<GameSaveManager>();
        if (gsm != null) gsm.ApplyFromCurrentSlot(true);

        if (mainMenuUI) mainMenuUI.SetActive(false);
        if (gameUI) gameUI.SetActive(true);
    }

    public void ToggleDeleteMode()
    {
        deleteMode = !deleteMode;
        UpdateDeleteHint();
        RefreshAll();
    }

    private void UpdateDeleteHint()
    {
        if (deleteModeHint) deleteModeHint.text = deleteMode ? "Delete Mode: Tap a slot to delete" : "";
        if (deleteModeButton)
        {
            var txt = deleteModeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = deleteMode ? "Exit Delete" : "Delete";
        }
    }
}