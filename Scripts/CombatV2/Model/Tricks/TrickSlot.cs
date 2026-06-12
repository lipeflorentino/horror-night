using System;

/// <summary>
/// Runtime slot used by TrickInventory to represent identity and casted trick slots.
/// </summary>
[Serializable]
public class TrickSlot
{
    public TrickSlotType SlotType { get; private set; }
    public int SlotIndex { get; private set; }
    public TrickSO Definition { get; private set; }
    public TrickRuntimeInstance RuntimeInstance { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsEmpty => Definition == null && RuntimeInstance == null;

    public TrickSlot(TrickSlotType slotType, int slotIndex, bool isLocked = false)
    {
        SlotType = slotType;
        SlotIndex = Math.Max(0, slotIndex);
        IsLocked = isLocked;
    }

    public void BindDefinition(TrickSO definition)
    {
        if (IsLocked)
            return;

        Definition = definition;
        RuntimeInstance = null;
    }

    public void BindRuntimeInstance(TrickRuntimeInstance runtimeInstance)
    {
        if (IsLocked)
            return;

        RuntimeInstance = runtimeInstance;
        Definition = runtimeInstance?.Definition;
        RuntimeInstance?.BindSlot(SlotType, SlotIndex);
    }

    public void Clear()
    {
        if (IsLocked)
            return;

        Definition = null;
        RuntimeInstance = null;
    }

    public void SetLocked(bool isLocked)
    {
        IsLocked = isLocked;
    }
}
