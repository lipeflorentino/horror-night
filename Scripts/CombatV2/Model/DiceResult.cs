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
    public int MaxValue;
    public DiceStatType StatType;
    public DiceRollType RollType;
    public bool IsMaxRoll => Value >= MaxValue;

    public DiceResult(int value, DiceTier tier, int maxValue, DiceStatType statType, DiceRollType rollType)
    {
        Value = value;
        Tier = tier;
        MaxValue = maxValue;
        StatType = statType;
        RollType = rollType;
    }
}
