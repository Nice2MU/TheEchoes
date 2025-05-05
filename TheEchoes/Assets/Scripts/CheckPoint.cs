using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Vector3 respawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDeathHandler.lastCheckpointPosition = respawnPosition;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(respawnPosition, 0.3f);
    }
}