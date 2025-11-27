using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class WarpChargeZone : MonoBehaviour
{
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Warp Settings")]
    public Transform warpTarget;
    public float chargeDuration = 1.5f;
    public KeyCode chargeKey = KeyCode.F;

    [Header("UI")]
    public GameObject uiRoot;
    public TMP_Text labelText;

    private bool inside;
    private Transform player;
    private float chargeTimer;
    private bool isCharging;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (uiRoot != null)
            uiRoot.SetActive(false);

        if (labelText != null)
            labelText.text = $"Hold {chargeKey}";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            inside = true;
            player = other.transform;

            if (uiRoot != null)
                uiRoot.SetActive(true);

            ResetCharge();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            inside = false;
            player = null;

            if (uiRoot != null)
                uiRoot.SetActive(false);

            ResetCharge();
        }
    }

    private void Update()
    {
        if (!inside) return;

        if (Input.GetKey(chargeKey))
        {
            isCharging = true;
            chargeTimer += Time.deltaTime;

            if (chargeTimer >= chargeDuration)
            {
                DoWarp();
                ResetCharge();
            }
        }
        else
        {
            if (isCharging)
                ResetCharge();
        }
    }

    private void DoWarp()
    {
        if (player != null && warpTarget != null)
        {
            player.position = warpTarget.position;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeTimer = 0f;
    }
}