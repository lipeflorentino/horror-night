using UnityEngine;

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
    private ActionResolverService actionResolverService;
    private CombatResolutionService combatResolutionService;
    private CombatTurnResolver combatTurnResolver;
    private CombatInputHandler combatInputHandler;
    private CombatPresenter combatPresenter;
    private CombatEndService combatEndService;
    private CombatModelFactory combatModelFactory;

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
        actionResolverService = new ActionResolverService(diceService);
        combatResolutionService = new CombatResolutionService();
        combatTurnResolver = new CombatTurnResolver(combatResolutionService, diceService);
        combatInputHandler = new CombatInputHandler(turnManager, actionResolverService, combatResolutionService, combatStateModel);
        combatPresenter = new CombatPresenter(combatUI, combatInputHandler, combatTurnService, combatTurnResolver, turnManager);
        combatEndService = new CombatEndService();

        combatPresenter.OnTurnStart(playerModel, enemyModel);
    }

    public ActionResult PlayerRecharge(bool boosted)
    {
        ActionResult result = combatPresenter.OnRecharge(playerModel, boosted);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerInvestigate()
    {
        ActionResult result = combatPresenter.OnAddInvestigateDice(playerModel, 1);
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
        ActionResult result = combatPresenter.OnAddAttackDice(playerModel, 1);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerAddAttackDice(int dice)
    {
        ActionResult result = combatPresenter.OnAddAttackDice(playerModel, dice);
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
