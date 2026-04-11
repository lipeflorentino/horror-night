public class CombatTurnService
{
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

    public void StartFirstTurn(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        InitiativeResolver initResolver = new(dice);

        if (initResolver.PlayerStarts(player, enemy))
            StartPlayerTurn();
        else
            StartEnemyTurn();
    }

    public void StartPlayerTurn()
    {
        combatStateModel.SetPlayerTurn();
        turnManager.ResetDice();
        turnManager.StartTurn();
        lastEnemyAction = EnemyTurnAction.None;
    }

    public void StartEnemyTurn()
    {
        combatStateModel.SetEnemyTurn();

        int aiRoll = dice.RollD6();
        lastEnemyAction = aiRoll <= 3 ? EnemyTurnAction.Attack : EnemyTurnAction.Defend;
    }
}
