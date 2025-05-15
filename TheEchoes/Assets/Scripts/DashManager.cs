using UnityEngine;
using System.Collections;

public class DashManager : MonoBehaviour
{
    private bool isDashing = false;

    public void Dash(bool shouldDash)
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
        if (isDashing && collision.gameObject.CompareTag("Wall"))
        {
            Animator wallAnimator = collision.gameObject.GetComponent<Animator>();
            Collider2D wallCollider = collision.gameObject.GetComponent<Collider2D>();

            if (wallAnimator != null)
            {
                if (wallCollider != null)
                    wallCollider.enabled = false;

                wallAnimator.SetTrigger("Wall");
                SoundManager.instance.PlaySFX("WallDestroy");

                StartCoroutine(DestroyAfterAnimation(collision.gameObject, wallAnimator));
            }

            else
            {
                Destroy(collision.gameObject);
            }
        }
    }

    private IEnumerator DestroyAfterAnimation(GameObject target, Animator animator)
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        Destroy(target);
    }
}