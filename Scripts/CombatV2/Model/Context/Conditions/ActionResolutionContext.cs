/// <summary>
/// Context para avaliação de resolução de ação.
/// </summary>
public class ActionResolutionContext : ICombatContext, IPerkConditionContext
{
    public Battler Actor;
    public Battler Opponent;
    public ActionType ActionType;
    public ActionOutcome Outcome;
}
