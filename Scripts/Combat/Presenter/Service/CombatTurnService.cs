using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTurnService
{
    private static readonly WaitForSeconds _waitForSeconds1 = new(1f);

    private readonly CombatStateModel combatStateModel;
    private readonly TurnManager turnManager;
    private readonly DiceService dice;

    public EnemyTurnAction lastEnemyAction;

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
            StartPlayerTurn(player);
        else
            StartEnemyTurn();

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

    public IEnumerator StartEnemyTurn()
    {
        combatStateModel.SetEnemyTurn();
        yield return _waitForSeconds1;
    }
}
