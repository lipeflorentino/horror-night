using System;
using System.Collections.Generic;

public enum DiceTier
{
    Low,
    Medium,
    High
}

public enum DiceStatType
{
    Mind,
    Heart,
    Body
}

public enum DiceRollType
{
    Power, 
    Accuracy
}

public class DiceResult
{
    public int Value;
    public DiceTier Tier;
    public int MaxValue; // Representa o teto original do dado (Base Stat)
    public int CurrentFaceValue; // Representa a face dinâmica do dado para a rolagem atual (Current Stat)
    public int MinValue;
    public DiceStatType StatType;
    public DiceRollType RollType;
    public bool IsExtra;
    
    // Agora o IsMaxRoll valida estritamente contra o MaxValue (Base Stat)
    public bool IsMaxRoll => MaxValue > 1 && Value >= GetMaxValueForStat(StatType, MaxValue);

    private static int GetMaxValueForStat(DiceStatType statType, int maxValue)
    {
        int statReferenceValue = Math.Max(1, maxValue);

        return statType switch
        {
            DiceStatType.Mind => statReferenceValue,
            DiceStatType.Heart => statReferenceValue,
            DiceStatType.Body => statReferenceValue,
            _ => statReferenceValue
        };
    }
    public List<DiceResult> SubRolls = new();

    // Construtor atualizado para receber o CurrentFaceValue
    public DiceResult(int value, DiceTier tier, int maxValue, int currentFaceValue, DiceStatType statType, DiceRollType rollType, int minValue = 1)
    {
        Value = value;
        Tier = tier;
        MaxValue = maxValue;
        CurrentFaceValue = currentFaceValue;
        MinValue = minValue;
        StatType = statType;
        RollType = rollType;
    }
}