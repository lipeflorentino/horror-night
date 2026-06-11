public enum PerkScope
{
    Dice,
    Battler,
    Action
}

public enum PerkTrigger
{
    BeforeRoll,
    PowerMultiplier,
    AfterResolve
}

public enum PerkModifierTarget
{
    MinRollPercent,
    ExtraDice,
    PowerMultiplier,
    DamagePercent,
    MomentumPoints
}

public enum PerkConditionKey
{
    Always,
    RollValueEquals,
    RollTierEquals,
    RollSumEquals,
    RollSumEqualsAttackersRollSum
}

public enum PerkOperation
{
    Add,
    Multiply,
    Override
}
