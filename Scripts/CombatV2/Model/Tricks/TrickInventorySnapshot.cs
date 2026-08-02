using System;
using System.Collections.Generic;

[Serializable]
public class TrickInventorySnapshot
{
    public List<string> learnedTrickIds = new();
    public List<string> identityTrickIds = new();
    public List<CastedTrickSlotSnapshot> castedSlots = new();
    public List<TrickCooldownSnapshot> cooldowns = new();
    
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

    public void AddLearnedTrickId(string trickId)
    {
        if (this == null || string.IsNullOrWhiteSpace(trickId))
            return;

        learnedTrickIds ??= new List<string>();

        if (!learnedTrickIds.Contains(trickId))
            learnedTrickIds.Add(trickId);
    }
}

[Serializable]
public struct TrickCooldownSnapshot
{
    public string trickId;
    public int cooldownTurnsRemaining;
}

[Serializable]
public struct CastedTrickSlotSnapshot
{
    public TrickSlotType slotType;
    public int slotIndex;
    public string trickId;
    public int remainingTurns;
    public int cooldownTurnsRemaining;
}
