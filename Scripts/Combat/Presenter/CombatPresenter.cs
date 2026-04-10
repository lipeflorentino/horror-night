public class CombatPresenter
{
    private readonly CombatUI combatUI;
    private readonly CombatInputHandler combatInputHandler;
    private readonly CombatTurnService combatTurnService;
    private readonly TurnManager turnManager;

    public CombatPresenter(
        CombatUI combatUI,
        CombatInputHandler combatInputHandler,
        CombatTurnService combatTurnService,
        TurnManager turnManager)
    {
        this.combatUI = combatUI;
        this.combatInputHandler = combatInputHandler;
        this.combatTurnService = combatTurnService;
        this.turnManager = turnManager;
    }

    public void OnTurnStart(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        bool isPlayerTurn = combatTurnService.StartFirstTurn(player, enemy);
        combatUI.SetTurnText($"Turno do {(isPlayerTurn ? "Jogador" : "Inimigo")}");
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.AddLog("Turn started.", CombatLogStyle.Neutral);
    }

    public ActionResult OnRecharge(CombatBattlerModel player, bool boosted)
    {
        ActionResult result = combatInputHandler.HandleRecharge(player, boosted);
        PublishResult("Recharge", result);
        return result;
    }

    public ActionResult OnInvestigate()
    {
        ActionResult result = combatInputHandler.HandleInvestigate();
        PublishResult("Investigate", result);
        return result;
    }

    public ActionResult OnFlee(int dice)
    {
        ActionResult result = combatInputHandler.HandleFlee(dice);
        PublishResult("Flee", result);
        return result;
    }

    public ActionResult OnDamage(CombatBattlerModel target, int baseAttack, int defense)
    {
        ActionResult result = combatInputHandler.HandleAttack(target, baseAttack, defense);
        PublishResult("Attack", result);
        return result;
    }

    public void OnUseItem()
    {
        combatUI.AddLog("Inventory not implemented yet", CombatLogStyle.Neutral);
    }

    public void OnSkills()
    {
        combatUI.AddLog("Skills not implemented yet", CombatLogStyle.Neutral);
    }

    public void OnInfo()
    {
        combatUI.AddLog("No information available", CombatLogStyle.Neutral);
    }

    public ActionResult OnEndTurn(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.HandleEndTurn();
        PublishResult("End Turn", result);

        combatUI.SetTurnText("Turno do inimigo");
        combatTurnService.StartEnemyTurn();
        combatUI.AddLog("Enemy turn finished", CombatLogStyle.Neutral);

        combatTurnService.StartPlayerTurn(player);
        combatUI.SetTurnText("Turno do jogador");
        combatUI.UpdateHud(turnManager.availableDice);

        return result;
    }

    private void PublishResult(string action, ActionResult result)
    {
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.ShowFeedback(result.message, true);
        combatUI.AddLog($"{action}: {result.message}", result.success ? CombatLogStyle.Action : CombatLogStyle.Negative);
    }
}
