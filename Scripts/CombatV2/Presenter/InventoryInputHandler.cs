using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private PlayerInventory playerInventory;

    public void Init(PlayerInventory inventory)
    {
        playerInventory = inventory;
        if (inventoryView != null)
        {
            inventoryView.BindInventory(playerInventory);
            inventoryView.OnInteractWithItem += HandleItemInteraction;
        }
    }

    private void OnDestroy()
    {
        if (inventoryView == null)
            return;

        inventoryView.OnInteractWithItem -= HandleItemInteraction;
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
                OnDiscardItem(item);
                break;
        }
    }

    public void OnUseItem(ItemSO item)
    {
        if (playerInventory == null || item == null)
            return;

        bool used = playerInventory.UseItem(item);
        if (inventoryView != null)
        {
            inventoryView.SetStatus(used ? $"Usou {item.itemName}" : $"Falha ao usar {item.itemName}");
            inventoryView.Refresh();
        }
    }

    public void OnDiscardItem(ItemSO item)
    {
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
