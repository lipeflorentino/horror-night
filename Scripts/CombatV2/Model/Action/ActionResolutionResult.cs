public enum ActionAccuracy
{
    Missed,
    Hit,
    Critical
}

public enum ActionOutcome
{
    Missed,
    Blocked,
    Hit,
    CriticalHit,
    Parried,
    Evaded
}

public class ActionResolutionResult
{
    public int Damage;
    public ActionAccuracy Accuracy;
    public ActionOutcome Outcome;
    public Battler FinalTarget;
    public string FeedbackText;
    public string AttackPowerLogText;
    public string DefensePowerLogText;
    public string AttackAccuracyLogText;
    public string DefenseAccuracyLogText;

    public bool AppliesDamage => Damage > 0;
}
