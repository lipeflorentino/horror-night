using System;

public class CombatStateModel
{
    public CombatFlowState currentState;
    public CombatOutcome? outcome;
    public CombatRole playerRole;
    public CombatRole enemyRole;

    public event Action<CombatFlowState> OnStateChanged;
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;
    public event Action<CombatOutcome> OnCombatEnded;

    public CombatStateModel()
    {
        playerRole = CombatRole.Attacker;
        enemyRole = CombatRole.Defender;
    }

    public void SetPlayerDecidingAttack()
    {
        if (currentState == CombatFlowState.PlayerDecidingAttack)
            return;

        currentState = CombatFlowState.PlayerDecidingAttack;
        playerRole = CombatRole.Attacker;
        enemyRole = CombatRole.Defender;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
    }

    public void SetEnemyDecidingDefense()
    {
        if (currentState == CombatFlowState.EnemyDecidingDefense)
            return;

        currentState = CombatFlowState.EnemyDecidingDefense;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
    }

    public void SetResolvingRound()
    {
        if (currentState == CombatFlowState.ResolvingRound)
            return;

        currentState = CombatFlowState.ResolvingRound;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
    }

    public void SetPlayerDecidingDefense()
    {
        if (currentState == CombatFlowState.PlayerDecidingDefense)
            return;

        currentState = CombatFlowState.PlayerDecidingDefense;
        playerRole = CombatRole.Defender;
        enemyRole = CombatRole.Attacker;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
    }

    public void SetEnemyDecidingAttack()
    {
        if (currentState == CombatFlowState.EnemyDecidingAttack)
            return;

        currentState = CombatFlowState.EnemyDecidingAttack;
        outcome = null;
        OnStateChanged?.Invoke(currentState);
    }

    public void SetPlayerTurn()
    {
        SetPlayerDecidingAttack();
        OnPlayerTurnStart?.Invoke();
    }

    public void SetEnemyTurn()
    {
        SetEnemyDecidingDefense();
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
        return currentState == CombatFlowState.PlayerDecidingAttack || 
               currentState == CombatFlowState.PlayerDecidingDefense;
    }

    public bool IsEnemyTurn()
    {
        return currentState == CombatFlowState.EnemyDecidingDefense || 
               currentState == CombatFlowState.EnemyDecidingAttack;
    }

    public bool IsCombatFinished()
    {
        return currentState == CombatFlowState.Finished;
    }

    public bool IsPlayerAttacking()
    {
        return playerRole == CombatRole.Attacker;
    }

    public bool IsEnemyAttacking()
    {
        return enemyRole == CombatRole.Attacker;
    }

    public bool IsPlayerDefending()
    {
        return currentState == CombatFlowState.PlayerDecidingDefense;
    }

    public bool IsPlayerAttacking_Phase()
    {
        return currentState == CombatFlowState.PlayerDecidingAttack;
    }
}
