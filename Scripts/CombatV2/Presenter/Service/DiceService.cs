using System;

public class DiceService
{
    private Random _random = new Random();

    public DiceResult Roll()
    {
        int value = _random.Next(1, 7);
        DiceTier tier = GetTier(value);

        Console.WriteLine($"[Dice] Rolled: {value} → {tier}");

        return new DiceResult(value, tier);
    }

    private DiceTier GetTier(int value)
    {
        if (value <= 2) return DiceTier.Low;
        if (value <= 4) return DiceTier.Medium;
        return DiceTier.High;
    }
}