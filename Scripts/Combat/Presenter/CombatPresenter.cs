using System.Collections.Generic;

public class CombatPresenter
{
    private readonly CombatUI combatUI;
    private readonly CombatInputHandler combatInputHandler;
    private readonly CombatTurnService combatTurnService;
    private readonly CombatTurnResolver combatTurnResolver;
    private readonly TurnManager turnManager;

    public CombatPresenter(
        CombatUI combatUI,
        CombatInputHandler combatInputHandler,
        CombatTurnService combatTurnService,
        CombatTurnResolver combatTurnResolver,
        TurnManager turnManager)
    {
        this.combatUI = combatUI;
        this.combatInputHandler = combatInputHandler;
        this.combatTurnService = combatTurnService;
        this.combatTurnResolver = combatTurnResolver;
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
        PublishResult("Defend", result);
        return result;
    }

    public ActionResult OnAttack(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.QueueAttack(player, 1);
        PublishResult("Attack", result);
        return result;
    }

    public ActionResult OnInvestigate(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.QueueInvestigate(player, 1);
        PublishResult("Investigate", result);
        return result;
    }

    public ActionResult OnDefend(CombatBattlerModel player)
    {
        ActionResult result = combatInputHandler.QueueDefend(player, 1);
        PublishResult("Defend", result);
        return result;
    }

    public ActionResult OnAddInvestigateDice(CombatBattlerModel player, int diceAmount)
    {
        ActionResult result = combatInputHandler.QueueInvestigate(player, diceAmount);
        PublishResult("Investigate", result);
        return result;
    }

    public ActionResult OnFlee(CombatBattlerModel player, int dice)
    {
        ActionResult result = combatInputHandler.HandleFlee(player, dice);
        PublishResult("Defend", result);
        return result;
    }

    public ActionResult OnAddAttackDice(CombatBattlerModel player, int diceAmount)
    {
        ActionResult result = combatInputHandler.QueueAttack(player, diceAmount);
        PublishResult("Attack", result);
        return result;
    }

    public ActionResult OnAddDefendDice(CombatBattlerModel player, int diceAmount)
    {
        ActionResult result = combatInputHandler.QueueDefend(player, diceAmount);
        PublishResult("Defend", result);
        return result;
    }

    public ActionResult OnUseItem()
    {
        combatUI.RequestInventorySelection();
        return new ActionResult { success = true, message = "Inventory opened." };
    }

    public ActionResult OnItemSelected(CombatBattlerModel player, int itemId)
    {
        ActionResult result = combatInputHandler.QueueUseItemSelection(player, itemId);
        PublishResult("UseItem", result);
        return result;
    }

    public ActionResult OnSkills()
    {
        combatUI.RequestSkillSelection();
        return new ActionResult { success = true, message = "Skill panel opened." };
    }

    public ActionResult OnSkillSelected(CombatBattlerModel player, int skillId)
    {
        ActionResult result = combatInputHandler.QueueUseSkillSelection(player, skillId);
        PublishResult("UseSkill", result);
        return result;
    }

    public ActionResult OnUseSkill(CombatBattlerModel player, int skillId)
    {
        ActionResult result = combatInputHandler.QueueUseSkillSelection(player, skillId);
        PublishResult("UseSkill", result);
        return result;
    }

    public void OnInfo()
    {
        combatUI.AddLog("No information available", CombatLogStyle.Neutral);
    }

    public ActionResult OnEndTurn(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        ActionResult endTurnResult = combatInputHandler.HandleEndTurn();
        if (!endTurnResult.success)
        {
            PublishResult("End Turn", endTurnResult);
            return endTurnResult;
        }

        ResolvePlayerTurn(player, enemy);
        ResolveEnemyTurn(enemy, player);

        turnManager.actionQueue?.Clear();
        combatTurnService.StartPlayerTurn(player);
        combatUI.SetTurnText("Turno do jogador");
        combatUI.UpdateHud(turnManager.availableDice);

        return endTurnResult;
    }

    private void ResolvePlayerTurn(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        IReadOnlyList<ActionInstance> playerActions = turnManager.actionQueue?.GetAll();
        List<ActionResult> playerResults = combatTurnResolver.ResolvePlayerTurn(player, enemy, playerActions);
        PublishResolvedActions("Player", playerResults);
    }

    private void ResolveEnemyTurn(CombatBattlerModel enemy, CombatBattlerModel player)
    {
        combatUI.SetTurnText("Turno do inimigo");
        List<ActionInstance> enemyActions = combatTurnService.GenerateEnemyActions();
        List<ActionResult> enemyResults = combatTurnResolver.ResolvePlayerTurn(enemy, player, enemyActions);
        PublishResolvedActions("Enemy", enemyResults);
    }

    private void PublishResolvedActions(string owner, List<ActionResult> results)
    {
        if (results == null)
        {
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            ActionResult result = results[i];
            CombatLogStyle style = result.success ? CombatLogStyle.Action : CombatLogStyle.Negative;
            combatUI.AddLog($"{owner} action {i + 1}: {result.message}", style);
        }
    }

    private void PublishResult(string action, ActionResult result)
    {
        if (result.success)
        {
            combatUI.NotifyActionQueued($"{action} queued.");
            combatUI.UpdateHud(turnManager.availableDice);
        }

        combatUI.ShowFeedback(result.message, true);
        combatUI.AddLog($"{action}: {result.message}", result.success ? CombatLogStyle.Action : CombatLogStyle.Negative);
    }
}
