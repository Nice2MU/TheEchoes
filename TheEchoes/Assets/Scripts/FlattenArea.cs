using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlattenArea : MonoBehaviour
{
    public bool requireHoldToFlatten = true;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        gameObject.name = "FlattenArea (Trigger)";
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var c = Gizmos.color;
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.25f);
        var col = GetComponent<Collider2D>();
        if (col is BoxCollider2D b)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.offset, b.size);
        }
        else if (col is CircleCollider2D cc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(cc.offset, cc.radius);
        }
        Gizmos.color = c;
    }
#endif
}