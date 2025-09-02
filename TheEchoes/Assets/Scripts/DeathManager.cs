using UnityEngine;

public class DeathManager : MonoBehaviour
{
    public float health = 100f;
    public static Vector3 lastCheckpointPosition;
    public GameObject[] objectsToReset;

    private ConsumeControl consumeControl;

    void Update() //ใช้ในช่วงเทส ปุ่มรี
    {
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            health = 0f;
            Respawn();
        }
    }

    void Start()
    {
        lastCheckpointPosition = transform.position;

        consumeControl = GetComponent<ConsumeControl>();

        if (consumeControl == null)
        {

        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Respawn"))
        {
            health = 0f;
            Respawn();
        }
    }

    void Respawn()
    {
        transform.position = lastCheckpointPosition;
        health = 100f;

        foreach (GameObject obj in objectsToReset)
        {
            ObjectReset objectResetScript = obj.GetComponent<ObjectReset>();

            if (objectResetScript != null)
            {
                objectResetScript.ResetState();
            }
        }

        if (consumeControl != null)
        {
            consumeControl.RestoreConsumedObjects();
            consumeControl.RevertToOriginalForm();
            consumeControl.SetHasStoredMorph(false);

            if (consumeControl.pawerBar != null)
            {
                consumeControl.pawerBar.value = 0f;
            }

            PlayerPrefs.DeleteKey("LastMorphTag");
            PlayerPrefs.DeleteKey("LastMorphTimeLeft");
            PlayerPrefs.Save();
        }
    }
}