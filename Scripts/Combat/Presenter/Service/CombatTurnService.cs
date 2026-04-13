using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTurnService
{
    private static readonly WaitForSeconds _waitForSeconds1 = new(1f);
    private static readonly WaitForSeconds _waitForSeconds2 = new(2f);

    private readonly CombatStateModel combatStateModel;
    private readonly TurnManager turnManager;
    private readonly DiceService dice;

    public EnemyTurnAction lastEnemyAction;
    
    /// <summary>
    /// Event disparado quando a ação do inimigo é determinada.
    /// Dispara durante o delay, permitindo UI feedback.
    /// </summary>
    public event Action<EnemyTurnAction> OnEnemyActionDetermined;

    public CombatTurnService(CombatStateModel combatStateModel, TurnManager turnManager, DiceService dice)
    {
        this.combatStateModel = combatStateModel;
        this.turnManager = turnManager;
        this.dice = dice;
        lastEnemyAction = EnemyTurnAction.None;
    }

    public bool StartFirstTurn(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        InitiativeResolver initResolver = new(dice);

        bool isPlayerTurn = initResolver.PlayerStarts(player, enemy);

        if (isPlayerTurn)
        {
            StartPlayerTurn(player);
        }
        else
        {
            combatStateModel.SetEnemyTurn();
        }

        return isPlayerTurn;
    }

    public void StartPlayerTurn(CombatBattlerModel player)
    {
        combatStateModel.SetPlayerTurn();
        player.RecoverResources(1);
        turnManager.StartTurn(3, player.heart, player.body, player.mind);
        lastEnemyAction = EnemyTurnAction.None;
    }

    public List<ActionInstance> GenerateEnemyActions()
    {
        ActionDefinitionFactory factory = new();
        int roll = dice.RollD6();
        ActionDefinition definition = roll <= 3 ? factory.CreateDefend() : factory.CreateAttack();

        return new List<ActionInstance>
        {
            new ActionInstance
            {
                definition = definition,
                allocatedDice = 0,
                allocatedHeart = 0,
                allocatedBody = 0,
                allocatedMind = 0
            }
        };
    }

    /// <summary>
    /// Executa o turno do inimigo com delay e feedback visual.
    /// Sequência: aguarda 0.5s, dispara evento de ação, aguarda 1.5s antes de resolver.
    /// </summary>
    public IEnumerator ExecuteEnemyTurn(CombatBattlerModel enemy)
    {
        combatStateModel.SetEnemyTurn();
        
        // Pequeno delay antes de revelar ação
        yield return new WaitForSeconds(0.5f);

        // Gerar ação do inimigo
        var enemyActions = GenerateEnemyActions();
        if (enemyActions.Count > 0)
        {
            lastEnemyAction = enemyActions[0].definition.type == PlayerActionType.Attack 
                ? EnemyTurnAction.Attack 
                : EnemyTurnAction.Defend;
            
            // Disparar evento para feedback visual
            OnEnemyActionDetermined?.Invoke(lastEnemyAction);
        }

        // Aguardar antes de resolver a ação
        yield return new WaitForSeconds(1.5f);
    }

    /// <summary>
    /// Aguarda o turno do jogador (aguarda input do usuário).
    /// Este método não faz nada além de preparar o estado - o gameplay real é controlado por eventos.
    /// </summary>
    public IEnumerator ExecutePlayerTurn(CombatBattlerModel player)
    {
        StartPlayerTurn(player);
        
        // Aguardar indefinidamente até que o jogador termine seu turno
        // (Isso será resolvido via callback de OnEndTurn)
        yield return new WaitUntil(() => combatStateModel.IsEnemyTurn() || combatStateModel.IsCombatFinished());
    }
}
