using UnityEngine;

public class WallRockTrigger : MonoBehaviour
{
    public RockFallAndDisappear[] rocks;

    public float delayBeforeFall = 0f;

    bool triggered = false;

    void Awake()
    {
        if (rocks == null || rocks.Length == 0)
            rocks = GetComponentsInChildren<RockFallAndDisappear>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger();
    }

    void TryTrigger()
    {
        if (triggered) return;

        RambleAbility ramble = FindObjectOfType<RambleAbility>();
        if (ramble == null)
        {
            return;
        }

        if (!ramble.IsDashing())
        {
            return;
        }

        triggered = true;

        if (delayBeforeFall > 0f)
            Invoke(nameof(DoFall), delayBeforeFall);
        else
            DoFall();
    }

    void DoFall()
    {
        foreach (var r in rocks)
        {
            if (r != null)
                r.MakeFall();
        }
    }
}