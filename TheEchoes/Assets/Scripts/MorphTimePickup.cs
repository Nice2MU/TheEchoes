using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(StableId))]
public class MorphTimePickup : MonoBehaviour
{
    [Header("Time Bonus")]
    public float addSeconds = 10f;
    public bool allowOvercap = false;

    [Header("Respawn")]
    public float respawnSeconds = 30f;

    [Header("SFX / VFX (Optional)")]
    public string pickupSfx = "PickupMorphTime";
    public GameObject pickupVfxPrefab;

    private bool _available = true;
    private Renderer[] _renderers;
    private Collider2D[] _colliders;
    private StableId _stableId;
    private Coroutine _respawnRoutine;

    private void Awake()
    {
        _stableId = GetComponent<StableId>();
        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        _colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        GameSaveManager.OnSlotApplied += RefreshFromSave;
    }

    private void OnDisable()
    {
        GameSaveManager.OnSlotApplied -= RefreshFromSave;
    }

    private void Start()
    {
        RefreshFromSave();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_available) return;

        var consume = other.GetComponentInParent<ConsumeControl>() ?? other.GetComponent<ConsumeControl>();
        if (consume == null) return;

        bool added = consume.AddMorphTime(addSeconds, allowOvercap, playSfx: false);
        if (!added) return;

        if (!string.IsNullOrEmpty(pickupSfx))
            SoundManager.instance?.PlaySFX(pickupSfx);
        if (pickupVfxPrefab)
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);

        var slot = GlobalGame.CurrentSlot;
        var data = SaveDataSystem.Load(slot) ?? new SaveData();
        var until = DateTime.UtcNow.AddSeconds(respawnSeconds);
        var id = _stableId.Id;

        data.dialogue.SetObjectState(id, false);
        data.dialogue.SetPickupCooldown(id, until);
        SaveDataSystem.Save(slot, data);

        BeginRespawnCountdown(until);
    }

    private void BeginRespawnCountdown(DateTime untilUtc)
    {
        SetAvailable(false);
        if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
        _respawnRoutine = StartCoroutine(RespawnWait(untilUtc));
    }

    private IEnumerator RespawnWait(DateTime untilUtc)
    {
        double seconds = Math.Max(0, (untilUtc - DateTime.UtcNow).TotalSeconds);
        if (seconds > 0)
            yield return new WaitForSecondsRealtime((float)seconds);

        var slot = GlobalGame.CurrentSlot;
        var data = SaveDataSystem.Load(slot) ?? new SaveData();
        var id = _stableId.Id;

        data.dialogue.SetObjectState(id, true);
        data.dialogue.ClearPickupCooldown(id);
        SaveDataSystem.Save(slot, data);

        SetAvailable(true);
        _respawnRoutine = null;
    }

    private void RefreshFromSave()
    {
        var slot = GlobalGame.CurrentSlot;
        var data = SaveDataSystem.Load(slot);
        if (data == null)
        {
            SetAvailable(true);
            return;
        }

        var id = _stableId.Id;
        bool active = true;

        if (data.dialogue.TryGetObjectState(id, out var activeSaved))
            active = activeSaved;

        if (data.dialogue.TryGetPickupCooldown(id, out var until))
        {
            if (DateTime.UtcNow >= until)
            {
                data.dialogue.SetObjectState(id, true);
                data.dialogue.ClearPickupCooldown(id);
                SaveDataSystem.Save(slot, data);
                active = true;
            }
            else
            {
                if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
                _respawnRoutine = StartCoroutine(RespawnWait(until));
                active = false;
            }
        }

        SetAvailable(active);
    }

    private void SetAvailable(bool value)
    {
        _available = value;
        foreach (var r in _renderers)
            if (r) r.enabled = value;
        foreach (var c in _colliders)
            if (c) c.enabled = value;
    }
}