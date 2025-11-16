using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuestPickup : MonoBehaviour
{
    [Header("Quest Item Id")]
    public string itemId = "apple";
    public int amount = 1;

    [Header("After Pick")]
    public bool hideAfterPick = true;
    public GameObject effectOnPick;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        QuestSystem.AddItem(itemId, Mathf.Max(1, amount));

        if (effectOnPick) Instantiate(effectOnPick, transform.position, Quaternion.identity);

        if (hideAfterPick)
        {
            gameObject.SetActive(false);
            DialogueRuntime.Instance?.RecordObjectActive(gameObject, false);
        }
    }
}