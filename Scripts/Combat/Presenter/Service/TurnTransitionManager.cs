using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gerencia a transição entre turnos do jogador e inimigo.
/// Controla a sequência completa de um turno (setup -> execução -> resolução -> transição).
/// </summary>
public class TurnTransitionManager
{
    private readonly CombatStateModel combatStateModel;
    private readonly TurnManager turnManager;
    private readonly CombatTurnService combatTurnService;

    public event Action<CombatFlowState> OnTurnTransitioned;
    public event Action OnPlayerTurnReady;
    public event Action OnEnemyTurnStarting;

    public TurnTransitionManager(
        CombatStateModel combatStateModel,
        TurnManager turnManager,
        CombatTurnService combatTurnService)
    {
        this.combatStateModel = combatStateModel;
        this.turnManager = turnManager;
        this.combatTurnService = combatTurnService;
    }

    /// <summary>
    /// Transiciona para o próximo turno (Player → Enemy ou Enemy → Player).
    /// Reseta os dados alocados e recursos conforme necessário.
    /// </summary>
    public void TransitionToNextTurn(bool toPlayerTurn)
    {
        if (toPlayerTurn)
        {
            // TransitionToPlayerTurn será chamado via corrotina
        }
        else
        {
            // TransitionToEnemyTurn será chamado via corrotina
        }
    }

    /// <summary>
    /// Corrotina que executa um ciclo completo de turno do jogador.
    /// 1. Inicia turno do jogador
    /// 2. Aguarda input do jogador
    /// 3. Permite transição automática
    /// </summary>
    public IEnumerator PlayPlayerTurn(CombatBattlerModel player)
    {
        ResetTurnData();
        combatStateModel.SetPlayerTurn();
        OnPlayerTurnReady?.Invoke();
        
        yield return new WaitUntil(() => 
            combatStateModel.IsEnemyTurn() || combatStateModel.IsCombatFinished());
    }

    public IEnumerator PlayEnemyTurn(CombatBattlerModel enemy)
    {
        OnEnemyTurnStarting?.Invoke();
        yield return combatTurnService.ExecuteEnemyTurn(enemy);        
        OnTurnTransitioned?.Invoke(CombatFlowState.PlayerTurn);
    }

    private void ResetTurnData()
    {
        turnManager.ResetDice();
    }

    public bool CanTransitionTurn()
    {
        if (combatStateModel.IsCombatFinished())
            return false;

        return true;
    }

    public void ForceTransitionToNextTurn()
    {
        if (combatStateModel.IsPlayerTurn())
        {
            OnTurnTransitioned?.Invoke(CombatFlowState.EnemyTurn);
        }
        else if (combatStateModel.IsEnemyTurn())
        {
            OnTurnTransitioned?.Invoke(CombatFlowState.PlayerTurn);
        }
    }
}
