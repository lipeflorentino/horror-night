using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollectableItem : MonoBehaviour
{
    [Header("Collectable Data")]
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private PlayerInventoryManager.ItemStatType statType = PlayerInventoryManager.ItemStatType.None;
    [SerializeField] private int amount = 1;

    [Header("Player Filter")]
    [SerializeField] private string playerTag = "player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryCollect(collision.gameObject);
    }

    private void TryCollect(GameObject other)
    {
        if (other == null || !other.CompareTag(playerTag))
        {
            return;
        }

        PlayerInventoryManager inventoryManager = other.GetComponentInParent<PlayerInventoryManager>();
        if (inventoryManager == null)
        {
            return;
        }

        bool collected = inventoryManager.AddItem(itemName, itemSprite, statType, amount);
        if (collected)
        {
            Destroy(gameObject);
        }
    }
}
