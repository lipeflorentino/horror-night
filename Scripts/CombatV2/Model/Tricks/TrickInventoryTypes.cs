using System;

public enum TrickInventoryAction
{
    Cast,
    Dischard,
    Close,
    ActivateCharge
}

public enum TrickInventoryLocation
{
    LearnedTricks,
    IdentitySlot,
    CastedActiveSlot,
    CastedPassiveSlot
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

    public readonly bool IsSlot => Location == TrickInventoryLocation.IdentitySlot || Location == TrickInventoryLocation.CastedActiveSlot || Location == TrickInventoryLocation.CastedPassiveSlot;
}
