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
    public int MinValue;
    public DiceStatType StatType;
    public DiceRollType RollType;
    public bool IsMaxRoll => Value >= MaxValue && MaxValue > 1;

    public DiceResult(int value, DiceTier tier, int maxValue, DiceStatType statType, DiceRollType rollType, int minValue = 1)
    {
        Value = value;
        Tier = tier;
        MaxValue = maxValue;
        MinValue = minValue;
        StatType = statType;
        RollType = rollType;
    }
}
