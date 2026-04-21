using System;
using System.Collections.Generic;

public class DiceService
{
    private readonly Random random = new();

    public DiceResult Roll()
    {
        int value = random.Next(1, 7);
        DiceTier tier = GetTier(value);

        Console.WriteLine($"[Dice] Rolled: {value} → {tier}");

        return new DiceResult(value, tier);
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
        int safeDiceCount = Math.Max(1, diceCount);
        List<DiceResult> results = new(safeDiceCount);

        for (int i = 0; i < safeDiceCount; i++)
            results.Add(Roll());

        return results;
    }

    private DiceTier GetTier(int value)
    {
        if (value <= 2) return DiceTier.Low;
        if (value <= 4) return DiceTier.Medium;
        return DiceTier.High;
    }
}