public enum PerkRole
{
    OwnerAsActor,
    OwnerAsOpponent,
    OwnerAsAttacker,
    OwnerAsDefender,
    OwnerAsTarget
}

public enum PerkStackMode
{
    RefreshDuration,
    AddStack,
    Replace
}

public enum PerkTrigger
{
    BeforeRoll,
    AfterRoll,
    AfterAccuracyRoll,
    PowerMultiplier,
    AfterResolve,
    OnActionResolved,
    OnTurnStart,
    OnTurnEnd,
    OnTrickCast,  
    OnInitiativeResolve,
    OnCombatEnd,
    OnCombatVictory,
    OnManualActivation,
}

public enum PerkModifierTarget
{
    MinRollPercent,
    LowRollThresholdPercent,
    MaxRollPercent,
    MinRollValue,
    MaxRollValue,
    ExtraDice,
    PowerMultiplier,
    AttackPower,
    DefensePower,
    DamagePercent,
    MomentumPoints,
    Focus,
    Strength,
    Initiative,
    DefensePercent,
    AttackPercent,
    Defense,
    Attack,
    Mind,
    Heart,
    Body,
    Accuracy,
    Agility,
    PowerDicesCount,
    AccuracyDicesCount,
    TrickCharges,
}

public enum PerkConditionKey
{
    Always,
    RollValueEquals,
    RollValueGreaterThan,
    RollValueLessThan,
    RollTierEquals,
    RollSumEquals,
    RollSumGreaterThan,
    RollSumLessThan,
    RollSumEqualsAttackersRollSum,
    BlockedAttack,
    EvadedAttack,
    PerriedAttack,
    ResolutionVariationEquals,
    CriticalHit,
    MissedAttack,
    HitAttack,
    DamageDealt,
    DamageTaken,
    ExtraDice,
    CombatEnd,
    CombatVictory,
    IncomingDamageGreaterThan,

}

public enum PerkOperation
{
    Add,
    Multiply,
    Override
}
