using System.Collections;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallDelay = 1f;
    [SerializeField] private float destroyDelay = 7f;

    private bool falling = false;

    [SerializeField] private Rigidbody2D rb;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (falling)
            return;

        if (collision.transform.CompareTag("Player"))
        {
            StartCoroutine(StartFall());
        }
    }

    private IEnumerator StartFall()
    {
        falling = true;

        yield return new WaitForSeconds(fallDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;

        yield return new WaitForSeconds(destroyDelay);

        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        falling = false;
        StopAllCoroutines();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}