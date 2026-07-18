using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatInventory : ICombatInventory
{
    private const int CoreStatCap = 20;
    private readonly Battler battler;
    private readonly List<ItemSO> items = new();
    private readonly List<EquippedItemInstance> equippedWeapons = new();
    private readonly List<EquippedItemInstance> equippedRelics = new();
    private readonly int maxWeaponSlots;
    private readonly int maxRelicSlots;
    private int baseHeart = -1;
    private int baseBody = -1;
    private int baseMind = -1;
    private bool hasCoreStatBase;

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

        bool snapshotHasCoreStatBase = HasEquippedItems();
        if (snapshotHasCoreStatBase)
            EnsureCoreStatBaseInitialized();

        PlayerInventorySnapshot snapshot = new()
        {
            itemIds = new List<int>(countsByItemId.Count),
            itemQuantities = new List<int>(countsByItemId.Count),
            equippedWeapons = CreateEquippedSnapshot(equippedWeapons),
            equippedRelics = CreateEquippedSnapshot(equippedRelics),
            hasCoreStatBase = snapshotHasCoreStatBase && hasCoreStatBase,
            baseHeart = snapshotHasCoreStatBase ? baseHeart : battler != null ? battler.Heart : 0,
            baseBody = snapshotHasCoreStatBase ? baseBody : battler != null ? battler.Body : 0,
            baseMind = snapshotHasCoreStatBase ? baseMind : battler != null ? battler.Mind : 0
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
        hasCoreStatBase = false;
        baseHeart = -1;
        baseBody = -1;
        baseMind = -1;

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
        RestoreCoreStatBase(snapshot);
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

        ApplyConsumableStatBonus(item.statBonus);
        return true;
    }

    private bool EquipItem(ItemSO item, List<EquippedItemInstance> slots, int maxSlots)
    {
        if (slots.Count >= maxSlots || !items.Remove(item))
            return false;

        if (HasEquippedItems())
            EnsureCoreStatBaseInitialized();
        else
            CaptureCurrentCoreStatsAsBase();

        slots.Add(new EquippedItemInstance(item));
        ApplyNonCoreStatBonus(item.statBonus, 1);
        RecalculateCoreStatsFromEquipment();
        return true;
    }

    private bool TryUnequipFromSlots(ItemSO item, List<EquippedItemInstance> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i]?.SourceItem != item)
                continue;

            ApplyNonCoreStatBonus(item.statBonus, -1);
            slots.RemoveAt(i);
            RecalculateCoreStatsFromEquipment();
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

            ApplyNonCoreStatBonus(item.statBonus, -1);
            slots.RemoveAt(i);
            RecalculateCoreStatsFromEquipment();
            return true;
        }

        return false;
    }

    private void ApplyConsumableStatBonus(string statBonus)
    {
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
        {
            string statName = NormalizeStatName(stat.statName);
            if (IsCoreStat(statName))
            {
                if (HasEquippedItems())
                {
                    EnsureCoreStatBaseInitialized();
                    SetCoreStatBase(statName, GetCoreStatBase(statName) + stat.value);
                    RecalculateCoreStatsFromEquipment();
                }
                else
                {
                    ApplyCoreDelta(statName, stat.value);
                    CaptureCurrentCoreStatsAsBase();
                }
                continue;
            }

            ApplyDelta(statName, stat.value);
        }
    }

    private void ApplyNonCoreStatBonus(string statBonus, int multiplier)
    {
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
        {
            string statName = NormalizeStatName(stat.statName);
            if (IsCoreStat(statName))
                continue;

            ApplyDelta(statName, stat.value * multiplier);
        }
    }

    private void RestoreCoreStatBase(PlayerInventorySnapshot snapshot)
    {
        if (snapshot.hasCoreStatBase)
        {
            baseHeart = ClampCoreStat(snapshot.baseHeart);
            baseBody = ClampCoreStat(snapshot.baseBody);
            baseMind = ClampCoreStat(snapshot.baseMind);
            hasCoreStatBase = true;
            return;
        }

        EnsureCoreStatBaseInitialized();
    }

    private void EnsureCoreStatBaseInitialized()
    {
        if (hasCoreStatBase || battler == null)
            return;

        baseHeart = ClampCoreStat(battler.Heart - GetTotalEquippedCoreBonus("heart"));
        baseBody = ClampCoreStat(battler.Body - GetTotalEquippedCoreBonus("body"));
        baseMind = ClampCoreStat(battler.Mind - GetTotalEquippedCoreBonus("mind"));
        hasCoreStatBase = true;
    }

    private void CaptureCurrentCoreStatsAsBase()
    {
        if (battler == null)
            return;

        baseHeart = ClampCoreStat(battler.Heart);
        baseBody = ClampCoreStat(battler.Body);
        baseMind = ClampCoreStat(battler.Mind);
        hasCoreStatBase = true;
    }

    private bool HasEquippedItems()
    {
        return equippedWeapons.Count > 0 || equippedRelics.Count > 0;
    }

    private void RecalculateCoreStatsFromEquipment()
    {
        EnsureCoreStatBaseInitialized();
        if (battler == null)
            return;

        battler.Heart = ClampCoreStat(baseHeart + GetTotalEquippedCoreBonus("heart"));
        battler.Body = ClampCoreStat(baseBody + GetTotalEquippedCoreBonus("body"));
        battler.Mind = ClampCoreStat(baseMind + GetTotalEquippedCoreBonus("mind"));
    }

    private int GetTotalEquippedCoreBonus(string statName)
    {
        return GetTotalEquippedCoreBonus(statName, equippedWeapons) + GetTotalEquippedCoreBonus(statName, equippedRelics);
    }

    private static int GetTotalEquippedCoreBonus(string statName, List<EquippedItemInstance> slots)
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSO item = slots[i]?.SourceItem;
            if (item == null)
                continue;

            foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(item.statBonus))
            {
                if (NormalizeStatName(stat.statName) == statName)
                    total += stat.value;
            }
        }

        return total;
    }

    private int GetCoreStatBase(string statName)
    {
        return statName switch
        {
            "heart" => baseHeart,
            "body" => baseBody,
            "mind" => baseMind,
            _ => 0,
        };
    }

    private void SetCoreStatBase(string statName, int value)
    {
        int clampedValue = ClampCoreStat(value);
        switch (statName)
        {
            case "heart":
                baseHeart = clampedValue;
                break;
            case "body":
                baseBody = clampedValue;
                break;
            case "mind":
                baseMind = clampedValue;
                break;
        }
    }

    private void ApplyCoreDelta(string statName, int value)
    {
        if (battler == null || value == 0)
            return;

        switch (statName)
        {
            case "heart": battler.Heart = ClampCoreStat(battler.Heart + value); break;
            case "body": battler.Body = ClampCoreStat(battler.Body + value); break;
            case "mind": battler.Mind = ClampCoreStat(battler.Mind + value); break;
        }
    }

    private void ApplyDelta(string statName, int value)
    {
        if (battler == null || value == 0)
            return;

        switch (NormalizeStatName(statName))
        {
            case "attack": battler.Attack = Math.Max(0, battler.Attack + value); break;
            case "defense": battler.Defense = Math.Max(0, battler.Defense + value); break;
            case "initiative": battler.Initiative = Math.Max(0, battler.Initiative + value); break;
            case "focus": battler.Focus = Math.Max(0, battler.Focus + value); break;
            case "strength": battler.Strength = Math.Max(0, battler.Strength + value); break;
            case "agility": battler.Agility = Math.Max(0, battler.Agility + value); break;
        }
    }

    private static bool IsCoreStat(string statName)
    {
        return statName == "heart" || statName == "body" || statName == "mind";
    }

    private static int ClampCoreStat(int value)
    {
        return Math.Min(CoreStatCap, Math.Max(0, value));
    }

    private static string NormalizeStatName(string statName)
    {
        string normalized = string.IsNullOrWhiteSpace(statName)
            ? string.Empty
            : statName.Trim().ToLowerInvariant();

        return normalized switch
        {
            "life" => "heart",
            "vida" => "heart",
            "coracao" => "heart",
            "coração" => "heart",
            "physical" => "body",
            "fisico" => "body",
            "físico" => "body",
            "corpo" => "body",
            "strength" => "strength",
            "força" => "strength",
            "forca" => "strength",
            "power" => "strength",
            "mental" => "mind",
            "sanity" => "mind",
            "sanidade" => "mind",
            "mente" => "mind",
            "iniciativa" => "initiative",
            "speed" => "initiative",
            "focus" => "focus",
            "foco" => "focus",
            "agility" => "agility",
            "agilidade" => "agility",
            _ => normalized,
        };
    }
}
