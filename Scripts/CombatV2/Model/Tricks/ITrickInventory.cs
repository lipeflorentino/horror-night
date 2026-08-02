using System.Collections.Generic;

public interface ITrickInventory
{
    event System.Action OnChanged;
    IReadOnlyList<TrickSlot> IdentitySlots { get; }
    IReadOnlyList<TrickSO> LearnedTricks { get; }
    IReadOnlyList<TrickSlot> ActiveCastedSlots { get; }
    IReadOnlyList<TrickSlot> PassiveCastedSlots { get; }

    bool LearnTrick(TrickSO trick);
    bool DischardTrick(TrickSO trick);
    bool CastTrick(TrickSO trick, out TrickRuntimeInstance instance);
    bool RemoveCastedTrick(TrickSlotType slotType, int slotIndex);
    void TickCooldowns();
    int GetRegisteredCooldown(string trickId);
    TrickInventorySnapshot GetSnapshot();
}
