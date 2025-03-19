using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // เมื่อผู้เล่นชนกับ Checkpoint, จะบันทึกตำแหน่ง Checkpoint
            PlayerControl.lastCheckpointPosition = transform.position;
        }
    }
}
