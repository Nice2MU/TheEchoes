using UnityEngine;

public class ObjectReset : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody2D rb;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rb = GetComponent<Rigidbody2D>();
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void ResetState()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        ResetPosition();
    }
}