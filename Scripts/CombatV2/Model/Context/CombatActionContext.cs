public readonly struct CombatActionContext : ICombatContext
{
    public readonly Battler Actor;
    public readonly Battler Opponent;
    public readonly ActionType ActionType;

    public CombatActionContext(Battler actor, Battler opponent, ActionType actionType)
    {
        Actor = actor;
        Opponent = opponent;
        ActionType = actionType;
    }
}
