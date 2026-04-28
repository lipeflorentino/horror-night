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
    public bool IsMaxRoll => Value >= MaxValue;

    public DiceResult(int value, DiceTier tier, int maxValue)
    {
        Value = value;
        Tier = tier;
        MaxValue = maxValue;
    }
}
