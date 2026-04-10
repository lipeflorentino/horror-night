using System.Collections;
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
        turnManager.StartTurn(3);
        player.RecoverResources(1);
        lastEnemyAction = EnemyTurnAction.None;
    }

    public IEnumerator StartEnemyTurn()
    {
        combatStateModel.SetEnemyTurn();
        yield return _waitForSeconds1;
    }
}
