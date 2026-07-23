using System.Collections.Generic;
using UnityEngine;

public enum AllocationConsistency 
{ 
    Balanced, 
    Consistent, 
    Risky 
}

public static class DiceAllocationCalculator
{
    public static DiceAllocationContext CalculatePreview(
        int baseActionPower, 
        IReadOnlyList<DiceStatType> powerDiceTypes,
        IReadOnlyList<int> powerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries,
        IReadOnlyDictionary<DiceStatType, int> statBaseTargets,
        DiceStatType powerPrimaryStat,   
        int allocatedPowerDiceCount)   
    {
        var data = new DiceAllocationContext
        {
            PowerDiceTypes = powerDiceTypes,
            PowerFaces = powerFaces,
            AccuracyDiceTypes = accuracyDiceTypes,
            AccuracyFaces = accuracyFaces,
            PowerTierBoundaries = powerTierBoundaries,
            AccuracyTierBoundaries = accuracyTierBoundaries,
            HasPower = powerDiceTypes != null && powerDiceTypes.Count > 0,
            HasAccuracy = accuracyDiceTypes != null && accuracyDiceTypes.Count > 0
        };

        if (!data.HasPower && !data.HasAccuracy)
            return data;

        data.HitThreshold = accuracyTierBoundaries.lowMax + 1;
        data.CriticalThreshold = accuracyTierBoundaries.highMin;
        data.MissThreshold = accuracyTierBoundaries.lowMax;

        Dictionary<int, float> powerDistribution = data.HasPower ? CalculateBestRollDistribution(powerDiceTypes, powerFaces) : null;
        Dictionary<int, float> accuracyDistribution = data.HasAccuracy ? CalculateBestRollDistribution(accuracyDiceTypes, accuracyFaces) : null;
        
        data.PowerChances = data.HasPower ? CalculateTierChances(powerDistribution, powerTierBoundaries) : new TierChances();
        data.AccuracyChances = data.HasAccuracy ? CalculateTierChances(accuracyDistribution, accuracyTierBoundaries) : new TierChances();

        data.PowerMinRollChance = data.HasPower ? CalculateMinRollChance(powerDistribution) : 0f;
        data.AccuracyMinRollChance = data.HasAccuracy ? CalculateMinRollChance(accuracyDistribution) : 0f;

        data.PowerMaxRollChance = data.HasPower ? CalculateMaxRollChance(powerDiceTypes, powerFaces, statBaseTargets) : 0f;
        data.AccuracyMaxRollChance = data.HasAccuracy ? CalculateMaxRollChance(accuracyDiceTypes, accuracyFaces, statBaseTargets) : 0f;

        if (data.HasPower)
        {
            (int minPower, int maxPower) = GetDistributionBounds(powerDistribution);
            
            data.MinPowerTier = CombatRules.GetTierFromBoundaries(minPower, powerTierBoundaries);
            data.MaxPowerTier = CombatRules.GetTierFromBoundaries(maxPower, powerTierBoundaries);
            
            data.MinDamage = CombatRules.CalculateBaseDamage(baseActionPower, powerPrimaryStat, data.MinPowerTier, allocatedPowerDiceCount);
            data.MaxDamage = CombatRules.CalculateBaseDamage(baseActionPower, powerPrimaryStat, data.MaxPowerTier, allocatedPowerDiceCount);
        }

        if (data.HasPower && data.HasAccuracy)
        {
            data.Consistency = GetAllocationConsistency(data.PowerChances, data.AccuracyChances);
        }

        return data;
    }

    private static AllocationConsistency GetAllocationConsistency(TierChances power, TierChances accuracy)
    {
        float favorable = accuracy.High + accuracy.Medium * power.High;
        float unfavorable = accuracy.Low + accuracy.Medium * power.Low;
        float medium = accuracy.Medium * power.Medium;

        if (medium >= favorable && medium >= unfavorable)
            return AllocationConsistency.Balanced;
        if (favorable > unfavorable)
            return AllocationConsistency.Consistent;
        
        return AllocationConsistency.Risky;
    }

    private static Dictionary<int, float> CalculateBestRollDistribution(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces)
    {
        if (types == null || faces == null || types.Count == 0 || types.Count != faces.Count)
            return new Dictionary<int, float> { { 1, 1f } };

        var groupFaces = new Dictionary<DiceStatType, List<int>>();
        for (int i = 0; i < types.Count; i++)
        {
            if (!groupFaces.TryGetValue(types[i], out var list))
            {
                list = new List<int>();
                groupFaces[types[i]] = list;
            }
            list.Add(Mathf.Max(1, faces[i]));
        }

        Dictionary<int, float> bestDistribution = new() { { 0, 1f } };
        foreach (var group in groupFaces.Values)
        {
            Dictionary<int, float> groupDistribution = CalculateGroupSumDistribution(group);
            Dictionary<int, float> nextBestDistribution = new();
            foreach (var best in bestDistribution)
            {
                foreach (var groupValue in groupDistribution)
                {
                    int value = Mathf.Max(best.Key, groupValue.Key);
                    float chance = best.Value * groupValue.Value;
                    nextBestDistribution[value] = nextBestDistribution.TryGetValue(value, out float accumulated) ? accumulated + chance : chance;
                }
            }
            bestDistribution = nextBestDistribution;
        }
        return bestDistribution;
    }

    private static Dictionary<int, float> CalculateGroupSumDistribution(List<int> faces)
    {
        Dictionary<int, float> distribution = new() { { 0, 1f } };
        foreach (int faceCount in faces)
        {
            Dictionary<int, float> nextDistribution = new();
            foreach (var current in distribution)
                for (int face = 1; face <= faceCount; face++)
                {
                    int value = current.Key + face;
                    float chance = current.Value / faceCount;
                    nextDistribution[value] = nextDistribution.TryGetValue(value, out float accumulated) ? accumulated + chance : chance;
                }
            distribution = nextDistribution;
        }
        return distribution;
    }

    private static TierChances CalculateTierChances(Dictionary<int, float> distribution, (int lowMax, int mediumMax, int highMin, int maxValue) boundaries)
    {
        TierChances chances = new();
        foreach (var result in distribution)
        {
            if (result.Key <= boundaries.lowMax) chances.Low += result.Value;
            else if (result.Key <= boundaries.mediumMax) chances.Medium += result.Value;
            else chances.High += result.Value;
        }
        return chances;
    }

    private static float CalculateMinRollChance(Dictionary<int, float> distribution)
    {
        (int minimum, _) = GetDistributionBounds(distribution);
        return distribution.TryGetValue(minimum, out float chance) ? chance : 0f;
    }

    private static float CalculateMaxRollChance(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces, IReadOnlyDictionary<DiceStatType, int> statBaseTargets)
    {
        if (types == null || faces == null || types.Count == 0 || types.Count != faces.Count) return 0f;

        var groupFaces = new Dictionary<DiceStatType, List<int>>();
        for (int i = 0; i < types.Count; i++)
        {
            if (!groupFaces.TryGetValue(types[i], out var list))
            {
                list = new List<int>();
                groupFaces[types[i]] = list;
            }
            list.Add(Mathf.Max(1, faces[i]));
        }

        float missChance = 1f;
        foreach (var group in groupFaces)
        {
            Dictionary<int, float> groupDistribution = CalculateGroupSumDistribution(group.Value);
            int target = statBaseTargets != null && statBaseTargets.TryGetValue(group.Key, out int statTarget) ? statTarget : SumFaces(group.Value);

            float hitChance = 0f;
            foreach (var outcome in groupDistribution)
                if (outcome.Key >= target) hitChance += outcome.Value;

            missChance *= 1f - Mathf.Clamp01(hitChance);
        }
        return 1f - missChance;
    }

    private static int SumFaces(List<int> faces)
    {
        int sum = 0;
        for (int i = 0; i < faces.Count; i++) sum += faces[i];
        return sum;
    }

    private static (int minimum, int maximum) GetDistributionBounds(Dictionary<int, float> distribution)
    {
        int minimum = int.MaxValue;
        int maximum = int.MinValue;
        foreach (int value in distribution.Keys)
        {
            minimum = Mathf.Min(minimum, value);
            maximum = Mathf.Max(maximum, value);
        }
        return (minimum, maximum);
    }
}