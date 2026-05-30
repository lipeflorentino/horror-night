using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RewardService
{
    public Dictionary<ItemSO, int> GetRandomLoot(int enemyLevel)
    {
        Dictionary<ItemSO, int> loot = new();

        ItemSO[] allItems = Resources.LoadAll<ItemSO>("Data/Items");
        if (allItems == null || allItems.Length == 0)
            return loot;

        ItemSO goldCoins = allItems.FirstOrDefault(item => item != null && string.Equals(item.itemName, "Moedas de Ouro", StringComparison.OrdinalIgnoreCase));
        if (goldCoins != null)
            loot[goldCoins] = Mathf.Max(1, GrantGoldCoinsReward(enemyLevel));

        List<ItemSO> candidateItems = allItems
            .Where(item => item != null && item.weight > 0 && item != goldCoins)
            .ToList();

        if (candidateItems.Count == 0)
            return loot;

        int normalizedLevel = Mathf.Max(1, enemyLevel);
        int extraDrops = UnityEngine.Random.Range(0, 2 + normalizedLevel / 8);

        for (int i = 0; i < extraDrops; i++)
        {
            ItemSO rolledItem = RollWeightedItem(candidateItems, normalizedLevel);
            if (rolledItem == null)
                continue;

            int quantity = RollQuantityByRarity(rolledItem.rarity, normalizedLevel);
            if (quantity <= 0)
                continue;

            if (loot.ContainsKey(rolledItem))
                loot[rolledItem] += quantity;
            else
                loot[rolledItem] = quantity;
        }

        return loot;
    }

    public int GrantXpRewardIfEligible(int enemyLevel, int playerLevel)
    {
        if (playerLevel < enemyLevel)
            return 0;

        int reward = Mathf.Max(0, enemyLevel);
        return reward;
    }    

    public int GrantGoldCoinsReward(int level)
    {
        int minGoldCoins = 1*level;
        int maxGoldCoins = Mathf.Max(minGoldCoins, level * 10);
        return UnityEngine.Random.Range(minGoldCoins, maxGoldCoins + 1);
    }

    private ItemSO RollWeightedItem(List<ItemSO> candidateItems, int enemyLevel)
    {
        int bonusWeightPool = Mathf.Clamp(enemyLevel / 4, 0, 40);
        int totalWeight = 0;
        int[] adjustedWeights = new int[candidateItems.Count];

        for (int i = 0; i < candidateItems.Count; i++)
        {
            ItemSO item = candidateItems[i];
            int rarityBias = item.rarity switch
            {
                Rarity.Common => Mathf.Max(0, 10 - enemyLevel / 2),
                Rarity.Uncommon => Mathf.Max(0, 7 - enemyLevel / 4),
                Rarity.Rare => Mathf.Clamp(enemyLevel / 3, 0, bonusWeightPool),
                Rarity.Epic => Mathf.Clamp(enemyLevel / 2, 0, bonusWeightPool),
                Rarity.Legendary => Mathf.Clamp(enemyLevel, 0, bonusWeightPool),
                _ => 0
            };

            int adjusted = Mathf.Max(1, item.weight + rarityBias);
            adjustedWeights[i] = adjusted;
            totalWeight += adjusted;
        }

        if (totalWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < candidateItems.Count; i++)
        {
            cumulative += adjustedWeights[i];
            if (roll < cumulative)
                return candidateItems[i];
        }

        return candidateItems[^1];
    }

    private int RollQuantityByRarity(Rarity rarity, int enemyLevel)
    {
        int levelBonus = Mathf.Clamp(enemyLevel / 10, 0, 6);

        return rarity switch
        {
            Rarity.Common => UnityEngine.Random.Range(1, 3 + levelBonus),
            Rarity.Uncommon => UnityEngine.Random.Range(1, 2 + levelBonus),
            Rarity.Rare => UnityEngine.Random.Range(1, 2 + Mathf.Max(1, levelBonus / 2)),
            Rarity.Epic => UnityEngine.Random.Range(1, 2 + Mathf.Max(1, levelBonus / 3)),
            Rarity.Legendary => UnityEngine.Random.Range(1, 2 + Mathf.Max(1, levelBonus / 4)),
            _ => 1
        };
    }
}