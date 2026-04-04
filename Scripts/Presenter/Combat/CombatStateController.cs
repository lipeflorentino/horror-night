using System;

public enum CombatFlowState
{
    Idle,
    Starting,
    PlayerTurn,
    EnemyTurn,
    Resolving,
    Victory,
    Defeat,
    Finished
}

public enum CombatOutcome
{
    None,
    Victory,
    Defeat,
    Fled
}

public class CombatStateController
{
    public CombatFlowState CurrentState { get; private set; } = CombatFlowState.Idle;
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.None;

    public event Action<CombatFlowState> OnStateChanged;
    public event Action<CombatOutcome> OnCombatEnded;

    public void BeginCombat()
    {
        Outcome = CombatOutcome.None;
        SetState(CombatFlowState.Starting);
    }

    public void SetPlayerTurn() => SetState(CombatFlowState.PlayerTurn);
    public void SetEnemyTurn() => SetState(CombatFlowState.EnemyTurn);
    public void SetResolving() => SetState(CombatFlowState.Resolving);

    public void EndCombat(CombatOutcome outcome)
    {
        if (CurrentState == CombatFlowState.Finished)
            return;

        Outcome = outcome;

        switch (outcome)
        {
            case CombatOutcome.Victory:
                SetState(CombatFlowState.Victory);
                break;
            case CombatOutcome.Defeat:
                SetState(CombatFlowState.Defeat);
                break;
            default:
                SetState(CombatFlowState.Finished);
                break;
        }

        OnCombatEnded?.Invoke(outcome);
    }

    public void SetFinished()
    {
        SetState(CombatFlowState.Finished);
    }

    private void SetState(CombatFlowState state)
    {
        CurrentState = state;
        OnStateChanged?.Invoke(CurrentState);
    }
}
