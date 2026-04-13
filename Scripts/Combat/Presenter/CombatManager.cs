using UnityEngine;
using System.Collections;

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

        // Subscribir aos eventos do estado de combate
        combatStateModel.OnPlayerTurnStart += HandlePlayerTurnStart;
        combatStateModel.OnEnemyTurnStart += HandleEnemyTurnStart;
        combatStateModel.OnCombatEnded += HandleCombatEnded;

        // Subscribir aos eventos do TurnTransitionManager
        turnTransitionManager.OnPlayerTurnReady += HandlePlayerTurnReady;
        turnTransitionManager.OnEnemyTurnStarting += HandleEnemyTurnStarting;

        // Iniciar o combate
        bool isPlayerTurn = combatTurnService.StartFirstTurn(playerModel, enemyModel);
        combatUI.SetTurnText($"Turno do {(isPlayerTurn ? "Jogador" : "Inimigo")}");
        combatUI.UpdateHud(turnManager.availableDice);
        combatUI.AddLog("Combate iniciado!", CombatLogStyle.Neutral);

        // Iniciar corrotina de loop de combate
        combatLoopCoroutine = StartCoroutine(CombatLoop());
    }

    /// <summary>
    /// Loop principal de combate. Alterna entre turnos do jogador e inimigo até o fim.
    /// </summary>
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
                // Após turno do inimigo, resolver danos e transicionar
                ResolveEnemyAction();
                turnManager.StartTurn(3, playerModel.heart, playerModel.body, playerModel.mind);
                combatStateModel.SetPlayerTurn();
            }

            // Checar se o combate acabou
            ResolveCombatEnd();
            yield return null;
        }

        // Combate terminado
        yield break;
    }

    private void HandlePlayerTurnStart()
    {
        combatUI.SetTurnText("Seu turno!");
        combatUI.AddLog("Seu turno começou.", CombatLogStyle.Neutral);
    }

    private void HandleEnemyTurnStart()
    {
        combatUI.SetTurnText("Turno do inimigo...");
        combatUI.AddLog("O inimigo está atacando!", CombatLogStyle.Negative);
    }

    private void HandlePlayerTurnReady()
    {
        combatUI.UpdateHud(turnManager.availableDice);
    }

    private void HandleEnemyTurnStarting()
    {
        combatUI.AddLog("Inimigo executando ação...", CombatLogStyle.Action);
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

    /// <summary>
    /// Resolve a ação do inimigo e aplica dano ao jogador.
    /// </summary>
    private void ResolveEnemyAction()
    {
        if (combatTurnService.lastEnemyAction == EnemyTurnAction.Attack)
        {
            int damage = enemyModel.attack;
            playerModel.TakeDamage(damage);
            combatUI.AddLog($"Inimigo atacou! Dano: {damage}", CombatLogStyle.Negative);
        }
        else if (combatTurnService.lastEnemyAction == EnemyTurnAction.Defend)
        {
            combatUI.AddLog("Inimigo se defendeu.", CombatLogStyle.Neutral);
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
        return result;
    }

    public ActionResult PlayerAddDefendDice(int dice)
    {
        return combatPresenter.OnAddDefendDice(playerModel, dice);
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
            // Transicionar para turno do inimigo
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
        
        // Parar o loop de combate
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
        // Desinscrever dos eventos
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

        // Parar corrotina se ainda ativa
        if (combatLoopCoroutine != null)
        {
            StopCoroutine(combatLoopCoroutine);
        }
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
        return result;
    }

    public ActionResult PlayerAddDefendDice(int dice)
    {
        return combatPresenter.OnAddDefendDice(playerModel, dice);
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
        CombatResultStore.SetResult(new CombatResultSnapshot
        {
            playerSnapshot = CreatePlayerSnapshot(),
            outcome = outcome
        });
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
}
