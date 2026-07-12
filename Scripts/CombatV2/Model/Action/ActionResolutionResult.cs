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

public enum DefenseOutcome
{
    None,
    Evaded,
    Parried,
    Blocked
}

public enum ActionResolutionVariation
{
    None,
    Hit,
    CriticalHit,
    PiercingHit,
    ParryBreak,
    PowerSurge,
    ArmorShatter,
    DevastatingStrike,
    FierceDefense,
    Overkill,
    Annihilation,
    Missed,
    Blocked,
    Evaded,
    Parried,
    LegendaryClash,
    IronWall
}

public enum PowerMaxSource 
{
    None,
    Attack,
    Defense
}

public class ActionResolutionResult
{
    public int Damage;
    public ActionAccuracy Accuracy;
    public ActionOutcome Outcome;
    public DefenseOutcome DefenseOutcome;
    public ActionResolutionVariation ResolutionVariation;
    public bool IgnoreAttack;
    public bool IgnoreDefense;
    public PowerMaxSource PowerMaxSource;
    public Battler FinalTarget;
    public string AttackFeedbackText;
    public string DefenseFeedbackText;
    public bool AppliesDamage => Damage > 0;
}
