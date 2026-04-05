public class CombatStateModel
{
    public CombatFlowState currentState;
    public CombatOutcome? outcome;

    public void SetPlayerTurn()
    {
        currentState = CombatFlowState.PlayerTurn;
        outcome = null;
    }

    public void SetEnemyTurn()
    {
        currentState = CombatFlowState.EnemyTurn;
        outcome = null;
    }

    public void EndCombat(CombatOutcome outcome)
    {
        currentState = CombatFlowState.Finished;
        this.outcome = outcome;
    }
}
