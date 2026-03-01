using UnityEngine;

public class LootSystem : MonoBehaviour
{
    [SerializeField] private ItemDatabase database;
    [SerializeField] private UILootPopup popup;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerGridMovement playerMovement;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();
        if (database == null)
            database = FindObjectOfType<ItemDatabase>();
        if (popup == null)
            popup = FindObjectOfType<UILootPopup>();
    }

    public void TriggerLoot(LevelNode node)
    {
        if (node == null || node.looted)
            return;

        if (!node.definition.flags.HasFlag(NodeFlags.CanSpawnLoot))
            return;

        ItemSO item = database != null ? database.GetRandomWeighted() : null;

        if (item == null)
            return;

        node.looted = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (popup == null)
        {
            inventory?.AddItem(item);
            if (playerMovement != null)
                playerMovement.enabled = true;
            return;
        }

        popup.Show(item,
            onPick: () =>
            {
                inventory?.AddItem(item);
                if (playerMovement != null)
                    playerMovement.enabled = true;
            },
            onLeave: () =>
            {
                if (playerMovement != null)
                    playerMovement.enabled = true;
            }
        );

        if (node.definition.flags.HasFlag(NodeFlags.OneTimeOnly))
            node.definition.flags &= ~NodeFlags.CanSpawnLoot;
    }
}
