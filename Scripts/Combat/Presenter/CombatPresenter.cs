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
        combatTurnService.StartFirstTurn(player, enemy);
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

    public ActionResult OnEndTurn()
    {
        ActionResult result = combatInputHandler.HandleEndTurn();
        PublishResult("End Turn", result);
        return result;
    }

    private void PublishResult(string action, ActionResult result)
    {
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.ShowFeedback(result.message, true);
        combatUI.AddLog($"{action}: {result.message}", result.success ? CombatLogStyle.Action : CombatLogStyle.Negative);
    }
}
