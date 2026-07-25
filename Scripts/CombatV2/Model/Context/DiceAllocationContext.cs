using System.Collections.Generic;

public struct TierChances 
{
    public float Low;
    public float Medium;
    public float High;
}

public class DiceAllocationContext
{
    public bool HasPower;
    public bool HasAccuracy;
    // Dados brutos para reconstrução visual na View
    public IReadOnlyList<DiceStatType> PowerDiceTypes;
    public IReadOnlyList<int> PowerFaces;
    public IReadOnlyList<DiceStatType> AccuracyDiceTypes;
    public IReadOnlyList<int> AccuracyFaces;
    public (int lowMax, int mediumMax, int highMin, int maxValue) PowerTierBoundaries;
    public (int lowMax, int mediumMax, int highMin, int maxValue) AccuracyTierBoundaries;
    // Valores calculados
    public float MinDamage;
    public float MaxDamage;
    public float DamageMultiplier;
    public DiceTier MinPowerTier;
    public DiceTier MaxPowerTier;
    public TierChances PowerChances;
    public AllocationConsistency Consistency;
    public float PowerMinRollChance;
    public float PowerMaxRollChance;
    public TierChances AccuracyChances;
    public float AccuracyMinRollChance;
    public float AccuracyMaxRollChance;
    public int MissThreshold;
    public int HitThreshold;
    public int CriticalThreshold;
}