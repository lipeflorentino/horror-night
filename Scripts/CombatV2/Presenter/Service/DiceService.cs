using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceService
{
    private const float CombatStatThresholdShift = 0.05f;
    private readonly System.Random random = new();
    private readonly BattlerStateService stateService;
    private readonly PerkService perkService;

    public DiceService(BattlerStateService stateService = null, PerkService perkService = null)
    {
        this.stateService = stateService;
        this.perkService = perkService;
    }

    private readonly struct DiceRollSpec
    {
        public readonly int MinValue;
        public readonly int MaxValue;
        public readonly DiceStatType StatType;
        public readonly DiceRollType RollType;

        public DiceRollSpec(int minValue, int maxValue, DiceStatType statType, DiceRollType rollType)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            StatType = statType;
            RollType = rollType;
        }
    }

    public DiceResult Roll(int maxValue, int attackerLevel, int defenderLevel, DiceStatType statType, DiceRollType rollType, int minValue = 1, int focus = 0, int strength = 0)
    {
        CombatRollContext context = new(null, null, ActionType.Attack, rollType, statType, attackerLevel, defenderLevel, focus, strength, maxValue);
        return Roll(context, minValue);
    }

    private DiceResult Roll(CombatRollContext context, int minValue = 1)
    {
        int safeMaxValue = Math.Max(1, context.MaxValue);
        int safeMinValue = Mathf.Clamp(minValue, 1, safeMaxValue);
        int value = random.Next(safeMinValue, safeMaxValue + 1);
        CombatRollContext safeContext = context.WithRoll(context.RollType, context.StatType, safeMaxValue);
        DiceTier tier = GetTier(value, safeContext);

        Logger.Log($"[Dice] Rolled {context.ActionType} {context.RollType} {context.StatType} d{safeMaxValue} ({safeMinValue}-{safeMaxValue}): {value} -> {tier}");

        return new DiceResult(value, tier, safeMaxValue, context.StatType, context.RollType, safeMinValue);
    }

    public DiceResult GetBestResult(List<DiceResult> rolls)
    {
        DiceResult best = null;
        for (int i = 0; i < rolls.Count; i++)
            if (best == null || IsBetterRoll(rolls[i], best))
                best = rolls[i];

        return best;
    }

    private bool IsBetterRoll(DiceResult candidate, DiceResult currentBest)
    {
        if (candidate.Value != currentBest.Value)
            return candidate.Value > currentBest.Value;

        int candidatePriority = GetStatPriority(candidate.StatType);
        int currentPriority = GetStatPriority(currentBest.StatType);
        if (candidatePriority != currentPriority)
            return candidatePriority > currentPriority;

        return false;
    }

    private int GetStatPriority(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => 3,
            DiceStatType.Heart => 2,
            DiceStatType.Body => 1,
            _ => 0
        };
    }

    public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, DiceRollType rollType, int attackerLevel = 1, int defenderLevel = 1)
    {
        return RollMany(battler, null, diceTypes, ActionType.Attack, rollType, attackerLevel, defenderLevel);
    }

    public List<DiceResult> RollMany(Battler actor, Battler opponent, IReadOnlyList<DiceStatType> diceTypes, ActionType actionType, DiceRollType rollType, int actorLevel = 1, int opponentLevel = 1)
    {
        int focus = stateService != null ? stateService.GetEffectiveFocus(actor, opponent, actionType) : actor?.Focus ?? 0;
        int strength = stateService != null ? stateService.GetEffectiveStrength(actor, opponent, actionType) : actor?.Strength ?? 0;
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(actor, opponent, diceTypes, actionType, rollType, actorLevel, opponentLevel, focus, strength);

        if (diceSpecs.Count == 0)
        {
            CombatRollContext fallbackContext = new(actor, opponent, actionType, rollType, DiceStatType.Body, actorLevel, opponentLevel, focus, strength, 1);
            return new List<DiceResult> { Roll(fallbackContext, 1) };
        }

        ConsumeDicePool(actor, rollType, diceTypes?.Count ?? diceSpecs.Count);

        List<DiceResult> rawResults = new(diceSpecs.Count);
        for (int i = 0; i < diceSpecs.Count; i++)
        {
            DiceRollSpec spec = diceSpecs[i];
            CombatRollContext context = new(actor, opponent, actionType, spec.RollType, spec.StatType, actorLevel, opponentLevel, focus, strength, spec.MaxValue);
            rawResults.Add(Roll(context, spec.MinValue));
        }

        CombatRollContext aggregateContext = new(actor, opponent, actionType, rollType, DiceStatType.Body, actorLevel, opponentLevel, focus, strength, 1);
        return AggregateDuplicateStatResults(rawResults, aggregateContext);
    }

    private void ConsumeDicePool(Battler battler, DiceRollType rollType, int spentDiceCount)
    {
        if (battler == null || spentDiceCount <= 0)
            return;

        if (rollType == DiceRollType.Power)
            battler.CurrentPowerDices = Mathf.Max(0, battler.CurrentPowerDices - spentDiceCount);
        else
            battler.CurrentAccuracyDices = Mathf.Max(0, battler.CurrentAccuracyDices - spentDiceCount);
    }

    private List<DiceResult> AggregateDuplicateStatResults(List<DiceResult> rawResults, CombatRollContext baseContext)
    {
        Dictionary<DiceStatType, DiceResult> aggregatedByStat = new();
        List<DiceResult> orderedResults = new();

        for (int i = 0; i < rawResults.Count; i++)
        {
            DiceResult roll = rawResults[i];
            if (!aggregatedByStat.TryGetValue(roll.StatType, out DiceResult aggregate))
            {
                DiceResult firstResult = new(roll.Value, roll.Tier, roll.MaxValue, roll.StatType, roll.RollType, roll.MinValue);
                aggregatedByStat[roll.StatType] = firstResult;
                orderedResults.Add(firstResult);
                continue;
            }

            aggregate.Value += roll.Value;
            aggregate.MinValue += roll.MinValue;
            aggregate.MaxValue += roll.MaxValue;
            CombatRollContext aggregateContext = baseContext.WithRoll(aggregate.RollType, aggregate.StatType, aggregate.MaxValue);
            aggregate.Tier = GetTier(aggregate.Value, aggregateContext);
        }
        
        return orderedResults;
    }

    public List<int> ConvertToAggregatedFaces(Battler battler, IReadOnlyList<DiceStatType> diceTypes)
    {
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, null, diceTypes, ActionType.Attack, DiceRollType.Power, 1, 1, battler?.Focus ?? 0, battler?.Strength ?? 0);
        Dictionary<DiceStatType, int> facesByType = new();
        for (int i = 0; i < diceSpecs.Count; i++)
        {
            DiceRollSpec spec = diceSpecs[i];
            facesByType[spec.StatType] = facesByType.TryGetValue(spec.StatType, out int currentFaces)
                ? currentFaces + spec.MaxValue
                : spec.MaxValue;
        }
        
        List<int> diceFaces = new();
        foreach (KeyValuePair<DiceStatType, int> pair in facesByType)
            diceFaces.Add(pair.Value);

        return diceFaces;
    }

    public List<int> ConvertToFaces(Battler battler, IReadOnlyList<DiceStatType> diceTypes)
    {
        List<int> diceFaces = new();
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, null, diceTypes, ActionType.Attack, DiceRollType.Power, 1, 1, battler?.Focus ?? 0, battler?.Strength ?? 0);
        for (int i = 0; i < diceSpecs.Count; i++)
            diceFaces.Add(diceSpecs[i].MaxValue);

        return diceFaces;
    }

    private List<DiceRollSpec> BuildDiceRollSpecs(Battler battler, Battler opponent, IReadOnlyList<DiceStatType> diceTypes, ActionType actionType, DiceRollType rollType, int actorLevel, int opponentLevel, int focus, int strength)
    {
        List<DiceRollSpec> diceSpecs = new();
        if (diceTypes == null)
            return diceSpecs;

        Dictionary<DiceStatType, int> diceCountByType = new();
        for (int i = 0; i < diceTypes.Count; i++)
        {
            DiceStatType type = diceTypes[i];
            diceCountByType[type] = diceCountByType.TryGetValue(type, out int count) ? count + 1 : 1;
        }

        int agility = Mathf.Max(0, battler?.Agility ?? 0);
        foreach (KeyValuePair<DiceStatType, int> pair in diceCountByType)
        {
            int totalValue = GetDiceMaxValueForType(battler, pair.Key);
            int diceCount = Mathf.Max(0, pair.Value);
            if (totalValue <= 0 || diceCount <= 0)
                continue;

            CombatRollContext perkContext = new(battler, opponent, actionType, rollType, pair.Key, actorLevel, opponentLevel, focus, strength, totalValue);
            diceCount += perkService?.GetExtraDiceCount(battler, opponent, perkContext) ?? 0;

            int baseFace = Mathf.Max(1, totalValue / diceCount);
            int remainder = Mathf.Max(0, totalValue - (baseFace * diceCount));

            if (battler != null && !battler.IsPlayer) Logger.Log($"[DiceService] Building dice for {pair.Key}: totalValue={totalValue}, diceCount={diceCount}, baseFace={baseFace}, remainder={remainder}");

            for (int i = 0; i < diceCount; i++)
            {
                int bonus = i < remainder ? 1 : 0;
                int maxFace = baseFace + bonus;
                int minFace = Mathf.Clamp(1 + agility, 1, maxFace);
                CombatRollContext minRollContext = new(battler, opponent, actionType, rollType, pair.Key, actorLevel, opponentLevel, focus, strength, maxFace);
                minFace = perkService?.GetMinimumRollValue(battler, opponent, minRollContext, minFace) ?? minFace;
                diceSpecs.Add(new DiceRollSpec(minFace, maxFace, pair.Key, rollType));
                LogDiceStatBonus(pair.Key);
            }
        }

        return diceSpecs;
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
    }

    private DiceTier GetTier(int value, CombatRollContext context)
    {
        if (context.MaxValue <= 1)
            return DiceTier.Low;

        float normalized = (float)value / context.MaxValue;
        ThresholdPair thresholds = GetThresholds(context);

        if (normalized <= thresholds.Low) return DiceTier.Low;
        if (normalized <= thresholds.High) return DiceTier.Medium;
        return DiceTier.High;
    }

    private ThresholdPair GetThresholds(CombatRollContext context)
    {
        int safeMaxValue = Mathf.Max(1, context.MaxValue);
        int delta = context.ActorLevel - context.OpponentLevel;

        const float baseLowThreshold = 0.25f;
        const float baseHighThreshold = 0.75f;

        float granularity = Mathf.Clamp01((safeMaxValue - 1f) / 11f);
        float deltaScale = Mathf.Lerp(7f, 4f, granularity);
        float normalizedDelta = Mathf.Clamp(delta / deltaScale, -1f, 1f);

        float maxShift = Mathf.Lerp(0.10f, 0.18f, granularity);
        float levelShift = normalizedDelta * maxShift;
        int combatStat = context.RollType == DiceRollType.Accuracy ? context.Focus : context.Strength;
        float combatStatShift = Mathf.Max(0, combatStat) * CombatStatThresholdShift;
        float shift = levelShift + combatStatShift;

        ThresholdPair thresholds = new(baseLowThreshold - shift, baseHighThreshold - shift);
        if (stateService != null)
            thresholds = stateService.ApplyThresholdModifiers(thresholds, context);

        thresholds.Low = Mathf.Clamp(thresholds.Low, 0.05f, 0.45f);
        thresholds.High = Mathf.Clamp(thresholds.High, 0.55f, 0.95f);

        if (thresholds.High < thresholds.Low + 0.2f)
            thresholds.High = Mathf.Min(0.95f, thresholds.Low + 0.2f);

        return thresholds;
    }

    public (int lowMax, int mediumMax, int highMin) GetTierBoundaries(int maxValue, int attackerLevel, int defenderLevel, DiceStatType statType, DiceRollType rollType, int focus = 0, int strength = 0)
    {
        CombatRollContext context = new(null, null, ActionType.Attack, rollType, statType, attackerLevel, defenderLevel, focus, strength, maxValue);
        return GetTierBoundaries(context);
    }

    public (int lowMax, int mediumMax, int highMin) GetTierBoundaries(CombatRollContext context)
    {
        int safeMaxValue = Math.Max(1, context.MaxValue);
        ThresholdPair thresholds = GetThresholds(context.WithRoll(context.RollType, context.StatType, safeMaxValue));

        int lowMax = 0;
        int mediumMax = 0;
        int highMin = 0;

        for (int value = 1; value <= safeMaxValue; value++)
        {
            float normalized = (float)value / safeMaxValue;
            if (normalized <= thresholds.Low)
            {
                lowMax = value;
                mediumMax = value;
                continue;
            }

            if (normalized <= thresholds.High)
            {
                mediumMax = value;
                continue;
            }

            highMin = value;
            break;
        }

        if (mediumMax < lowMax)
            mediumMax = lowMax;

        return (lowMax, mediumMax, highMin);
    }
}
