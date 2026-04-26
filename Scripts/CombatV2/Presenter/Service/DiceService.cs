using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceService
{
    private readonly Random random = new();

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

    public List<int> ConvertToFaces(Battler battler, IReadOnlyList<StatDiceType> diceTypes)
    {
        List<int> diceFaces = new();
        if (diceTypes == null)
            return diceFaces;

        for (int i = 0; i < diceTypes.Count; i++)
        {
            int faceValue = GetDiceMaxValueForType(battler, diceTypes[i]);
            if (faceValue <= 0)
                continue;

            diceFaces.Add(faceValue);
            LogDiceStatBonus(diceTypes[i]);
        }

        return diceFaces;
    }

    public int GetDiceMaxValueForType(Battler battler, StatDiceType diceType)
    {
        if (battler == null)
            return 0;

        return diceType switch
        {
            StatDiceType.Mind => Mathf.Max(0, battler.Mind),
            StatDiceType.Heart => Mathf.Max(0, battler.Heart),
            StatDiceType.Body => Mathf.Max(0, battler.Body),
            _ => 0
        };
    }

    public void LogDiceStatBonus(StatDiceType diceType)
    {
        string bonusText = diceType switch
        {
            StatDiceType.Mind => "Mind DiceStatBonus placeholder",
            StatDiceType.Heart => "Heart DiceStatBonus placeholder",
            StatDiceType.Body => "Body DiceStatBonus placeholder",
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
