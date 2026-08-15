using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fonte Única de Verdade (SSOT) para os cálculos matemáticos do combate.
/// </summary>
public static class CombatRules
{
    public const float PowerDiceBonusPerExtraDice = 0.10f;
    // TODO: integrar com sistema de progressão (level, mods) — hoje são valores fixos.
    public const int MinCoreStatValue = 4;
    public const int MaxCoreStatValue = 20;

    // Deslocamento de threshold por dado alocado (ver GetBaseThresholds).
    public const float MindLowReductionPerDice = 0.10f;
    public const float HeartExtremeShiftPerDice = 0.20f;
    public const float BodyExtremeReductionPerDice = 0.15f;

    // Clamps de segurança para os thresholds finais (mantém a matemática sempre válida).
    private const float ThresholdExtremeMin = 0.05f;
    private const float ThresholdExtremeMax = 0.95f;
    private const float ThresholdMinGap = 0.10f;

    public enum ThresholdStrategy { Safe, Balanced, Risky }

    // Presets de threshold (low / high). Medium é o intervalo restante entre eles.
    private static readonly Dictionary<ThresholdStrategy, (float low, float high)> StrategyThresholds = new()
    {
        { ThresholdStrategy.Safe,     (0.20f, 0.90f) }, // 20% low / 70% medium / 10% high
        { ThresholdStrategy.Balanced, (0.30f, 0.70f) }, // 30% low / 40% medium / 30% high
        { ThresholdStrategy.Risky,    (0.40f, 0.60f) }, // 40% low / 20% medium / 40% high
    };

    public static ThresholdStrategy PlayerStrategy { get; private set; } = ThresholdStrategy.Balanced;

    public static void SetPlayerStrategy(ThresholdStrategy strategy)
    {
        PlayerStrategy = strategy;
    }

    public static float GetPowerMultiplier(DiceStatType statType, DiceTier tier)
    {
        return statType switch
        {
            DiceStatType.Mind => tier switch { DiceTier.Low => 0.4f, DiceTier.Medium => 0.6f, DiceTier.High => 1f, _ => 1f },
            DiceStatType.Heart => tier switch { DiceTier.Low => 0.2f, DiceTier.Medium => 1f, DiceTier.High => 1.6f, _ => 1f },
            DiceStatType.Body => tier switch { DiceTier.Low => 0.8f, DiceTier.Medium => 1.4f, DiceTier.High => 1.8f, _ => 1f },
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
        return Mathf.RoundToInt(basePower * GetDamageMultiplier(statType, tier, allocatedPowerDiceCount));
    }

    public static float GetDamageMultiplier(DiceStatType statType, DiceTier tier, int allocatedPowerDiceCount)
    {
        float statMult = GetPowerMultiplier(statType, tier);
        float commitMult = GetCommitmentMultiplier(allocatedPowerDiceCount);
        return statMult * commitMult;
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

    /// <summary>
    /// Calcula os limiares de probabilidade baseado na quantidade e tipo de dado alocado.
    /// Lógica de limites: Aumentar o threshold Low = Mais falhas. Diminuir o threshold High = Mais críticos.
    /// </summary>
    public static (float low, float high) GetBaseThresholds(DiceStatType statType, DiceRollType rollType, int allocatedDiceCount, ThresholdStrategy strategy)
    {
        var (presetLow, presetHigh) = StrategyThresholds[strategy];
        float low = presetLow;
        float high = presetHigh;

        /* int count = Mathf.Max(1, allocatedDiceCount);

        if (rollType == DiceRollType.Accuracy && allocatedDiceCount > 0)
        {
            switch (statType)
            {
                case DiceStatType.Mind:
                    // Diminui chance de low em 10% por dado
                    low -= MindLowReductionPerDice * count;
                    break;
                case DiceStatType.Heart:
                    // Aumenta chance dos extremos (Low e High) em 20% por dado
                    low += HeartExtremeShiftPerDice * count;  // Mais Low
                    high += HeartExtremeShiftPerDice * count; // Mais High
                    break;
                case DiceStatType.Body:
                    // Aumenta medio diminuindo os extremos em 15% por dado
                    low -= BodyExtremeReductionPerDice * count;  // Menos Low
                    high += BodyExtremeReductionPerDice * count; // Menos High
                    break;
            }
        }
        else if (rollType == DiceRollType.Power && allocatedDiceCount > 0)
        {
            switch (statType)
            {
                case DiceStatType.Mind:
                    // Diminui chance de low em 10% por dado
                    low -= MindLowReductionPerDice * count;
                    break;
                case DiceStatType.Heart:
                    // Aumenta chance dos extremos (Low e High) em 20% por dado
                    low += HeartExtremeShiftPerDice * count;  // Mais Low
                    high -= HeartExtremeShiftPerDice * count; // Mais High
                    break;
                case DiceStatType.Body:
                    // Aumenta medio diminuindo os extremos em 15% por dado
                    low -= BodyExtremeReductionPerDice * count;  // Menos Low
                    high += BodyExtremeReductionPerDice * count; // Menos High
                    break;
            }
        } */

        // TODO: ajustar logica
        // Garante que o sistema nunca quebre a matemática (mantém mínimo de 5% para cada extremo)
        low = Mathf.Clamp(low, ThresholdExtremeMin, ThresholdExtremeMax - ThresholdMinGap);
        high = Mathf.Clamp(high, low + ThresholdMinGap, ThresholdExtremeMax);

        return (low, high);
    }

    public static int GetDamageBonus(ActionResolutionVariation variation)
    {
        return variation switch
        {
            ActionResolutionVariation.IronWall => -3,
            ActionResolutionVariation.Stronghold => -3,
            ActionResolutionVariation.PiercingHit => 2,
            ActionResolutionVariation.PowerHit => 3,
            ActionResolutionVariation.CriticalHit => 4,
            ActionResolutionVariation.ArmorShatter => 5,
            ActionResolutionVariation.Overpower => 6,
            ActionResolutionVariation.DevastatingStrike => 7,
            ActionResolutionVariation.Deathstroke => 10,
            ActionResolutionVariation.LegendaryClash => 8,
            _ => 0
        };
    }

    public static int GetStatPriority(DiceStatType statType, DiceRollType rollType)
    {
        return rollType switch
        {
            DiceRollType.Accuracy => statType switch
            {
                DiceStatType.Mind => 3,
                DiceStatType.Heart => 2,
                DiceStatType.Body => 1,
                _ => 0
            },
            DiceRollType.Power => statType switch
            {
                DiceStatType.Body => 3,
                DiceStatType.Heart => 2,
                DiceStatType.Mind => 1,
                _ => 0
            },
            _ => 0
        };
    }

    public static void VerifyWearness(IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes, Battler battler, DrawbackService drawbackService)
    {
        Dictionary<DiceStatType, int> diceCounts = new();
        if (powerDiceTypes != null)
        {
            foreach (var stat in powerDiceTypes)
            {
                diceCounts.TryAdd(stat, 0);
                diceCounts[stat]++;
            }
        }
        if (accuracyDiceTypes != null)
        {
            foreach (var stat in accuracyDiceTypes)
            {
                diceCounts.TryAdd(stat, 0);
                diceCounts[stat]++;
            }
        }

        foreach (var kvp in diceCounts)
        {
            if (kvp.Value >= 3)
            {
                string drawbackId = kvp.Key switch
                {
                    DiceStatType.Mind => "wear_mind",
                    DiceStatType.Heart => "wear_heart",
                    DiceStatType.Body => "wear_body",
                    _ => null
                };
                
                if (drawbackId != null)
                {
                    drawbackService.ApplyDrawback(battler, drawbackId);
                }
            }
        }
    }
}