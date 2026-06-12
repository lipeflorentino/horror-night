using System;

public enum TrickInventoryAction
{
    Cast,
    Dischard,
    Close
}

public enum TrickInventoryLocation
{
    LearnedTricks,
    IdentitySlot,
    CastedSlot
}

[Serializable]
public struct TrickInventoryItemLocation
{
    public TrickInventoryLocation Location;
    public int SlotIndex;

    public TrickInventoryItemLocation(TrickInventoryLocation location, int slotIndex = -1)
    {
        Location = location;
        SlotIndex = slotIndex;
    }

    public bool IsSlot => Location == TrickInventoryLocation.IdentitySlot || Location == TrickInventoryLocation.CastedSlot;
}
