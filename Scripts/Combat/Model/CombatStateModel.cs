using System;

public class CombatStateModel
{
    public CombatFlowState currentState;
    public CombatOutcome? outcome;

    public event Action<CombatFlowState> OnStateChanged;
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;
    public event Action<CombatOutcome> OnCombatEnded;

    public void SetPlayerTurn()
    {
        if (currentState == CombatFlowState.PlayerTurn)
            return;

        currentState = CombatFlowState.PlayerTurn;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
        OnPlayerTurnStart?.Invoke();
    }

    public void SetEnemyTurn()
    {
        if (currentState == CombatFlowState.EnemyTurn)
            return;

        currentState = CombatFlowState.EnemyTurn;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
        OnEnemyTurnStart?.Invoke();
    }

    public void EndCombat(CombatOutcome finalOutcome)
    {
        currentState = CombatFlowState.Finished;
        this.outcome = finalOutcome;
        OnStateChanged?.Invoke(currentState);
        OnCombatEnded?.Invoke(finalOutcome);
    }

    public bool IsPlayerTurn()
    {
        return currentState == CombatFlowState.PlayerTurn;
    }

    public bool IsEnemyTurn()
    {
        return currentState == CombatFlowState.EnemyTurn;
    }

    public bool IsCombatFinished()
    {
        return currentState == CombatFlowState.Finished;
    }
}
