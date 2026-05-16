using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private RewardItemView itemPrefab;

    private readonly List<RewardItemView> spawnedItems = new();

    public void Show(Dictionary<ItemSO, int> itemRewards)
    {
        Clear();

        if (gridParent == null || itemPrefab == null || itemRewards == null)
            return;

        foreach (KeyValuePair<ItemSO, int> reward in itemRewards)
        {
            if (reward.Key == null || reward.Value <= 0)
                continue;

            RewardItemView itemView = Instantiate(itemPrefab, gridParent);
            itemView.Bind(reward.Key.icon, reward.Key.itemName, reward.Value);
            spawnedItems.Add(itemView);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }
}