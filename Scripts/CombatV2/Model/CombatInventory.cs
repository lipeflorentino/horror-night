using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatInventory : ICombatInventory
{
    private readonly Battler battler;
    private readonly List<ItemSO> items = new();
    private readonly List<EquippedItemInstance> equippedWeapons = new();
    private readonly List<EquippedItemInstance> equippedRelics = new();
    private readonly int maxWeaponSlots;
    private readonly int maxRelicSlots;

    public CombatInventory(Battler battler, ItemDatabase itemDatabase, PlayerInventorySnapshot snapshot, int maxWeaponSlots = 2, int maxRelicSlots = 2)
    {
        this.battler = battler;
        this.maxWeaponSlots = Mathf.Max(1, maxWeaponSlots);
        this.maxRelicSlots = Mathf.Max(1, maxRelicSlots);
        RestoreSnapshot(itemDatabase, snapshot);
    }

    public IReadOnlyList<ItemSO> Items => items;
    public IReadOnlyList<EquippedItemInstance> GetEquippedWeapons() => equippedWeapons;
    public IReadOnlyList<EquippedItemInstance> GetEquippedRelics() => equippedRelics;

    public bool UseItem(ItemSO item)
    {
        if (item == null)
            return false;

        return item.type switch
        {
            ItemType.Consumable => ConsumeItem(item),
            ItemType.Weapon => EquipItem(item, equippedWeapons, maxWeaponSlots),
            ItemType.Relic => EquipItem(item, equippedRelics, maxRelicSlots),
            _ => false,
        };
    }

    public bool UnEquipItem(ItemSO item)
    {
        if (item == null)
            return false;

        return TryUnequipFromSlots(item, equippedWeapons) || TryUnequipFromSlots(item, equippedRelics);
    }

    public bool DeschardItem(ItemSO item)
    {
        if (item == null)
            return false;

        if (items.Remove(item))
            return true;

        return RemoveEquippedItem(item, equippedWeapons) || RemoveEquippedItem(item, equippedRelics);
    }

    public PlayerInventorySnapshot GetSnapshot()
    {
        Dictionary<int, int> countsByItemId = new();
        for (int i = 0; i < items.Count; i++)
        {
            ItemSO item = items[i];
            if (item == null)
                continue;

            countsByItemId[item.id] = countsByItemId.TryGetValue(item.id, out int current) ? current + 1 : 1;
        }

        PlayerInventorySnapshot snapshot = new()
        {
            itemIds = new List<int>(countsByItemId.Count),
            itemQuantities = new List<int>(countsByItemId.Count),
            equippedWeapons = CreateEquippedSnapshot(equippedWeapons),
            equippedRelics = CreateEquippedSnapshot(equippedRelics)
        };

        foreach (KeyValuePair<int, int> entry in countsByItemId)
        {
            snapshot.itemIds.Add(entry.Key);
            snapshot.itemQuantities.Add(entry.Value);
        }

        return snapshot;
    }

    private void RestoreSnapshot(ItemDatabase itemDatabase, PlayerInventorySnapshot snapshot)
    {
        items.Clear();
        equippedWeapons.Clear();
        equippedRelics.Clear();

        if (itemDatabase == null || itemDatabase.allItems == null || snapshot == null)
            return;

        if (snapshot.itemIds != null && snapshot.itemQuantities != null)
        {
            int count = Math.Min(snapshot.itemIds.Count, snapshot.itemQuantities.Count);
            for (int i = 0; i < count; i++)
            {
                ItemSO item = FindById(itemDatabase, snapshot.itemIds[i]);
                int quantity = Math.Max(0, snapshot.itemQuantities[i]);
                if (item == null || quantity <= 0)
                    continue;

                for (int j = 0; j < quantity; j++)
                    items.Add(item);
            }
        }

        RestoreEquippedSnapshot(itemDatabase, snapshot.equippedWeapons, equippedWeapons);
        RestoreEquippedSnapshot(itemDatabase, snapshot.equippedRelics, equippedRelics);
    }

    private static List<EquippedItemSnapshot> CreateEquippedSnapshot(List<EquippedItemInstance> slots)
    {
        List<EquippedItemSnapshot> result = new();
        for (int i = 0; i < slots.Count; i++)
        {
            EquippedItemInstance slot = slots[i];
            if (slot?.SourceItem == null)
                continue;

            result.Add(new EquippedItemSnapshot
            {
                itemId = slot.SourceItem.id,
                remainingDurability = slot.RemainingDurability
            });
        }

        return result;
    }

    private static void RestoreEquippedSnapshot(ItemDatabase itemDatabase, List<EquippedItemSnapshot> snapshots, List<EquippedItemInstance> destination)
    {
        if (snapshots == null)
            return;

        for (int i = 0; i < snapshots.Count; i++)
        {
            ItemSO item = FindById(itemDatabase, snapshots[i].itemId);
            if (item == null)
                continue;

            destination.Add(new EquippedItemInstance(item, snapshots[i].remainingDurability));
        }
    }

    private static ItemSO FindById(ItemDatabase itemDatabase, int itemId)
    {
        for (int i = 0; i < itemDatabase.allItems.Count; i++)
        {
            ItemSO item = itemDatabase.allItems[i];
            if (item != null && item.id == itemId)
                return item;
        }

        return null;
    }

    private bool ConsumeItem(ItemSO item)
    {
        if (!items.Remove(item))
            return false;

        ApplyStatBonus(item.statBonus);
        return true;
    }

    private bool EquipItem(ItemSO item, List<EquippedItemInstance> slots, int maxSlots)
    {
        if (slots.Count >= maxSlots || !items.Remove(item))
            return false;

        slots.Add(new EquippedItemInstance(item));
        ApplyStatBonus(item.statBonus);
        return true;
    }

    private bool TryUnequipFromSlots(ItemSO item, List<EquippedItemInstance> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i]?.SourceItem != item)
                continue;

            RemoveStatBonus(item.statBonus);
            slots.RemoveAt(i);
            items.Add(item);
            return true;
        }

        return false;
    }

    private bool RemoveEquippedItem(ItemSO item, List<EquippedItemInstance> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i]?.SourceItem != item)
                continue;

            RemoveStatBonus(item.statBonus);
            slots.RemoveAt(i);
            return true;
        }

        return false;
    }

    private void ApplyStatBonus(string statBonus)
    {
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
            ApplyDelta(stat.statName, stat.value);
    }

    private void RemoveStatBonus(string statBonus)
    {
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
            ApplyDelta(stat.statName, -stat.value);
    }

    private void ApplyDelta(string statName, int value)
    {
        if (battler == null || value == 0)
            return;

        switch (statName.ToLowerInvariant())
        {
            case "heart": battler.Heart = Math.Max(0, battler.Heart + value); break;
            case "body": battler.Body = Math.Max(0, battler.Body + value); break;
            case "mind": battler.Mind = Math.Max(0, battler.Mind + value); break;
            case "attack": battler.Attack = Math.Max(0, battler.Attack + value); break;
            case "defense": battler.Defense = Math.Max(0, battler.Defense + value); break;
            case "initiative": battler.Initiative = Math.Max(0, battler.Initiative + value); break;
        }
    }
}
