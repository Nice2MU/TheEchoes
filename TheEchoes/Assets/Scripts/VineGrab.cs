using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class VineGrab : MonoBehaviour
{
    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private bool isOnVine = false;
    private bool canGrabVine = true;
    private Rigidbody2D vineRb;
    private Vector2 vineTopPosition;

    private float swingSpeed = 2f;
    private float swingAmount = 30f;
    private float vineGrabCooldown = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        joint.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (canGrabVine && other.CompareTag("Vine"))
        {
            vineRb = other.GetComponent<Rigidbody2D>();

            joint.connectedBody = vineRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.autoConfigureDistance = false;

            vineTopPosition = new Vector2(vineRb.position.x, vineRb.position.y + other.bounds.size.y / 2f);

            joint.connectedAnchor = vineRb.transform.InverseTransformPoint(vineTopPosition);
            joint.distance = 0f;

            joint.enabled = true;
            isOnVine = true;
        }
    }

    void FixedUpdate()
    {
        if (isOnVine)
        {
            float angle = Mathf.Sin(Time.time * swingSpeed) * swingAmount;

            Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.down * (vineRb.GetComponent<Collider2D>().bounds.size.y / 2f);
            Vector2 newVineTopPosition = (Vector2)vineRb.position + new Vector2(offset.x, offset.y);

            joint.connectedAnchor = vineRb.transform.InverseTransformPoint(newVineTopPosition);
        }
    }

    void Update()
    {
        if (isOnVine && Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseVine();
        }
    }

    void ReleaseVine()
    {
        joint.enabled = false;
        isOnVine = false;
        vineRb = null;

        StartCoroutine(VineGrabCooldownRoutine());

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, 5f), ForceMode2D.Impulse);
    }

    IEnumerator VineGrabCooldownRoutine()
    {
        canGrabVine = false;
        yield return new WaitForSeconds(vineGrabCooldown);
        canGrabVine = true;
    }
}