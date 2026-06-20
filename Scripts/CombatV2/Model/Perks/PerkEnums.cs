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
}

public enum PerkModifierTarget
{
    MinRollPercent,
    MaxRollPercent,
    MinRollValue,
    MaxRollValue,
    ExtraDice,
    PowerMultiplier,
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
