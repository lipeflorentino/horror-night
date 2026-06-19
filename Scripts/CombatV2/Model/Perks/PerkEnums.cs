public enum PerkTrigger
{
    BeforeRoll,
    AfterAccuracyRoll,
    PowerMultiplier,
    AfterResolve,
    OnActionResolved  
}

public enum PerkModifierTarget
{
    MinRollPercent,
    ExtraDice,
    PowerMultiplier,
    DamagePercent,
    MomentumPoints,
    Focus,
    Strength,
}

public enum PerkConditionKey
{
    Always,
    RollValueEquals,
    RollTierEquals,
    RollSumEquals,
    RollSumEqualsAttackersRollSum,
    BlockedAttack
}

public enum PerkOperation
{
    Add,
    Multiply,
    Override
}
