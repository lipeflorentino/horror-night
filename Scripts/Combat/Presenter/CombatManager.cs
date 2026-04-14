using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    private CombatUI combatUI;
    private CombatBattlerModel playerModel;
    private CombatBattlerModel enemyModel;
    private PlayerStatusSnapshot basePlayerSnapshot;
    private CombatStateModel combatStateModel;
    private TurnManager turnManager;
    private DiceService diceService;
    private CombatTurnService combatTurnService;
    private TurnTransitionManager turnTransitionManager;
    private ActionResolverService actionResolverService;
    private CombatResolutionService combatResolutionService;
    private CombatTurnResolver combatTurnResolver;
    private CombatInputHandler combatInputHandler;
    private CombatPresenter combatPresenter;
    private CombatEndService combatEndService;
    private CombatModelFactory combatModelFactory;
    private InputView inputView;
    private HudView hudView;

    private Coroutine combatLoopCoroutine;

    private void Awake()
    {
        InitializeCombat();
    }

    public void InitializeCombat()
    {
        combatModelFactory = new CombatModelFactory();

        CombatUIViewBinder binder = FindObjectOfType<CombatUIViewBinder>();
        CombatSessionData sessionData = CombatSessionStore.Consume();

        if (sessionData == null || sessionData.enemyInstance == null)
        {
            Debug.LogWarning("Combat session data was not found. CombatManager requires CombatSessionStore data to initialize.");
            return;
        }

        playerModel = combatModelFactory.CreatePlayer(sessionData.playerSnapshot);
        enemyModel = combatModelFactory.CreateEnemy(sessionData.enemyInstance);
        basePlayerSnapshot = sessionData.playerSnapshot;
        
        combatUI = new CombatUI();
        binder.Bind(combatUI);

        combatStateModel = new CombatStateModel();
        turnManager = new TurnManager();
        diceService = new DiceService();
        combatTurnService = new CombatTurnService(combatStateModel, turnManager, diceService);
        turnTransitionManager = new TurnTransitionManager(combatStateModel, turnManager, combatTurnService);
        actionResolverService = new ActionResolverService(diceService);
        combatResolutionService = new CombatResolutionService();
        combatTurnResolver = new CombatTurnResolver(combatResolutionService, diceService);
        combatInputHandler = new CombatInputHandler(turnManager, actionResolverService, combatResolutionService, combatStateModel);
        combatPresenter = new CombatPresenter(combatUI, combatInputHandler, combatTurnService, combatTurnResolver, turnManager);
        combatEndService = new CombatEndService();

        inputView = FindObjectOfType<InputView>();
        if (inputView != null)
        {
            combatPresenter.SetInputView(inputView);
        }

        hudView = FindObjectOfType<HudView>();
        if (hudView != null)
        {
            combatPresenter.SetHudView(hudView);
            hudView.SetTurnManager(turnManager);
            hudView.UpdatePlayerHP(playerModel.hp, playerModel.maxHp);
            hudView.UpdatePlayerResources(playerModel.heart, playerModel.body, playerModel.mind);
            hudView.UpdateEnemyHP(enemyModel.hp, enemyModel.maxHp);
            hudView.UpdateAvailableDice(turnManager.availableDice);
        }

        turnManager.OnPrimaryActionSet += HandlePrimaryActionSelected;

        combatStateModel.OnPlayerTurnStart += HandlePlayerTurnStart;
        combatStateModel.OnEnemyTurnStart += HandleEnemyTurnStart;
        combatStateModel.OnCombatEnded += HandleCombatEnded;

        turnTransitionManager.OnPlayerTurnReady += HandlePlayerTurnReady;
        turnTransitionManager.OnEnemyTurnStarting += HandleEnemyTurnStarting;

        combatTurnService.OnEnemyActionDetermined += HandleEnemyActionDetermined;

        bool isPlayerTurn = combatTurnService.StartFirstTurn(playerModel, enemyModel);

        combatUI.SetTurnText($"Turno do {(isPlayerTurn ? "Jogador" : "Inimigo")}");
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.AddLog("Combate iniciado!", CombatLogStyle.Neutral);

        combatLoopCoroutine = StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        while (!combatStateModel.IsCombatFinished())
        {
            if (combatStateModel.IsPlayerTurn())
            {
                yield return StartCoroutine(turnTransitionManager.PlayPlayerTurn(playerModel));
            }
            else if (combatStateModel.IsEnemyTurn())
            {
                yield return StartCoroutine(turnTransitionManager.PlayEnemyTurn(enemyModel));
                ResolveEnemyAction();
                turnManager.StartTurn(3, playerModel.heart, playerModel.body, playerModel.mind);
                combatStateModel.SetPlayerTurn();
            }

            ResolveCombatEnd();
            yield return null;
        }

        yield break;
    }

    private void HandlePlayerTurnStart()
    {
        combatUI.SetTurnText("Seu turno!");
        combatUI.AddLog("Seu turno começou.", CombatLogStyle.Neutral);
        combatPresenter.OnPlayerTurnUIUpdate();
    }

    private void HandleEnemyTurnStart()
    {
        combatUI.SetTurnText("Turno do inimigo...");
        combatUI.AddLog("O inimigo está atacando!", CombatLogStyle.Negative);
        combatPresenter.OnEnemyTurnUIUpdate();
    }

    private void HandlePrimaryActionSelected(PlayerActionType actionType)
    {
        combatPresenter.OnPrimaryActionSelected();
    }

    private void HandlePlayerTurnReady()
    {
        combatUI.UpdateHud(turnManager.availableDice);
    }

    private void HandleEnemyTurnStarting()
    {
        combatUI.AddLog("Inimigo executando ação...", CombatLogStyle.Action);
    }

    private void HandleEnemyActionDetermined(EnemyTurnAction action)
    {
        string actionText = action == EnemyTurnAction.Attack ? "Ataque!" : "Defesa!";
        combatUI.AddLog($"Inimigo: {actionText}", CombatLogStyle.Action);
        combatPresenter.ShowActionFeedback($"Enemy {actionText}");
    }

    private void HandleCombatEnded(CombatOutcome outcome)
    {
        combatUI.AddLog($"Combate terminou: {outcome}!", CombatLogStyle.Info);
        CombatResultStore.SetResult(new CombatResultSnapshot
        {
            playerSnapshot = CreatePlayerSnapshot(),
            outcome = outcome
        });
    }
    
    private void ResolveEnemyAction()
    {
        if (combatTurnService.lastEnemyAction == EnemyTurnAction.Attack)
        {
            int baseDamage = enemyModel.attack;
            int damage = Mathf.Max(1, baseDamage - playerModel.defense);
            
            playerModel.TakeDamage(damage);
            combatUI.AddLog($"Inimigo atacou! Dano recebido: {damage} (ataque: {baseDamage}, defesa: {playerModel.defense})", CombatLogStyle.Negative);
            
            combatPresenter.ShowDamagePopup(damage);
            combatPresenter.PlayDamageShake();

            combatPresenter.UpdatePlayerHPDisplay(playerModel.hp, playerModel.maxHp);
        }
        else if (combatTurnService.lastEnemyAction == EnemyTurnAction.Defend)
        {
            combatUI.AddLog("Inimigo se defendeu, aumentando defesa!", CombatLogStyle.Neutral);
            // Aqui você pode adicionar lógica de defesa permanente ou temporal
            // Por enquanto, apenas registra no log
        }
    }
    
    private void ResolvePlayerAction()
    {
        IReadOnlyList<ActionInstance> queuedActions = combatPresenter.GetQueuedPlayerActions();
        if (queuedActions == null || queuedActions.Count == 0)
        {
            combatUI.AddLog("Nenhuma ação foi executada.", CombatLogStyle.Neutral);
            combatPresenter.ClearActionQueue();
            return;
        }

        ActionInstance action = queuedActions[0];
        if (action == null || action.definition == null)
        {
            combatUI.AddLog("Ação inválida.", CombatLogStyle.Negative);
            combatPresenter.ClearActionQueue();
            return;
        }

        switch (action.definition.type)
        {
            case PlayerActionType.Attack:
                ResolvePlayerAttack(action);
                break;

            case PlayerActionType.Defend:
                ResolvePlayerDefend(action);
                break;

            case PlayerActionType.Investigate:
                ResolvePlayerInvestigate(action);
                break;

            case PlayerActionType.UseItem:
                ResolvePlayerUseItem(action);
                break;

            case PlayerActionType.UseSkill:
                ResolvePlayerUseSkill(action);
                break;

            default:
                combatUI.AddLog($"Ação desconhecida: {action.definition.type}", CombatLogStyle.Negative);
                break;
        }

        combatPresenter.ClearActionQueue();
    }
    
    private void ResolvePlayerAttack(ActionInstance action)
    {
        int roll = diceService.RollD6();
        int baseDamage = playerModel.attack + roll + action.allocatedDice;
        int damage = Mathf.Max(1, baseDamage - enemyModel.defense); 

        enemyModel.TakeDamage(damage);
        
        combatUI.AddLog($"Você atacou! Dano aplicado: {damage} (ataque: {baseDamage}, defesa: {enemyModel.defense})", CombatLogStyle.Action);
        
        combatPresenter.ShowActionFeedback("Ataque!");
        
        // Animar rolagem de dado
        combatPresenter.PlaySingleDiceRollAnimation(roll);
        
        combatPresenter.ShowDamagePopup(damage);
        
        combatPresenter.UpdateEnemyHPDisplay(enemyModel.hp, enemyModel.maxHp);
    }

    private void ResolvePlayerDefend(ActionInstance action)
    {
        int recovery = 1 + action.allocatedDice;
        playerModel.RecoverResources(recovery);
        
        combatUI.AddLog($"Você se defendeu e recuperou {recovery} recurso(s).", CombatLogStyle.Action);
        combatPresenter.ShowActionFeedback("Defesa!");
        
        combatPresenter.UpdatePlayerResourcesDisplay(playerModel.heart, playerModel.body, playerModel.mind);
    }

    private void ResolvePlayerInvestigate(ActionInstance action)
    {
        int roll = diceService.RollD6();
        int investigateValue = roll + action.allocatedDice;
        
        if (investigateValue >= 5)
        {
            combatUI.AddLog($"Investigação bem-sucedida! Você obtém informação completa.", CombatLogStyle.Action);
        }
        else if (investigateValue >= 3)
        {
            combatUI.AddLog($"Investigação parcial. Você descobre algo útil.", CombatLogStyle.Action);
        }
        else
        {
            combatUI.AddLog($"Investigação falhou. Nenhuma informação útil.", CombatLogStyle.Negative);
        }
        
        combatPresenter.ShowActionFeedback("Investigando...");
        
        // Animar rolagem de dado
        combatPresenter.PlaySingleDiceRollAnimation(roll);
    }
    
    private void ResolvePlayerUseItem(ActionInstance action)
    {
        int healAmount = 2;
        playerModel.RecoverResources(healAmount);
        
        combatUI.AddLog($"Item #{action.itemId} usado! Recuperou {healAmount} recursos.", CombatLogStyle.Action);
        combatPresenter.ShowActionFeedback($"Item #{action.itemId}");
        combatPresenter.ShowHealingPopup(healAmount);
        
        combatPresenter.UpdatePlayerResourcesDisplay(playerModel.heart, playerModel.body, playerModel.mind);
    }
    
    private void ResolvePlayerUseSkill(ActionInstance action)
    {
        int roll = diceService.RollD6();
        int skillDamage = Mathf.Max(1, playerModel.attack + 2 + roll - enemyModel.defense);
        
        enemyModel.TakeDamage(skillDamage);
        
        combatUI.AddLog($"Skill #{action.skillId} usada! Dano: {skillDamage}", CombatLogStyle.Action);
        combatPresenter.ShowActionFeedback($"Skill #{action.skillId}!");
        
        // Animar rolagem de dado
        combatPresenter.PlaySingleDiceRollAnimation(roll);
        
        combatPresenter.ShowDamagePopup(skillDamage);
        
        combatPresenter.UpdateEnemyHPDisplay(enemyModel.hp, enemyModel.maxHp);
    }

    public ActionResult PlayerRecharge(bool boosted)
    {
        ActionResult result = combatPresenter.OnRecharge(playerModel, boosted);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerInvestigate()
    {
        ActionResult result = combatPresenter.OnInvestigate(playerModel);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerAddInvestigateDice(int dice)
    {
        ActionResult result = combatPresenter.OnAddInvestigateDice(playerModel, dice);
        return result;
    }

    public ActionResult PlayerFlee(int dice)
    {
        ActionResult result = combatPresenter.OnFlee(playerModel, dice);
        return result;
    }

    public ActionResult PlayerAttack()
    {
        ActionResult result = combatPresenter.OnAttack(playerModel);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerDefend()
    {
        ActionResult result = combatPresenter.OnDefend(playerModel);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerAddAttackDice(int dice)
    {
        ActionResult result = combatPresenter.OnAddAttackDice(playerModel, dice);
        if (result.success)
        {
            combatPresenter.UpdateAvailableDiceDisplay(turnManager.availableDice);
            combatPresenter.UpdateAllActionDiceCounters(
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
            );
        }
        return result;
    }

    public ActionResult PlayerAddDefendDice(int dice)
    {
        ActionResult result = combatPresenter.OnAddDefendDice(playerModel, dice);
        if (result.success)
        {
            combatPresenter.UpdateAvailableDiceDisplay(turnManager.availableDice);
            combatPresenter.UpdateAllActionDiceCounters(
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
            );
        }
        return result;
    }

    public ActionResult PlayerSubtractAttackDice()
    {
        ActionResult result = combatPresenter.OnSubtractAttackDice();
        if (result.success)
        {
            combatUI.UpdateHud(turnManager.availableDice);
            combatPresenter.UpdateAvailableDiceDisplay(turnManager.availableDice);
            combatPresenter.UpdateAllActionDiceCounters(
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
            );
        }
        return result;
    }

    public ActionResult PlayerSubtractInvestigateDice()
    {
        ActionResult result = combatPresenter.OnSubtractInvestigateDice();
        if (result.success)
        {
            combatUI.UpdateHud(turnManager.availableDice);
            combatPresenter.UpdateAvailableDiceDisplay(turnManager.availableDice);
            combatPresenter.UpdateAllActionDiceCounters(
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
            );
        }
        return result;
    }

    public ActionResult PlayerSubtractDefendDice()
    {
        ActionResult result = combatPresenter.OnSubtractDefendDice();
        if (result.success)
        {
            combatUI.UpdateHud(turnManager.availableDice);
            combatPresenter.UpdateAvailableDiceDisplay(turnManager.availableDice);
            combatPresenter.UpdateAllActionDiceCounters(
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
                turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
            );
        }
        return result;
    }

    public ActionResult PlayerUseItem()
    {
        return combatPresenter.OnUseItem();
    }

    public ActionResult PlayerSelectItem(int itemId)
    {
        return combatPresenter.OnItemSelected(playerModel, itemId);
    }

    public ActionResult PlayerSkills()
    {
        return combatPresenter.OnSkills();
    }

    public ActionResult PlayerSelectSkill(int skillId)
    {
        return combatPresenter.OnSkillSelected(playerModel, skillId);
    }

    public ActionResult PlayerUseSkill()
    {
        return combatPresenter.OnUseSkill(playerModel, 1);
    }

    public void PlayerInfo()
    {
        combatPresenter.OnInfo();
    }

    public ActionResult EndPlayerTurn()
    {
        ActionResult result = combatPresenter.OnEndTurn(playerModel, enemyModel);
        
        if (result.success)
        {
            ResolvePlayerAction();
            combatStateModel.SetEnemyTurn();
        }

        ResolveCombatEnd();
        return result;
    }

    private void ResolveCombatEnd()
    {
        CombatOutcome? outcome = combatEndService.CheckEnd(playerModel, enemyModel);
        if (outcome.HasValue)
        {
            EndCombat(outcome.Value);
        }
    }

    private void EndCombat(CombatOutcome outcome)
    {
        combatStateModel.EndCombat(outcome);
        if (combatLoopCoroutine != null)
        {
            StopCoroutine(combatLoopCoroutine);
        }
    }

    private PlayerStatusSnapshot CreatePlayerSnapshot()
    {
        return new PlayerStatusSnapshot
        {
            heart = playerModel.heart,
            body = playerModel.body,
            mind = playerModel.mind,
            hp = playerModel.hp,
            maxHeart = playerModel.maxHeart,
            maxBody = playerModel.maxBody,
            maxMind = playerModel.maxMind,
            maxHp = playerModel.maxHp,
            currentArchetype = basePlayerSnapshot.currentArchetype,
            archetypePoints = basePlayerSnapshot.archetypePoints
        };
    }

    private void OnDestroy()
    {
        if (combatStateModel != null)
        {
            combatStateModel.OnPlayerTurnStart -= HandlePlayerTurnStart;
            combatStateModel.OnEnemyTurnStart -= HandleEnemyTurnStart;
            combatStateModel.OnCombatEnded -= HandleCombatEnded;
        }

        if (turnTransitionManager != null)
        {
            turnTransitionManager.OnPlayerTurnReady -= HandlePlayerTurnReady;
            turnTransitionManager.OnEnemyTurnStarting -= HandleEnemyTurnStarting;
        }

        if (combatTurnService != null)
        {
            combatTurnService.OnEnemyActionDetermined -= HandleEnemyActionDetermined;
        }

        if (turnManager != null)
        {
            turnManager.OnPrimaryActionSet -= HandlePrimaryActionSelected;
        }

        if (combatLoopCoroutine != null)
        {
            StopCoroutine(combatLoopCoroutine);
        }
    }
}
