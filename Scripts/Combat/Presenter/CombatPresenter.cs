using System.Collections.Generic;

public class CombatPresenter
{
    private readonly CombatUI combatUI;
    private readonly CombatInputHandler combatInputHandler;
    private readonly CombatTurnService combatTurnService;
    private readonly CombatTurnResolver combatTurnResolver;
    private readonly TurnManager turnManager;
    private InputView inputView;
    private HudView hudView;

    public CombatPresenter(
        CombatUI combatUI,
        CombatInputHandler combatInputHandler,
        CombatTurnService combatTurnService,
        CombatTurnResolver combatTurnResolver,
        TurnManager turnManager,
        InputView inputView = null,
        HudView hudView = null)
    {
        this.combatUI = combatUI;
        this.combatInputHandler = combatInputHandler;
        this.combatTurnService = combatTurnService;
        this.combatTurnResolver = combatTurnResolver;
        this.turnManager = turnManager;
        this.inputView = inputView;
        this.hudView = hudView;
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

    public ActionResult OnSubtractAttackDice()
    {
        ActionResult result = combatInputHandler.TryRemoveAttackDice(1);
        PublishResult("Remove Attack Dice", result);
        return result;
    }

    public ActionResult OnSubtractInvestigateDice()
    {
        ActionResult result = combatInputHandler.TryRemoveInvestigateDice(1);
        PublishResult("Remove Investigate Dice", result);
        return result;
    }

    public ActionResult OnSubtractDefendDice()
    {
        ActionResult result = combatInputHandler.TryRemoveDefendDice(1);
        PublishResult("Remove Defend Dice", result);
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

    /// <summary>
    /// Valida se o turno pode ser encerrado. Não resolve ações.
    /// Resolução acontece em CombatManager.ResolvePlayerAction().
    /// </summary>
    public ActionResult OnEndTurn(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        ActionResult endTurnResult = combatInputHandler.HandleEndTurn();
        if (!endTurnResult.success)
        {
            PublishResult("End Turn", endTurnResult);
            return endTurnResult;
        }

        PublishResult("End Turn", endTurnResult);
        return endTurnResult;
    }

    /// <summary>
    /// Retorna as ações enfileiradas do turno atual do jogador.
    /// </summary>
    public IReadOnlyList<ActionInstance> GetQueuedPlayerActions()
    {
        return turnManager.actionQueue?.GetAll() ?? new List<ActionInstance>();
    }

    /// <summary>
    /// Limpa a fila de ações após resolução do turno.
    /// </summary>
    public void ClearActionQueue()
    {
        turnManager.actionQueue?.Clear();
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

    /// <summary>
    /// Atualiza a UI para mostrar que é o turno do jogador.
    /// Ativa botões e reseta estado de ação primária.
    /// </summary>
    public void OnPlayerTurnUIUpdate()
    {
        if (inputView != null)
        {
            inputView.ShowPlayerTurnUI();
            inputView.ResetPrimaryActions();
        }
    }

    /// <summary>
    /// Atualiza a UI para mostrar que é o turno do inimigo.
    /// Desativa botões do jogador.
    /// </summary>
    public void OnEnemyTurnUIUpdate()
    {
        if (inputView != null)
        {
            inputView.ShowEnemyTurnUI();
        }
    }

    /// <summary>
    /// Desabilita os botões de ação primária após um ser selecionado.
    /// </summary>
    public void OnPrimaryActionSelected()
    {
        if (inputView != null)
        {
            inputView.DisablePrimaryActions();
        }
    }

    /// <summary>
    /// Atualiza os contadores de dados alocados na UI.
    /// </summary>
    public void UpdateDiceCounters()
    {
        if (inputView != null)
        {
            inputView.UpdateDiceCounters();
        }
    }

    /// <summary>
    /// Define a referência ao InputView (pode ser definida após construção).
    /// </summary>
    public void SetInputView(InputView view)
    {
        inputView = view;
    }

    /// <summary>
    /// Define a referência ao HudView (pode ser definida após construção).
    /// </summary>
    public void SetHudView(HudView view)
    {
        hudView = view;
    }

    /// <summary>
    /// Atualiza o display de HP do jogador na HUD.
    /// </summary>
    public void UpdatePlayerHPDisplay(int currentHP, int maxHP)
    {
        if (hudView != null)
            hudView.UpdatePlayerHP(currentHP, maxHP);
    }

    /// <summary>
    /// Atualiza o display de recursos (Heart, Body, Mind) do jogador.
    /// </summary>
    public void UpdatePlayerResourcesDisplay(int heart, int body, int mind)
    {
        if (hudView != null)
            hudView.UpdatePlayerResources(heart, body, mind);
    }

    /// <summary>
    /// Atualiza o display de HP do inimigo na HUD.
    /// </summary>
    public void UpdateEnemyHPDisplay(int currentHP, int maxHP)
    {
        if (hudView != null)
            hudView.UpdateEnemyHP(currentHP, maxHP);
    }

    /// <summary>
    /// Mostra feedback de dano recebido com animação.
    /// </summary>
    public void ShowDamagePopup(int damageAmount)
    {
        if (hudView != null)
            hudView.ShowDamagePopup(damageAmount);
    }

    /// <summary>
    /// Mostra feedback de cura com animação.
    /// </summary>
    public void ShowHealingPopup(int healAmount)
    {
        if (hudView != null)
            hudView.ShowHealingPopup(healAmount);
    }

    /// <summary>
    /// Anima um shake de câmera ao receber dano.
    /// </summary>
    public void PlayDamageShake()
    {
        if (hudView != null)
            hudView.PlayDamageShake();
    }

    /// <summary>
    /// Mostra feedback de ação executada.
    /// </summary>
    public void ShowActionFeedback(string actionName)
    {
        if (hudView != null)
            hudView.ShowActionFeedback(actionName);
    }

    /// <summary>
    /// Resolve os resultados das ações de um turno e publica no log.
    /// </summary>
    public void PublishPlayerActionResults(List<ActionResult> results)
    {
        PublishResolvedActions("Player", results);
    }
}
