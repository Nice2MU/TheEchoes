using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class HiddenManager : MonoBehaviour
{
    public bool oneShot = false;
    private bool alreadyEntered = false;
    private bool alreadyExited = false;

    public string collisionTag;
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;

    public SpriteRenderer spriteRenderer;
    public float fadeDuration = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (alreadyEntered)
            return;

        if (!string.IsNullOrEmpty(collisionTag) && !collision.CompareTag(collisionTag))
            return;

        Fade(true);
        onTriggerEnter?.Invoke();

        if (oneShot)
            alreadyEntered = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (alreadyExited)
            return;

        if (!string.IsNullOrEmpty(collisionTag) && !collision.CompareTag(collisionTag))
            return;

        Fade(false);
        onTriggerExit?.Invoke();

        if (oneShot)
            alreadyExited = true;
    }

    private void Fade(bool fadeIn)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float targetAlpha = fadeIn ? 1f : 0f;
        spriteRenderer.DOFade(targetAlpha, fadeDuration);
    }
}