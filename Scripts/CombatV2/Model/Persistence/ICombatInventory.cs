using System.Collections.Generic;

public interface ICombatInventory
{
    IReadOnlyList<ItemSO> Items { get; }
    IReadOnlyList<EquippedItemInstance> GetEquippedWeapons();
    IReadOnlyList<EquippedItemInstance> GetEquippedRelics();
    bool UseItem(ItemSO item);
    bool UnEquipItem(ItemSO item);
    bool DeschardItem(ItemSO item);
    PlayerInventorySnapshot GetSnapshot();
}
