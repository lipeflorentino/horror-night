using System;
using System.Collections.Generic;

[Serializable]
public class TrickInventorySnapshot
{
    public List<string> learnedTrickIds = new();
    public List<string> identityTrickIds = new();
    public List<CastedTrickSlotSnapshot> castedSlots = new();
}

[Serializable]
public struct CastedTrickSlotSnapshot
{
    public int slotIndex;
    public string trickId;
    public int remainingTurns;
    public int cooldownTurnsRemaining;
}
