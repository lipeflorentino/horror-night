using System;

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

    public DiceResult RollBestOf(int diceCount)
    {
        DiceResult best = null;

        for (int i = 0; i < diceCount; i++)
        {
            var roll = Roll();

            if (best == null || roll.Value > best.Value)
            {
                best = roll;
            }
        }

        return best;
    }

    private DiceTier GetTier(int value)
    {
        if (value <= 2) return DiceTier.Low;
        if (value <= 4) return DiceTier.Medium;
        return DiceTier.High;
    }
}