using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;
    public List<ItemSO> items = new();

    public void AddItem(ItemSO item)
    {
        if (item == null)
            return;

        items.Add(item);
        Debug.Log("Item adicionado: " + item.itemName);
    }

    public void AddItem(ItemSO item, int quantity)
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

        if (itemDatabase == null)
            itemDatabase = FindObjectOfType<ItemDatabase>();

        if (itemDatabase == null || itemDatabase.allItems == null)
            return null;

        for (int i = 0; i < itemDatabase.allItems.Count; i++)
        {
            ItemSO item = itemDatabase.allItems[i];
            if (item != null && string.Equals(item.itemName, itemName, System.StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }

    public bool AddItem(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0)
            return false;

        ItemSO item = FindItemByName(itemName);
        if (item == null)
            return false;

        AddItem(item, quantity);
        return true;
    }

    public List<ItemSO> CreateSnapshot()
    {
        return new List<ItemSO>(items);
    }

    public PlayerInventorySnapshot GetSnapshot()
    {
        Dictionary<int, int> countsByItemId = new Dictionary<int, int>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemSO item = items[i];
            if (item == null)
                continue;

            if (!countsByItemId.ContainsKey(item.id))
                countsByItemId[item.id] = 0;

            countsByItemId[item.id]++;
        }

        PlayerInventorySnapshot snapshot = new PlayerInventorySnapshot
        {
            itemIds = new List<int>(countsByItemId.Count),
            itemQuantities = new List<int>(countsByItemId.Count)
        };

        foreach (KeyValuePair<int, int> entry in countsByItemId.OrderBy(entry => entry.Key))
        {
            snapshot.itemIds.Add(entry.Key);
            snapshot.itemQuantities.Add(entry.Value);
        }

        return snapshot;
    }

    public void RestoreSnapshot(List<ItemSO> snapshot)
    {
        items = snapshot != null ? new List<ItemSO>(snapshot) : new List<ItemSO>();
    }

    public void RestoreSnapshot(PlayerInventorySnapshot snapshot)
    {
        items.Clear();

        if (snapshot == null || snapshot.itemIds == null || snapshot.itemQuantities == null)
            return;

        if (itemDatabase == null)
            itemDatabase = FindObjectOfType<ItemDatabase>();

        if (itemDatabase == null || itemDatabase.allItems == null)
            return;

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
}
