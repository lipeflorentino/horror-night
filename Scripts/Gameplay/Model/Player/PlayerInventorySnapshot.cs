using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInventorySnapshot
{
    public List<int> itemIds;
    public List<int> itemQuantities;
    public List<EquippedItemSnapshot> equippedWeapons;
    public List<EquippedItemSnapshot> equippedRelics;
}

[Serializable]
public struct EquippedItemSnapshot
{
    public int itemId;
    public int remainingDurability;
}
