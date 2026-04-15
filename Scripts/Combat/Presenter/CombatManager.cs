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
        combatInputHandler = new CombatInputHandler(turnManager, actionResolverService, combatResolutionService, combatStateModel);
        combatPresenter = new CombatPresenter(combatUI, combatInputHandler, combatTurnService, turnManager);
        combatEndService = new CombatEndService();

        inputView = FindObjectOfType<InputView>();
        if (inputView != null)
        {
            combatPresenter.SetInputView(inputView);
        }

        hudView = FindObjectOfType<HudView>();
        if (hudView != null)
        {
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

        bool isPlayerTurn = combatTurnService.StartFirstTurn(playerModel, enemyModel);

        combatUI.SetTurnText($"Turno do {(isPlayerTurn ? "Jogador" : "Inimigo")}");
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.AddLog("Combate iniciado!", CombatLogStyle.Neutral);

        combatLoopCoroutine = StartCoroutine(CombatLoop());
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
    }

    private IEnumerator CombatLoop()
    {
        bool isFirstRound = true;

        while (!combatStateModel.IsCombatFinished())
        {
            if (isFirstRound)
            {
                bool playerAttacksFirst = combatTurnService.StartFirstTurn(playerModel, enemyModel);
                if (!playerAttacksFirst)
                {
                    yield return StartCoroutine(HandleEnemyAttackPhase());
                    yield return StartCoroutine(HandlePlayerDefensePhase());
                }
                isFirstRound = false;
            }

            yield return StartCoroutine(HandlePlayerAttackPhase());
            yield return StartCoroutine(HandleEnemyDefensePhase());
            yield return StartCoroutine(ResolveRoundPhase());

            ResolveCombatEnd();

            if (!combatStateModel.IsCombatFinished())
            {
                yield return StartCoroutine(HandleEnemyAttackPhase());
                yield return StartCoroutine(HandlePlayerDefensePhase());
                yield return StartCoroutine(ResolveRoundPhase());

                ResolveCombatEnd();
            }

            yield return null;
        }

        yield break;
    }

    private IEnumerator HandlePlayerAttackPhase()
    {
        combatStateModel.SetPlayerDecidingAttack();
        turnManager.StartTurn(3, playerModel.heart, playerModel.body, playerModel.mind);
        turnManager.actionQueue = new CombatActionQueue();
        
        yield return new WaitUntil(() => combatStateModel.currentState != CombatFlowState.PlayerDecidingAttack || combatStateModel.IsCombatFinished());
    }

    private IEnumerator HandleEnemyDefensePhase()
    {
        combatStateModel.SetEnemyDecidingDefense();
        var enemyActions = combatTurnService.GenerateEnemyActionsForDefense(enemyModel);
        
        if (enemyActions.Count > 0)
        {
            var mainAction = enemyActions[0];
            combatUI.AddLog($"Inimigo escolheu: {mainAction.definition.type}", CombatLogStyle.Action);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleEnemyAttackPhase()
    {
        combatStateModel.SetEnemyDecidingAttack();
        var enemyActions = combatTurnService.GenerateEnemyActionsForAttack(enemyModel);
        
        if (enemyActions.Count > 0)
        {
            var mainAction = enemyActions[0];
            combatTurnService.lastEnemyAction = mainAction.definition.type == PlayerActionType.Attack 
                ? EnemyTurnAction.Attack 
                : EnemyTurnAction.Defend;
            combatUI.AddLog($"Inimigo escolheu: {mainAction.definition.type}", CombatLogStyle.Action);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandlePlayerDefensePhase()
    {
        combatStateModel.SetPlayerDecidingDefense();
        turnManager.StartTurn(3, playerModel.heart, playerModel.body, playerModel.mind);
        turnManager.actionQueue = new CombatActionQueue();
        
        yield return new WaitUntil(() => combatStateModel.currentState != CombatFlowState.PlayerDecidingDefense || combatStateModel.IsCombatFinished());
    }

    private IEnumerator ResolveRoundPhase()
    {
        combatStateModel.SetResolvingRound();
        
        var playerActions = turnManager.actionQueue?.GetAll() ?? new List<ActionInstance>();
        ResolvePlayerActions(playerActions);

        ResolveEnemyAction();

        yield return new WaitForSeconds(0.5f);
    }

    private void ResolvePlayerActions(IReadOnlyList<ActionInstance> playerActions)
    {
        if (playerActions == null || playerActions.Count == 0)
        {
            combatUI.AddLog("Nenhuma ação foi executada.", CombatLogStyle.Neutral);
            return;
        }

        foreach (var action in playerActions)
        {
            if (action == null || action.definition == null)
            {
                combatUI.AddLog("Ação inválida.", CombatLogStyle.Negative);
                continue;
            }

            var context = new CombatContext { Actor = playerModel, Target = enemyModel };
            var result = actionResolverService.Resolve(action, context);
            
            if (result.success)
            {
                combatUI.AddLog(result.message, CombatLogStyle.Action);
                
                if (action.definition.type == PlayerActionType.Attack || action.definition.type == PlayerActionType.UseSkill)
                {
                    hudView?.ShowDamagePopup(result.damage);
                    hudView?.UpdateEnemyHP(enemyModel.hp, enemyModel.maxHp);
                    if (result.roll > 0)
                        hudView?.PlaySingleDiceRoll(result.roll);
                }
                else if (action.definition.type == PlayerActionType.Defend)
                {
                    hudView?.UpdatePlayerResources(playerModel.heart, playerModel.body, playerModel.mind);
                }
                else if (action.definition.type == PlayerActionType.UseItem)
                {
                    hudView?.ShowHealingPopup(result.damage);
                    hudView?.UpdatePlayerResources(playerModel.heart, playerModel.body, playerModel.mind);
                }
                else if (action.definition.type == PlayerActionType.Investigate)
                {
                    if (result.roll > 0)
                        hudView?.PlaySingleDiceRoll(result.roll);
                }
            }
            else
            {
                combatUI.AddLog(result.message, CombatLogStyle.Negative);
            }
        }
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
            
            hudView?.ShowDamagePopup(damage);
            hudView?.PlayDamageShake();
            hudView?.UpdatePlayerHP(playerModel.hp, playerModel.maxHp);
        }
        else if (combatTurnService.lastEnemyAction == EnemyTurnAction.Defend)
        {
            combatUI.AddLog("Inimigo se defendeu, aumentando defesa!", CombatLogStyle.Neutral);
        }
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
            UpdateDiceUI();
        }
        return result;
    }

    public ActionResult PlayerAddDefendDice(int dice)
    {
        ActionResult result = combatPresenter.OnAddDefendDice(playerModel, dice);
        if (result.success)
        {
            UpdateDiceUI();
        }
        return result;
    }

    public ActionResult PlayerSubtractAttackDice()
    {
        ActionResult result = combatPresenter.OnSubtractAttackDice();
        if (result.success)
        {
            UpdateDiceUI();
        }
        return result;
    }

    public ActionResult PlayerSubtractInvestigateDice()
    {
        ActionResult result = combatPresenter.OnSubtractInvestigateDice();
        if (result.success)
        {
            UpdateDiceUI();
        }
        return result;
    }

    public ActionResult PlayerSubtractDefendDice()
    {
        ActionResult result = combatPresenter.OnSubtractDefendDice();
        if (result.success)
        {
            UpdateDiceUI();
        }
        return result;
    }

    private void UpdateDiceUI()
    {
        combatUI.UpdateHud(turnManager.availableDice);
        combatPresenter.UpdateAllActionDiceCounters(
            turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack),
            turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate),
            turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend)
        );
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
            combatStateModel.SetEnemyDecidingDefense();
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
