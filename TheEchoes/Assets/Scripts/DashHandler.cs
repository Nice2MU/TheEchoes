using UnityEngine;

public class DashHandler : MonoBehaviour
{
    private bool isDashing = false;

    public void HandleDash(bool shouldDash)
    {
        isDashing = shouldDash;
        if (isDashing)
        {
            PerformDash();
        }
    }

    private void PerformDash()
    {
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing && collision.gameObject.CompareTag("ObjectToDestroy"))
        {
            Destroy(collision.gameObject);
        }
    }
}