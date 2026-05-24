using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(PlayerStatusManager))]
public class PlayerInventory : MonoBehaviour, ICombatInventory
{
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private PlayerStatusManager playerStatusManager;
    [SerializeField] private int maxWeaponSlots = 2;
    [SerializeField] private int maxRelicSlots = 2;
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
        
        Logger.Log($"[PlayerInventory] Criando snapshot de inventário... Itens atuais: {items.Count}");

        for (int i = 0; i < items.Count; i++)
        {
            ItemSO item = items[i];
            if (item == null)
                continue;

            if (!countsByItemId.ContainsKey(item.id))
                countsByItemId[item.id] = 0;

            countsByItemId[item.id]++;
        }

        PlayerInventorySnapshot snapshot = new()
        {
            itemIds = new List<int>(countsByItemId.Count),
            itemQuantities = new List<int>(countsByItemId.Count),
            equippedWeapons = CreateEquippedSnapshot(equippedWeapons),
            equippedRelics = CreateEquippedSnapshot(equippedRelics)
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
        Logger.Log($"[Inventory] Tentando usar item: {item.itemName}");
        if (item == null || playerStatusManager == null)
            return false;

        switch (item.type)
        {
            case ItemType.Consumable:
                if (!items.Remove(item))
                    return false;
                ApplyStatBonus(item.statBonus);
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
        Logger.Log($"[Inventory] Tentando desequipar item: {item.itemName}");
        if (item == null)
            return false;

        if (TryUnequipFromSlots(item, equippedWeapons))
            return true;

        if (TryUnequipFromSlots(item, equippedRelics))
            return true;

        return false;
    }

    public bool DeschardItem(ItemSO item)
    {
        Logger.Log($"[Inventory] Tentando descartar item: {item.itemName}");
        if (item == null)
            return false;

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

        EquippedItemInstance instance = new(item);
        slots.Add(instance);
        ApplyStatBonus(item.statBonus);

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

            RemoveStatBonus(equippedItem.SourceItem.statBonus);
            ItemSO brokenVersion = ScriptableObject.CreateInstance<ItemSO>();
            brokenVersion.id = equippedItem.SourceItem.id;
            brokenVersion.itemName = $"{equippedItem.SourceItem.itemName} (Broken)";
            brokenVersion.description = equippedItem.SourceItem.description;
            brokenVersion.rarity = equippedItem.SourceItem.rarity;
            brokenVersion.icon = equippedItem.SourceItem.icon;
            brokenVersion.type = ItemType.Broken;
            brokenVersion.weight = equippedItem.SourceItem.weight;
            slots.RemoveAt(i);
            items.Add(brokenVersion);
        }
    }

    private void ApplyStatBonus(string statBonus)
    {
        Logger.Log($"[Inventory] Aplicando stat bonus: {statBonus}");
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
            playerStatusManager.ApplyStatDelta(stat.statName, stat.value);
    }

    private void RemoveStatBonus(string statBonus)
    {
        Logger.Log($"[Inventory] Removendo stat bonus: {statBonus}");
        foreach (ItemStatBonus stat in ItemStatBonusParser.Parse(statBonus))
            playerStatusManager.ApplyStatDelta(stat.statName, -stat.value);
    }

    private bool TryUnequipFromSlots(ItemSO item, List<EquippedItemInstance> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].SourceItem != item)
                continue;

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
            if (slots[i].SourceItem != item)
                continue;

            RemoveStatBonus(item.statBonus);
            slots.RemoveAt(i);
            return true;
        }

        return false;
    }
}
