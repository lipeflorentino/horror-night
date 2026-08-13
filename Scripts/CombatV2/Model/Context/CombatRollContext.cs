public readonly struct CombatRollContext : ICombatContext
{
    public readonly Battler Actor;
    public readonly Battler Opponent;
    public readonly ActionType ActionType;
    public readonly DiceRollType RollType;
    public readonly DiceStatType StatType;
    public readonly int ActorLevel;
    public readonly int OpponentLevel;
    public readonly int MaxValue;
    public readonly int AllocatedDiceCount;

    public CombatRollContext(Battler actor, Battler opponent, ActionType actionType, DiceRollType rollType, DiceStatType statType, int actorLevel, int opponentLevel, int maxValue, int allocatedDiceCount = 1)
    {
        Actor = actor;
        Opponent = opponent;
        ActionType = actionType;
        RollType = rollType;
        StatType = statType;
        ActorLevel = actorLevel;
        OpponentLevel = opponentLevel;
        MaxValue = maxValue;
        AllocatedDiceCount = allocatedDiceCount;
    }

    public CombatActionContext ToActionContext()
    {
        return new CombatActionContext(Actor, Opponent, ActionType);
    }

    public CombatRollContext WithRoll(DiceRollType rollType, DiceStatType statType, int maxValue)
    {
        return new CombatRollContext(Actor, Opponent, ActionType, rollType, statType, ActorLevel, OpponentLevel, maxValue, AllocatedDiceCount);
    }
}
