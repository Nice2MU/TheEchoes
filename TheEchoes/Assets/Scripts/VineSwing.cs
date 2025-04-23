using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VineSwing : MonoBehaviour
{
    public float swingSpeed = 2f;
    public float swingAmount = 30f;

    private Rigidbody2D rb;
    private float startTime;
    private Vector3 topPivotPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startTime = Time.time;
        topPivotPosition = transform.position + new Vector3(0, transform.localScale.y / 2f, 0);
    }

    void FixedUpdate()
    {
        float angle = Mathf.Sin((Time.time - startTime) * swingSpeed) * swingAmount;

        RotateAroundTopPivot(angle);
    }

    void RotateAroundTopPivot(float angle)
    {
        Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.down * (transform.localScale.y / 2f);
        rb.MovePosition(topPivotPosition + offset);
        rb.MoveRotation(angle);
    }
}