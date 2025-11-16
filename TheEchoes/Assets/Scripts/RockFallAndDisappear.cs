using UnityEngine;

public class RockFallAndDisappear : MonoBehaviour
{
    [Header("Falling")]
    public bool addImpulse = true;
    public Vector2 impulse = new Vector2(0.3f, 0.8f);

    [Header("Cleanup")]
    public float destroyAfter = 3f;

    Rigidbody2D rb;
    Collider2D col;
    bool hasFallen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void MakeFall()
    {
        if (hasFallen) return;
        hasFallen = true;

        rb.bodyType = RigidbodyType2D.Dynamic;

        if (addImpulse)
            rb.AddForce(impulse, ForceMode2D.Impulse);

        Destroy(gameObject, destroyAfter);
    }
}