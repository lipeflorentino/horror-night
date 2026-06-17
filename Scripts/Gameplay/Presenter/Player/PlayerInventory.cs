using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(PlayerStatusManager))]
public class PlayerInventory : MonoBehaviour, ICombatInventory
{
    private const int CoreStatCap = 20;

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private PlayerStatusManager playerStatusManager;
    [SerializeField] private int maxWeaponSlots = 2;
    [SerializeField] private int maxRelicSlots = 2;
    [SerializeField] private int baseHeart = -1;
    [SerializeField] private int baseBody = -1;
    [SerializeField] private int baseMind = -1;
    [SerializeField] private bool hasCoreStatBase;
    public List<ItemSO> items = new();
    public IReadOnlyList<ItemSO> Items => items;
    [SerializeField] private List<EquippedItemInstance> equippedWeapons = new();
    [SerializeField] private List<EquippedItemInstance> equippedRelics = new();

    private void Awake()
    {
        playerStatusManager = GetComponent<PlayerStatusManager>();
        itemDatabase = FindObjectOfType<ItemDatabase>();
    }

    public void AddItem(ItemSO item)
    {
        if (item == null)
            return;

        items.Add(item);
        Debug.Log("Item adicionado: " + item.itemName);
    }

    public void AddManyItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
            return;

        for (int i = 0; i < quantity; i++)
            AddItem(item);
    }

    public ItemSO FindItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        if (itemDatabase == null || itemDatabase.allItems == null)
            return null;

        for (int i = 0; i < itemDatabase.allItems.Count; i++)
        {
            ItemSO item = itemDatabase.allItems[i];
            if (item != null && string.Equals(item.itemName, itemName, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }

    public bool AddInventoryItem(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0)
            return false;

        ItemSO item = FindItemByName(itemName);
        if (item == null)
            return false;

        AddManyItem(item, quantity);
        return true;
    }

    public List<ItemSO> CreateSnapshot()
    {
        return new List<ItemSO>(items);
    }

    public PlayerInventorySnapshot GetSnapshot()
    {
        Dictionary<int, int> countsByItemId = new();

        for (int i = 0; i < items.Count; i++)
        {
            ItemSO item = items[i];
            if (item == null)
                continue;

            if (!countsByItemId.ContainsKey(item.id))
                countsByItemId[item.id] = 0;

            countsByItemId[item.id]++;
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
            baseHeart = snapshotHasCoreStatBase ? baseHeart : playerStatusManager != null ? playerStatusManager.GetStatValue("heart") : 0,
            baseBody = snapshotHasCoreStatBase ? baseBody : playerStatusManager != null ? playerStatusManager.GetStatValue("body") : 0,
            baseMind = snapshotHasCoreStatBase ? baseMind : playerStatusManager != null ? playerStatusManager.GetStatValue("mind") : 0
        };

        foreach (KeyValuePair<int, int> entry in countsByItemId.OrderBy(entry => entry.Key))
        {
            snapshot.itemIds.Add(entry.Key);
            snapshot.itemQuantities.Add(entry.Value);
        }

        return snapshot;
    }

    public void RestoreSnapshot(PlayerInventorySnapshot snapshot)
    {
        items.Clear();
        equippedWeapons.Clear();
        equippedRelics.Clear();
        hasCoreStatBase = false;
        baseHeart = -1;
        baseBody = -1;
        baseMind = -1;

        if (snapshot == null)
            return;

        if (itemDatabase == null || itemDatabase.allItems == null)
            return;

        if (snapshot.itemIds != null && snapshot.itemQuantities != null)
        {
            int entryCount = Mathf.Min(snapshot.itemIds.Count, snapshot.itemQuantities.Count);

            for (int i = 0; i < entryCount; i++)
            {
                int itemId = snapshot.itemIds[i];
                int quantity = Mathf.Max(0, snapshot.itemQuantities[i]);
                if (quantity == 0)
                    continue;

                ItemSO item = FindItemById(itemId);
                if (item == null)
                    continue;

                for (int j = 0; j < quantity; j++)
                    items.Add(item);
            }
        }

        RestoreEquippedSnapshot(snapshot.equippedWeapons, equippedWeapons);
        RestoreEquippedSnapshot(snapshot.equippedRelics, equippedRelics);
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

    private void RestoreEquippedSnapshot(List<EquippedItemSnapshot> snapshots, List<EquippedItemInstance> destination)
    {
        if (snapshots == null)
            return;

        for (int i = 0; i < snapshots.Count; i++)
        {
            ItemSO item = FindItemById(snapshots[i].itemId);
            if (item == null)
                continue;

            destination.Add(new EquippedItemInstance(item, snapshots[i].remainingDurability));
        }
    }

    private ItemSO FindItemById(int itemId)
    {
        for (int i = 0; i < itemDatabase.allItems.Count; i++)
        {
            ItemSO item = itemDatabase.allItems[i];
            if (item != null && item.id == itemId)
                return item;
        }

        return null;
    }

    public IReadOnlyList<EquippedItemInstance> GetEquippedWeapons() => equippedWeapons;
    public IReadOnlyList<EquippedItemInstance> GetEquippedRelics() => equippedRelics;

    public bool UseItem(ItemSO item)
    {
        if (item == null || playerStatusManager == null)
            return false;

        Logger.Log($"[Inventory] Tentando usar item: {item.itemName}");

        switch (item.type)
        {
            case ItemType.Consumable:
                if (!items.Remove(item))
                    return false;
                ApplyConsumableStatBonus(item.statBonus);
                return true;
            case ItemType.Weapon:
                return EquipItem(item, equippedWeapons, maxWeaponSlots);
            case ItemType.Relic:
                return EquipItem(item, equippedRelics, maxRelicSlots);
            default:
                return false;
        }
    }

    public bool UnEquipItem(ItemSO item)
    {
        if (item == null)
            return false;

        Logger.Log($"[Inventory] Tentando desequipar item: {item.itemName}");

        if (TryUnequipFromSlots(item, equippedWeapons))
            return true;

        if (TryUnequipFromSlots(item, equippedRelics))
            return true;

        return false;
    }

    public bool DeschardItem(ItemSO item)
    {
        if (item == null)
            return false;

        Logger.Log($"[Inventory] Tentando descartar item: {item.itemName}");

        if (items.Remove(item))
            return true;

        return RemoveEquippedItem(item, equippedWeapons) || RemoveEquippedItem(item, equippedRelics);
    }

    public void TickEquippedItems()
    {
        TickSlotCollection(equippedWeapons);
        TickSlotCollection(equippedRelics);
    }

    private bool EquipItem(ItemSO item, List<EquippedItemInstance> slots, int maxSlots)
    {
        Logger.Log($"[Inventory] Tentando equipar item: {item.itemName} em slots do tipo {(slots == equippedWeapons ? "Weapon" : "Relic")}");
        if (slots.Count >= maxSlots || !items.Remove(item))
            return false;

        if (HasEquippedItems())
            EnsureCoreStatBaseInitialized();
        else
            CaptureCurrentCoreStatsAsBase();

        EquippedItemInstance instance = new(item);
        slots.Add(instance);
        ApplyNonCoreStatBonus(item.statBonus, 1);
        RecalculateCoreStatsFromEquipment();

        if (!string.IsNullOrWhiteSpace(item.specialEffect) && !string.Equals(item.specialEffect, "none", StringComparison.OrdinalIgnoreCase))
            Debug.Log($"[Inventory] Efeito especial ativo: {item.specialEffect} ({item.itemName})");

        return true;
    }

    private void TickSlotCollection(List<EquippedItemInstance> slots)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            EquippedItemInstance equippedItem = slots[i];
            if (!equippedItem.ConsumeTurn())
                continue;

            ApplyNonCoreStatBonus(equippedItem.SourceItem.statBonus, -1);
            ItemSO brokenVersion = ScriptableObject.CreateInstance<ItemSO>();
            brokenVersion.id = equippedItem.SourceItem.id;
            brokenVersion.itemName = $"{equippedItem.SourceItem.itemName} (Broken)";
            brokenVersion.description = equippedItem.SourceItem.description;
            brokenVersion.rarity = equippedItem.SourceItem.rarity;
            brokenVersion.icon = equippedItem.SourceItem.icon;
            brokenVersion.type = ItemType.Broken;
            brokenVersion.weight = equippedItem.SourceItem.weight;
            slots.RemoveAt(i);
            RecalculateCoreStatsFromEquipment();
            items.Add(brokenVersion);
        }
    }

    private void ApplyConsumableStatBonus(string statBonus)
    {
        Logger.Log($"[Inventory] Aplicando stat bonus consumível: {statBonus}");
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
                    playerStatusManager.ApplyStatDelta(stat.statName, stat.value);
                    CaptureCurrentCoreStatsAsBase();
                }
                continue;
            }

            playerStatusManager.ApplyStatDelta(stat.statName, stat.value);
        }
    }

    private void ApplyNonCoreStatBonus(string statBonus, int multiplier)
    {
        Logger.Log($"[Inventory] Aplicando stat bonus de equipamento: {statBonus} x{multiplier}");
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
        {
            if (IsCoreStat(NormalizeStatName(stat.statName)))
                continue;

            playerStatusManager.ApplyStatDelta(stat.statName, stat.value * multiplier);
        }
    }

    private bool TryUnequipFromSlots(ItemSO item, List<EquippedItemInstance> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].SourceItem != item)
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
            if (slots[i].SourceItem != item)
                continue;

            ApplyNonCoreStatBonus(item.statBonus, -1);
            slots.RemoveAt(i);
            RecalculateCoreStatsFromEquipment();
            return true;
        }

        return false;
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
        if (hasCoreStatBase || playerStatusManager == null)
            return;

        baseHeart = ClampCoreStat(playerStatusManager.GetStatValue("heart") - GetTotalEquippedCoreBonus("heart"));
        baseBody = ClampCoreStat(playerStatusManager.GetStatValue("body") - GetTotalEquippedCoreBonus("body"));
        baseMind = ClampCoreStat(playerStatusManager.GetStatValue("mind") - GetTotalEquippedCoreBonus("mind"));
        hasCoreStatBase = true;
    }

    private void CaptureCurrentCoreStatsAsBase()
    {
        if (playerStatusManager == null)
            return;

        baseHeart = ClampCoreStat(playerStatusManager.GetStatValue("heart"));
        baseBody = ClampCoreStat(playerStatusManager.GetStatValue("body"));
        baseMind = ClampCoreStat(playerStatusManager.GetStatValue("mind"));
        hasCoreStatBase = true;
    }

    private bool HasEquippedItems()
    {
        return equippedWeapons.Count > 0 || equippedRelics.Count > 0;
    }

    private void RecalculateCoreStatsFromEquipment()
    {
        EnsureCoreStatBaseInitialized();
        ApplyCoreStatTarget("heart", baseHeart + GetTotalEquippedCoreBonus("heart"));
        ApplyCoreStatTarget("body", baseBody + GetTotalEquippedCoreBonus("body"));
        ApplyCoreStatTarget("mind", baseMind + GetTotalEquippedCoreBonus("mind"));
    }

    private void ApplyCoreStatTarget(string statName, int targetValue)
    {
        int clampedTarget = ClampCoreStat(targetValue);
        int currentValue = playerStatusManager.GetStatValue(statName);
        int delta = clampedTarget - currentValue;
        if (delta != 0)
            playerStatusManager.ApplyStatDelta(statName, delta);
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

    private static bool IsCoreStat(string statName)
    {
        return statName == "heart" || statName == "body" || statName == "mind";
    }

    private static int ClampCoreStat(int value)
    {
        return Mathf.Clamp(value, 0, CoreStatCap);
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
