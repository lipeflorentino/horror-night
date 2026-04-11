using System.Collections.Generic;

public class CombatPresenter
{
    private readonly CombatUI combatUI;
    private readonly CombatInputHandler combatInputHandler;
    private readonly CombatTurnService combatTurnService;
    private readonly CombatTurnResolver combatTurnResolver;
    private readonly EnemyTurnHandler enemyTurnHandler;
    private readonly TurnManager turnManager;

    private CombatBattlerModel playerModel;
    private CombatBattlerModel enemyModel;

    public CombatPresenter(
        CombatUI combatUI,
        CombatInputHandler combatInputHandler,
        CombatTurnService combatTurnService,
        CombatTurnResolver combatTurnResolver,
        EnemyTurnHandler enemyTurnHandler,
        TurnManager turnManager)
    {
        this.combatUI = combatUI;
        this.combatInputHandler = combatInputHandler;
        this.combatTurnService = combatTurnService;
        this.combatTurnResolver = combatTurnResolver;
        this.enemyTurnHandler = enemyTurnHandler;
        this.turnManager = turnManager;
    }

    public void OnTurnStart(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        playerModel = player;
        enemyModel = enemy;

        combatTurnService.StartFirstTurn(player, enemy);
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.AddLog("Turn started.", CombatLogStyle.Neutral);
    }

    public ActionResult OnRecharge(CombatBattlerModel player, bool boosted)
    {
        ActionResult result = combatInputHandler.HandleRecharge(player, boosted);
        PublishActionSelection("Recharge", result);
        return result;
    }

    public ActionResult OnInvestigate(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.HandleInvestigate(player);
        PublishActionSelection("Investigate", result);
        return result;
    }

    public ActionResult OnFlee(CombatBattlerModel player, int dice)
    {
        ActionResult result = combatInputHandler.HandleFlee(player, dice);
        PublishActionSelection("Flee", result);
        return result;
    }

    public ActionResult OnDamage(CombatBattlerModel player, CombatBattlerModel target, int baseAttack, int defense)
    {
        ActionResult result = combatInputHandler.HandleAttack(player, target, baseAttack, defense);
        PublishActionSelection("Attack", result);
        return result;
    }

    public ActionResult OnEndTurn(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.HandleEndTurn(player);
        PublishActionSelection("End Turn", result);

        if (!result.success)
        {
            return result;
        }

        combatUI.AddLog("Turno do inimigo", CombatLogStyle.Info);

        IReadOnlyList<CombatActionData> playerActions = turnManager.actionQueue.GetAll();
        combatTurnResolver.ResolvePlayerTurn(playerModel, enemyModel, playerActions);
        PublishResolutionLogs(combatTurnResolver.lastResolutionLog);

        IReadOnlyList<string> enemyLogs = enemyTurnHandler.ResolveEnemyTurn(enemyModel, playerModel);
        PublishResolutionLogs(enemyLogs);

        turnManager.actionQueue.Clear();
        combatTurnService.StartPlayerTurn();
        combatUI.UpdateHud(turnManager.availableDice);

        return result;
    }

    private void PublishActionSelection(string action, ActionResult result)
    {
        if (result.success)
        {
            combatUI.NotifyActionQueued(action);
            combatUI.UpdateHud(turnManager.availableDice);
        }

        combatUI.ShowFeedback(result.message, true);
        combatUI.AddLog($"{action}: {result.message}", result.success ? CombatLogStyle.Action : CombatLogStyle.Negative);
    }

    private void PublishResolutionLogs(IReadOnlyList<string> logs)
    {
        for (int i = 0; i < logs.Count; i++)
        {
            combatUI.AddLog(logs[i], CombatLogStyle.Info);
        }
    }
}
