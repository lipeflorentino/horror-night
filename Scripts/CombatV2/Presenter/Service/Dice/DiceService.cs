using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceService
{
    private const float CombatStatThresholdShift = 0.05f;
    private readonly System.Random random = new();
    private readonly PerkService perkService;

    public DiceService(PerkService perkService = null)
    {
        this.perkService = perkService;
    }

    private readonly struct DiceRollSpec
    {
        public readonly int MinValue;
        public readonly int MaxValue;
        public readonly DiceStatType StatType;
        public readonly DiceRollType RollType;
        public readonly bool IsExtra;

        public DiceRollSpec(int minValue, int maxValue, DiceStatType statType, DiceRollType rollType, bool isExtra = false)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            StatType = statType;
            RollType = rollType;
            IsExtra = isExtra;
        }
    }


    private struct ThresholdPair
    {
        public float Low;
        public float High;

        public ThresholdPair(float low, float high)
        {
            Low = low;
            High = high;
        }
    }

    public DiceResult Roll(int maxValue, int attackerLevel, int defenderLevel, DiceStatType statType, DiceRollType rollType, int minValue = 1, int focus = 0, int strength = 0, bool isExtra = false)
    {
        CombatRollContext context = new(null, null, ActionType.Attack, rollType, statType, attackerLevel, defenderLevel, focus, strength, maxValue);
        return Roll(context, minValue, isExtra);
    }

    private DiceResult Roll(CombatRollContext context, int minValue = 1, bool isExtra = false)
    {
        int safeMaxValue = Math.Max(1, context.MaxValue);
        int safeMinValue = Mathf.Clamp(minValue, 1, safeMaxValue);
        int value = random.Next(safeMinValue, safeMaxValue + 1);
        CombatRollContext safeContext = context.WithRoll(context.RollType, context.StatType, safeMaxValue);
        DiceTier tier = GetTier(value, safeContext);

        return new DiceResult(value, tier, safeMaxValue, context.StatType, context.RollType, safeMinValue)
        {
            IsExtra = isExtra
        };
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
        int focus = perkService != null ? perkService.GetEffectiveFocus(actor, opponent, actionType) : actor?.Focus ?? 0;
        int strength = perkService != null ? perkService.GetEffectiveStrength(actor, opponent, actionType) : actor?.Strength ?? 0;
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(actor, opponent, diceTypes, actionType, rollType, actorLevel, opponentLevel, focus, strength, evaluateRollTriggers: true);

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
            rawResults.Add(Roll(context, spec.MinValue, spec.IsExtra));
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
                firstResult.SubRolls.Add(roll);
                aggregatedByStat[roll.StatType] = firstResult;
                orderedResults.Add(firstResult);
                continue;
            }

            aggregate.SubRolls.Add(roll);
            aggregate.IsExtra = aggregate.IsExtra || roll.IsExtra;
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
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, null, diceTypes, ActionType.Attack, DiceRollType.Power, 1, 1, battler?.Focus ?? 0, battler?.Strength ?? 0, evaluateRollTriggers: false);
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
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, null, diceTypes, ActionType.Attack, DiceRollType.Power, 1, 1, battler?.Focus ?? 0, battler?.Strength ?? 0, evaluateRollTriggers: false);
        for (int i = 0; i < diceSpecs.Count; i++)
            diceFaces.Add(diceSpecs[i].MaxValue);

        return diceFaces;
    }

    /// <summary>
    /// Igual a ConvertToFaces, mas retorna também a lista de tipos alinhada 1:1 com as faces
    /// (incluindo dados extras concedidos por perk, que não têm correspondência na seleção bruta do jogador).
    /// Use esta versão sempre que "types" e "faces" forem consumidos juntos (ex.: preview de UI, cálculo de chance).
    /// </summary>
    public (List<DiceStatType> types, List<int> faces) ConvertToFacesWithTypes(Battler battler, IReadOnlyList<DiceStatType> diceTypes)
    {
        List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, null, diceTypes, ActionType.Attack, DiceRollType.Power, 1, 1, battler?.Focus ?? 0, battler?.Strength ?? 0, evaluateRollTriggers: false);
        List<DiceStatType> types = new(diceSpecs.Count);
        List<int> faces = new(diceSpecs.Count);
        for (int i = 0; i < diceSpecs.Count; i++)
        {
            types.Add(diceSpecs[i].StatType);
            faces.Add(diceSpecs[i].MaxValue);
        }

        return (types, faces);
    }

    private List<DiceRollSpec> BuildDiceRollSpecs(Battler battler, Battler opponent, IReadOnlyList<DiceStatType> diceTypes, ActionType actionType, DiceRollType rollType, int actorLevel, int opponentLevel, int focus, int strength, bool evaluateRollTriggers = true)
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
            int baseDiceCount = Mathf.Max(0, pair.Value);
            if (totalValue <= 0 || baseDiceCount <= 0)
                continue;

            CombatRollContext perkContext = new(battler, opponent, actionType, rollType, pair.Key, actorLevel, opponentLevel, focus, strength, totalValue);
            int extraDice = perkService?.GetExtraDiceCount(battler, opponent, perkContext, evaluateRollTriggers) ?? 0;

            // Dados base dividem o valor total da stat entre si (aloc. do jogador).
            int baseFace = Mathf.Max(1, totalValue / baseDiceCount);
            int remainder = Mathf.Max(0, totalValue - (baseFace * baseDiceCount));

            for (int i = 0; i < baseDiceCount; i++)
            {
                int bonus = i < remainder ? 1 : 0;
                int maxFace = baseFace + bonus;
                AddDiceSpec(diceSpecs, battler, opponent, actionType, rollType, pair.Key, actorLevel, opponentLevel, focus, strength, agility, maxFace, isExtra: false);
            }

            // Dados extras concedidos por perk usam o valor total da stat com face própria,
            // sem diluir os dados base já alocados pelo jogador.
            for (int i = 0; i < extraDice; i++)
                AddDiceSpec(diceSpecs, battler, opponent, actionType, rollType, pair.Key, actorLevel, opponentLevel, focus, strength, agility, Mathf.Max(1, totalValue), isExtra: true);
        }

        return diceSpecs;
    }

    private void AddDiceSpec(List<DiceRollSpec> diceSpecs, Battler battler, Battler opponent, ActionType actionType, DiceRollType rollType, DiceStatType statType, int actorLevel, int opponentLevel, int focus, int strength, int agility, int maxFace, bool isExtra)
    {
        int minFace = Mathf.Clamp(1 + agility, 1, maxFace);
        CombatRollContext minRollContext = new(battler, opponent, actionType, rollType, statType, actorLevel, opponentLevel, focus, strength, maxFace);
        minFace = perkService?.GetMinimumRollValue(battler, opponent, minRollContext, minFace, false) ?? minFace;
        diceSpecs.Add(new DiceRollSpec(minFace, maxFace, statType, rollType, isExtra));
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

    private int GetTierReferenceMaxValue(CombatRollContext context)
    {
        if (context.Actor != null)
        {
            int baseValue = context.Actor.GetBaseStatValue(context.StatType);
            if (baseValue > 0)
                return Mathf.Max(1, baseValue);
        }

        return Mathf.Max(1, context.MaxValue);
    }

    private DiceTier GetTier(int value, CombatRollContext context)
    {
        int tierReferenceMaxValue = GetTierReferenceMaxValue(context);
        if (tierReferenceMaxValue <= 1)
            return DiceTier.Low;

        float normalized = (float)value / tierReferenceMaxValue;
        ThresholdPair thresholds = GetThresholds(context, tierReferenceMaxValue);

        if (normalized <= thresholds.Low) return DiceTier.Low;
        if (normalized <= thresholds.High) return DiceTier.Medium;
        return DiceTier.High;
    }

    private ThresholdPair GetThresholds(CombatRollContext context, int tierReferenceMaxValue)
    {
        int safeMaxValue = Mathf.Max(1, tierReferenceMaxValue);
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
        if (perkService != null)
        {
            var (low, high) = perkService.GetModifiedRollThresholds(context.Actor, context.Opponent, context, thresholds.Low, thresholds.High);
            thresholds.Low = low;
            thresholds.High = high;
        }

        thresholds.Low = Mathf.Clamp(thresholds.Low, 0.05f, 0.45f);
        thresholds.High = Mathf.Clamp(thresholds.High, 0.55f, 0.95f);

        if (thresholds.High < thresholds.Low + 0.2f)
            thresholds.High = Mathf.Min(0.95f, thresholds.Low + 0.2f);

        return thresholds;
    }

    public (int lowMax, int mediumMax, int highMin, int maxValue) GetTierBoundaries(int maxValue, int attackerLevel, int defenderLevel, DiceStatType statType, DiceRollType rollType, int focus = 0, int strength = 0)
    {
        CombatRollContext context = new(null, null, ActionType.Attack, rollType, statType, attackerLevel, defenderLevel, focus, strength, maxValue);
        return GetTierBoundaries(context);
    }

    public (int lowMax, int mediumMax, int highMin, int maxValue) GetTierBoundaries(CombatRollContext context)
    {
        int tierReferenceMaxValue = GetTierReferenceMaxValue(context);
        int safeMaxValue = Math.Max(1, tierReferenceMaxValue);
        ThresholdPair thresholds = GetThresholds(context.WithRoll(context.RollType, context.StatType, safeMaxValue), safeMaxValue);

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

        return (lowMax, mediumMax, highMin, safeMaxValue);
    }
}