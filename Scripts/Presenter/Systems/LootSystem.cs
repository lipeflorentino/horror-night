using UnityEngine;

public class LootSystem : MonoBehaviour
{
    [SerializeField] private ItemDatabase database;
    [SerializeField] private UILootPopup popup;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private LevelController levelController;
    [SerializeField] private PlayerGridMovement playerMovement;
    [SerializeField, Range(0f, 1f)] private float lootSpawnChance = 0.5f;

    private void Start()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();
        if (database == null)
            database = FindObjectOfType<ItemDatabase>();
        if (popup == null)
            popup = FindObjectOfType<UILootPopup>();

        playerMovement.OnMoveCompleted += HandlePlayerArrivedAtNode;
    }

    private void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnMoveCompleted -= HandlePlayerArrivedAtNode;
    }

    private void HandlePlayerArrivedAtNode(int index)
    {
        LevelNode node = levelController.nodes[index];

        if (!node.definition.flags.HasFlag(NodeFlags.CanSpawnLoot) || node.looted)
            return;

        node.looted = true;

        if (Random.value > lootSpawnChance)
            return;

        ItemSO item = database.GetRandomWeighted();

        if (item == null)
            return;

        playerMovement.enabled = false;

        popup.Show(item,
            onPick: () =>
            {
                inventory.AddItem(item);
                playerMovement.enabled = true;
            },
            onLeave: () =>
            {
                playerMovement.enabled = true;
            }
        );

        if (node.definition.flags.HasFlag(NodeFlags.OneTimeOnly))
        {
            node.definition.flags &= ~NodeFlags.CanSpawnLoot;
        }
    }
}
