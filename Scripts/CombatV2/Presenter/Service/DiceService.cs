using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceService
{
    private readonly System.Random random = new();

    public DiceResult Roll(int maxValue = 6)
    {
        int safeMaxValue = Math.Max(1, maxValue);
        int value = random.Next(1, safeMaxValue + 1);
        DiceTier tier = GetTier(value, safeMaxValue);

        Console.WriteLine($"[Dice] Rolled d{safeMaxValue}: {value} → {tier}");

        return new DiceResult(value, tier, safeMaxValue);
    }

    public DiceResult GetBestResult(List<DiceResult> rolls)
    {
        DiceResult best = null;
        for (int i = 0; i < rolls.Count; i++)
            if (best == null || rolls[i].Value > best.Value)
                best = rolls[i];

        return best;
    }

    public List<DiceResult> RollMany(int diceCount)
    {
        if (diceCount <= 0)
            return new List<DiceResult>();

        List<DiceResult> results = new(diceCount);

        for (int i = 0; i < diceCount; i++)
            results.Add(Roll());

        return results;
    }

    public List<DiceResult> RollMany(IReadOnlyList<int> diceMaxValues)
    {
        if (diceMaxValues == null || diceMaxValues.Count == 0)
            return new List<DiceResult>();

        List<DiceResult> results = new(diceMaxValues.Count);
        for (int i = 0; i < diceMaxValues.Count; i++)
            results.Add(Roll(diceMaxValues[i]));

        return results;
    }

    public List<int> ConvertToFaces(Battler battler, IReadOnlyList<DiceStatType> diceTypes)
    {
        List<int> diceFaces = new();
        if (diceTypes == null)
            return diceFaces;

        Dictionary<DiceStatType, int> diceCountByType = new();
        for (int i = 0; i < diceTypes.Count; i++)
        {
            DiceStatType type = diceTypes[i];
            diceCountByType[type] = diceCountByType.TryGetValue(type, out int count) ? count + 1 : 1;
        }

        foreach (KeyValuePair<DiceStatType, int> pair in diceCountByType)
        {
            int totalValue = GetDiceMaxValueForType(battler, pair.Key);
            int diceCount = Mathf.Max(0, pair.Value);
            if (totalValue <= 0 || diceCount <= 0)
                continue;
                
            int baseFace = Mathf.Max(1, totalValue / diceCount);
            int remainder = Mathf.Max(0, totalValue - (baseFace * diceCount));

            for (int i = 0; i < diceCount; i++)
            {
                int bonus = i < remainder ? 1 : 0;
                diceFaces.Add(baseFace + bonus);
                LogDiceStatBonus(pair.Key);
            }
        }

        return diceFaces;
    }

    public int GetDiceMaxValueForType(Battler battler, DiceStatType diceType)
    {
        if (battler == null)
            return 0;

        return diceType switch
        {
            DiceStatType.Mind => Mathf.Max(0, battler.Mind),
            DiceStatType.Heart => Mathf.Max(0, battler.Heart),
            DiceStatType.Body => Mathf.Max(0, battler.Body),
            _ => 0
        };
    }

    public void LogDiceStatBonus(DiceStatType diceType)
    {
        string bonusText = diceType switch
        {
            DiceStatType.Mind => "Mind DiceStatBonus placeholder",
            DiceStatType.Heart => "Heart DiceStatBonus placeholder",
            DiceStatType.Body => "Body DiceStatBonus placeholder",
            _ => "Unknown DiceStatBonus placeholder"
        };

        Debug.Log($"[Dice Bonus] {bonusText}");
    }

    private DiceTier GetTier(int value, int maxValue)
    {
        if (maxValue <= 1)
            return DiceTier.Low;

        float normalized = (float)value / maxValue;
        if (normalized <= 0.34f) return DiceTier.Low;
        if (normalized <= 0.67f) return DiceTier.Medium;
        return DiceTier.High;
    }
}
