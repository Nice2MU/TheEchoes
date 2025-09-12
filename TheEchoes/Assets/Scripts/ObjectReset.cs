using UnityEngine;

[DisallowMultipleComponent]
public class ObjectReset : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private Rigidbody2D rb2d;
    private Rigidbody rb3d;

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;

        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();
    }

    public void ResetState()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        if (rb2d)
        {
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }

        if (rb3d)
        {
            rb3d.velocity = Vector3.zero;
            rb3d.angularVelocity = Vector3.zero;
        }
    }
}