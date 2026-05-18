using System;

[Serializable]
public class EquippedItemInstance
{
    public ItemSO SourceItem { get; }
    public int RemainingDurability { get; private set; }

    public EquippedItemInstance(ItemSO item)
    {
        SourceItem = item;
        RemainingDurability = item != null ? item.durability : -1;
    }

    public bool ConsumeTurn()
    {
        if (RemainingDurability < 0)
            return false;

        RemainingDurability--;
        return RemainingDurability <= 0;
    }
}
