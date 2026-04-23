public enum ActionAccuracy
{
    Missed,
    Hit
}

public enum HitQuality
{
    Normal,
    Critical
}

public enum ActionOutcome
{
    Missed,
    Blocked,
    Hit,
    CriticalHit
}

public class ActionResolutionResult
{
    public int Damage;
    public ActionAccuracy Accuracy;
    public HitQuality HitQuality;
    public ActionOutcome Outcome;
    public string FeedbackText;

    public bool AppliesDamage => Damage > 0;
}
