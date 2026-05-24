using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private MonoBehaviour playerInventorySource;
    private ICombatInventory playerInventory;
    [SerializeField] private CombatManager Combat;

    public void Init(CombatManager cm, ICombatInventory inventory)
    {
        Combat = cm;
        inventoryView = FindObjectOfType<InventoryView>();
        Logger.Log($"[InventoryInputHandler] Inicializando com inventário do jogador: {inventory}");
        playerInventory = inventory;
        playerInventorySource = inventory as MonoBehaviour;
        if (inventoryView != null)
        {
            inventoryView.BindInventory(playerInventory);
            inventoryView.OnInteractWithInventoryItem += HandleItemInteraction;
            Logger.Log($"[InventoryInputHandler] Subscribed to inventoryView.OnInteractWithInventoryItem");
        }
    }

    private void OnDestroy()
    {
        if (inventoryView == null)
            return;

        inventoryView.OnInteractWithInventoryItem -= HandleItemInteraction;
    }

    private void HandleItemInteraction(ItemSO item, InventoryItemAction action, InventoryItemLocation location)
    {
        switch (action)
        {
            case InventoryItemAction.Use:
                OnUseItem(item);
                break;
            case InventoryItemAction.Equip:
                OnEquipItem(item);
                break;
            case InventoryItemAction.Unequip:
                OnUnequipItem(item);
                break;
            case InventoryItemAction.Discard:
                OnDischardItem(item);
                break;
        }

        Combat.RefreshCombatUI();
    }

    public void OnUseItem(ItemSO item)
    {
        Logger.Log($"[InventoryInputHandler] Tentando usar item: {item.itemName}");
        if (playerInventory == null || item == null)
            return;

        bool used = playerInventory.UseItem(item);
        if (inventoryView != null)
        {
            inventoryView.SetStatus(used ? $"Usou {item.itemName}" : $"Falha ao usar {item.itemName}");
            inventoryView.Refresh();
        }
    }

    public void OnDischardItem(ItemSO item)
    {
        Logger.Log($"[InventoryInputHandler] Tentando descartar item: {item.itemName}");
        if (playerInventory == null || item == null)
            return;

        bool discarded = playerInventory.DeschardItem(item);
        if (inventoryView != null)
        {
            inventoryView.SetStatus(discarded ? $"Descartou {item.itemName}" : $"Falha ao descartar {item.itemName}");
            inventoryView.Refresh();
        }
    }

    public void OnEquipItem(ItemSO item)
    {
        if (playerInventory == null || item == null)
            return;

        bool equipped = playerInventory.UseItem(item);
        if (inventoryView != null)
        {
            inventoryView.SetStatus(equipped ? $"Equipou {item.itemName}" : $"Falha ao equipar {item.itemName}");
            inventoryView.Refresh();
        }
    }

    public void OnUnequipItem(ItemSO item)
    {
        Logger.Log($"[InventoryInputHandler] Tentando desequipar item: {item.itemName}");
        if (playerInventory == null || item == null)
            return;

        bool unequipped = playerInventory.UnEquipItem(item);
        if (inventoryView != null)
        {
            inventoryView.SetStatus(unequipped ? $"Desequipou {item.itemName}" : $"Falha ao desequipar {item.itemName}");
            inventoryView.Refresh();
        }
    }
}
