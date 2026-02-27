using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public List<ItemSO> allItems = new List<ItemSO>();

    private void Awake()
    {
        LoadAllItems();
    }

    void LoadAllItems()
    {
        ItemSO[] loaded = Resources.LoadAll<ItemSO>("Data/Items");

        allItems.Clear();
        allItems.AddRange(loaded);

        Debug.Log($"Itens carregados: {allItems.Count}");
    }

    public ItemSO GetRandomWeighted()
    {
        int totalWeight = 0;

        foreach (var item in allItems)
            totalWeight += item.weight;

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var item in allItems)
        {
            cumulative += item.weight;

            if (roll < cumulative)
                return item;
        }

        return null;
    }
}