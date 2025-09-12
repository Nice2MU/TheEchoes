using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckPoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public bool useOverridePosition = false;
    public Vector3 overridePosition;

    public AudioSource sfx;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 cp = useOverridePosition ? overridePosition : transform.position;
        PlayerHealth.lastCheckpointPosition = cp;

        if (sfx != null)
        {
            sfx.Play();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 pos = useOverridePosition ? overridePosition : transform.position;
        Gizmos.DrawSphere(pos, 0.3f);
    }
}