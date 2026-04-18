public enum DiceTier
{
    Low,
    Medium,
    High
}

public class DiceResult
{
    public int Value;
    public DiceTier Tier;

    public DiceResult(int value, DiceTier tier)
    {
        Value = value;
        Tier = tier;
    }
}