using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
    [SerializeField] private InventoryView inventoryView;

    public void OnUseItem(ItemSO item)
    {
        if (inventoryView != null)
            inventoryView.UseItem(item);
    }

    public void OnDiscardItem(ItemSO item)
    {
        if (inventoryView != null)
            inventoryView.DeschardItem(item);
    }

    public void OnEquipItem(ItemSO item)
    {
        if (inventoryView != null)
            inventoryView.EquipItem(item);
    }

    public void OnUnequipItem(ItemSO item)
    {
        if (inventoryView != null)
            inventoryView.UnEquipItem(item);
    }
}
