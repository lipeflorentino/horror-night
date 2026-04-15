using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTurnService
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);

    private static readonly WaitForSeconds _waitForSeconds1 = new(1f);
    private static readonly WaitForSeconds _waitForSeconds2 = new(2f);

    private readonly CombatStateModel combatStateModel;
    private readonly TurnManager turnManager;
    private readonly DiceService dice;

    public EnemyTurnAction lastEnemyAction;
    public ActionInstance lastEnemyActionInstance;
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

    public List<ActionInstance> GenerateEnemyActionsForDefense(CombatBattlerModel enemy)
    {
        ActionDefinitionFactory factory = new();
        int roll = dice.RollD6();
        ActionDefinition definition = roll <= 3 ? factory.CreateDefend() : factory.CreateDefend();

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

    public List<ActionInstance> GenerateEnemyActionsForAttack(CombatBattlerModel enemy)
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

    public List<ActionInstance> GenerateEnemyActions()
    {
        return GenerateEnemyActionsForAttack(null);
    }
    
    public IEnumerator ExecuteEnemyTurn(CombatBattlerModel enemy)
    {
        combatStateModel.SetEnemyTurn();
        
        yield return _waitForSeconds0_5;

        var enemyActions = GenerateEnemyActions();
        if (enemyActions.Count > 0)
        {
            lastEnemyAction = enemyActions[0].definition.type == PlayerActionType.Attack 
                ? EnemyTurnAction.Attack 
                : EnemyTurnAction.Defend;
            
            OnEnemyActionDetermined?.Invoke(lastEnemyAction);
        }

        yield return _waitForSeconds2;
    }

    public IEnumerator ExecutePlayerTurn(CombatBattlerModel player)
    {
        StartPlayerTurn(player);
        
        yield return new WaitUntil(() => combatStateModel.IsEnemyTurn() || combatStateModel.IsCombatFinished());
    }
}
