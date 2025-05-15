using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VineSwing : MonoBehaviour
{
    public float swingSpeed = 2f;
    public float swingAmount = 30f;
    public float maxDistance = 12f;

    private Rigidbody2D rb;
    private float startTime;
    private Vector3 topPivotPosition;

    private Transform playerTransform;
    private bool vineSoundPlaying = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startTime = Time.time;
        topPivotPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (SoundManager.instance != null && SoundManager.instance.distanceSource != null && SoundManager.instance.vine != null)
        {
            SoundManager.instance.distanceSource.clip = SoundManager.instance.vine;
            SoundManager.instance.distanceSource.loop = true;
            SoundManager.instance.distanceSource.Play();
            vineSoundPlaying = true;
        }
    }

    void FixedUpdate()
    {
        float angle = Mathf.Sin((Time.time - startTime) * swingSpeed) * swingAmount;
        RotateAroundTopPivot(angle);
        AdjustVolumeBasedOnDistance();
    }

    void RotateAroundTopPivot(float angle)
    {
        Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.down * (transform.localScale.y / 2f);
        rb.MovePosition(topPivotPosition + offset);
        rb.MoveRotation(angle);
    }

    void AdjustVolumeBasedOnDistance()
    {
        if (playerTransform == null || SoundManager.instance == null || SoundManager.instance.distanceSource == null || !vineSoundPlaying)
            return;

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance > maxDistance)
        {
            SoundManager.instance.distanceSource.volume = 0f;
        }

        else
        {
            float volume = 1f - (distance / maxDistance);
            SoundManager.instance.distanceSource.volume = Mathf.Clamp01(volume);
        }
    }

    void OnDisable()
    {
        if (vineSoundPlaying && SoundManager.instance != null && SoundManager.instance.distanceSource != null)
        {
            SoundManager.instance.distanceSource.Stop();
            vineSoundPlaying = false;
        }
    }
}