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
    DamagePercent
}

public enum PerkConditionKey
{
    Always,
    RollValueEquals,
    RollTierEquals
}

public enum PerkOperation
{
    Add,
    Multiply,
    Override
}
