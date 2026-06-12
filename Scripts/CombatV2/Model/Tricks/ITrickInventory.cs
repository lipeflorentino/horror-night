using System.Collections.Generic;

public interface ITrickInventory
{
    event System.Action OnChanged;
    IReadOnlyList<TrickSlot> IdentitySlots { get; }
    IReadOnlyList<TrickSO> LearnedTricks { get; }
    IReadOnlyList<TrickSlot> CastedSlots { get; }

    bool LearnTrick(TrickSO trick);
    bool DischardTrick(TrickSO trick);
    bool CastTrick(TrickSO trick, out TrickRuntimeInstance instance);
    bool RemoveCastedTrick(int slotIndex);
    TrickInventorySnapshot GetSnapshot();
}
