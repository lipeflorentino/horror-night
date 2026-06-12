using System;
using System.Collections.Generic;

[Serializable]
public class TrickInventorySnapshot
{
    public List<string> learnedTrickIds = new();
    public List<string> identityTrickIds = new();
    public List<CastedTrickSlotSnapshot> castedSlots = new();
    
    /// <summary>
    /// Snapshot persistido fora do combate. Tricks aprendidas e de identidade ficam no jogador,
    /// enquanto casted/cooldown são estado runtime do combate e não atravessam encontros.
    /// </summary>
    public static TrickInventorySnapshot CreatePersistentSnapshot(TrickInventorySnapshot source)
    {
        TrickInventorySnapshot snapshot = new();
        if (source == null)
            return snapshot;

        if (source.learnedTrickIds != null)
            snapshot.learnedTrickIds.AddRange(source.learnedTrickIds);

        if (source.identityTrickIds != null)
            snapshot.identityTrickIds.AddRange(source.identityTrickIds);

        return snapshot;
    }
}

[Serializable]
public struct CastedTrickSlotSnapshot
{
    public int slotIndex;
    public string trickId;
    public int remainingTurns;
    public int cooldownTurnsRemaining;
}
