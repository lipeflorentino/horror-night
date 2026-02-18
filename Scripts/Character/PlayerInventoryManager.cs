using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    public enum ItemStatType
    {
        None,
        Life,
        Strength,
        Sanity
    }

    [Serializable]
    public class InventoryItem
    {
        public string itemName;
        public Sprite sprite;
        public ItemStatType statType;
        public int quantity;
    }

    [Serializable]
    public class InventorySlot
    {
        public bool IsEmpty => item == null || item.quantity <= 0;
        public InventoryItem item;
    }

    [Header("Inventory Capacity")]
    [SerializeField] private int initialSlotCount = 8;
    [SerializeField] private int maxItemsPerSlot = 10;

    [Header("Debug View (Read Only)")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public int SlotCount => slots.Count;
    public int MaxItemsPerSlot => maxItemsPerSlot;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        initialSlotCount = Mathf.Max(1, initialSlotCount);
        maxItemsPerSlot = Mathf.Max(1, maxItemsPerSlot);
        EnsureSlotCount(initialSlotCount);
    }

    public bool AddItem(string itemName, Sprite sprite, ItemStatType statType, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || !CanStack(slot.item, itemName, statType))
            {
                continue;
            }

            int freeSpace = maxItemsPerSlot - slot.item.quantity;
            if (freeSpace <= 0)
            {
                continue;
            }

            int toAdd = Mathf.Min(freeSpace, remaining);
            slot.item.quantity += toAdd;
            remaining -= toAdd;
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty)
            {
                continue;
            }

            int toAdd = Mathf.Min(maxItemsPerSlot, remaining);
            slot.item = new InventoryItem
            {
                itemName = itemName,
                sprite = sprite,
                statType = statType,
                quantity = toAdd
            };

            remaining -= toAdd;
        }

        bool addedAny = remaining < amount;
        if (addedAny)
        {
            OnInventoryChanged?.Invoke();
        }

        return remaining == 0;
    }

    public bool RemoveItem(string itemName, ItemStatType statType, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || !CanStack(slot.item, itemName, statType))
            {
                continue;
            }

            int toRemove = Mathf.Min(slot.item.quantity, remaining);
            slot.item.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.item.quantity <= 0)
            {
                slot.item = null;
            }
        }

        bool removedAny = remaining < amount;
        if (removedAny)
        {
            OnInventoryChanged?.Invoke();
        }

        return remaining == 0;
    }

    public int GetTotalAmount(string itemName, ItemStatType statType)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || !CanStack(slot.item, itemName, statType))
            {
                continue;
            }

            total += slot.item.quantity;
        }

        return total;
    }

    public void ExpandSlots(int additionalSlots)
    {
        if (additionalSlots <= 0)
        {
            return;
        }

        EnsureSlotCount(slots.Count + additionalSlots);
        OnInventoryChanged?.Invoke();
    }

    public void IncreaseMaxItemsPerSlot(int additionalCapacity)
    {
        if (additionalCapacity <= 0)
        {
            return;
        }

        maxItemsPerSlot += additionalCapacity;
        OnInventoryChanged?.Invoke();
    }

    public IReadOnlyList<InventorySlot> GetSlots()
    {
        return slots;
    }

    private void EnsureSlotCount(int targetCount)
    {
        while (slots.Count < targetCount)
        {
            slots.Add(new InventorySlot());
        }
    }

    private bool CanStack(InventoryItem item, string itemName, ItemStatType statType)
    {
        return item != null
            && string.Equals(item.itemName, itemName, StringComparison.OrdinalIgnoreCase)
            && item.statType == statType;
    }
}
