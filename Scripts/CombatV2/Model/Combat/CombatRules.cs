using UnityEngine;

/// <summary>
/// Fonte Única de Verdade (SSOT) para os cálculos matemáticos do combate.
/// Compartilhado tanto pelo Preview de Alocação quanto pela Resolução Real.
/// </summary>
public static class CombatRules
{
    public const float PowerDiceBonusPerExtraDice = 0.10f; // 10% por dado extra

    public static float GetPowerMultiplier(DiceStatType statType, DiceTier tier)
    {
        return statType switch
        {
            DiceStatType.Mind => tier switch { DiceTier.Low => 0.6f, DiceTier.Medium => 1f, DiceTier.High => 1.4f, _ => 1f },
            DiceStatType.Heart => tier switch { DiceTier.Low => 0.4f, DiceTier.Medium => 1f, DiceTier.High => 1.6f, _ => 1f },
            DiceStatType.Body => tier switch { DiceTier.Low => 0.4f, DiceTier.Medium => 1f, DiceTier.High => 1.5f, _ => 1f },
            _ => 1f,
        };
    }

    public static float GetCommitmentMultiplier(int allocatedPowerDiceCount)
    {
        int extraDice = Mathf.Max(0, allocatedPowerDiceCount - 1);
        return 1f + (PowerDiceBonusPerExtraDice * extraDice);
    }

    public static int CalculateBaseDamage(int basePower, DiceStatType statType, DiceTier tier, int allocatedPowerDiceCount)
    {
        float statMult = GetPowerMultiplier(statType, tier);
        float commitMult = GetCommitmentMultiplier(allocatedPowerDiceCount);
        return Mathf.RoundToInt(basePower * statMult * commitMult);
    }

    public static DiceTier GetTierFromBoundaries(int value, (int lowMax, int mediumMax, int highMin, int maxValue) boundaries)
    {
        if (value <= boundaries.lowMax) return DiceTier.Low;
        if (value <= boundaries.mediumMax) return DiceTier.Medium;
        return DiceTier.High;
    }

    public static ActionAccuracy GetAccuracyOutcome(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => ActionAccuracy.Missed,
            DiceTier.Medium => ActionAccuracy.Hit,
            DiceTier.High => ActionAccuracy.Critical,
            _ => ActionAccuracy.Hit,
        };
    }
}