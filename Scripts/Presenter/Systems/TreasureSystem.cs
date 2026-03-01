using UnityEngine;

public class TreasureSystem : MonoBehaviour
{
    [SerializeField] private ItemDatabase database;
    [SerializeField] private PlayerInventory inventory;

    private void Awake()
    {
        if (database == null)
            database = FindObjectOfType<ItemDatabase>();
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
    }

    public void TriggerTreasure(LevelNode node)
    {
        if (node == null || node.looted)
            return;

        node.looted = true;

        ItemSO reward = database != null ? database.GetRandomWeighted() : null;

        if (reward != null)
            inventory?.AddItem(reward);

        Debug.Log($"[TreasureSystem] Treasure chest resolved on node {node.index}. Reward: {(reward != null ? reward.itemName : "None (mock)")}");
    }
}
