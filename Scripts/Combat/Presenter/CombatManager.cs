using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private CombatUI combatUI;

    private CombatBattlerModel playerModel;
    private CombatBattlerModel enemyModel;
    private PlayerStatusSnapshot basePlayerSnapshot;

    private CombatStateModel combatStateModel;
    private TurnManager turnManager;
    private DiceService diceService;
    private CombatTurnService combatTurnService;
    private ActionResolverService actionResolverService;
    private CombatResolutionService combatResolutionService;
    private CombatInputHandler combatInputHandler;
    private CombatTurnResolver combatTurnResolver;
    private EnemyActionPlanner enemyActionPlanner;
    private EnemyTurnHandler enemyTurnHandler;
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

        CombatSessionData sessionData = CombatSessionStore.Consume();
        if (sessionData == null || sessionData.enemyInstance == null)
        {
            Debug.LogError("Combat session data was not found. CombatManager requires CombatSessionStore data to initialize.");
            return;
        }

        playerModel = combatModelFactory.CreatePlayer(sessionData.playerSnapshot);
        enemyModel = combatModelFactory.CreateEnemy(sessionData.enemyInstance);
        basePlayerSnapshot = sessionData.playerSnapshot;

        combatStateModel = new CombatStateModel();
        turnManager = new TurnManager();
        diceService = new DiceService();
        combatTurnService = new CombatTurnService(combatStateModel, turnManager, diceService);
        actionResolverService = new ActionResolverService(diceService);
        combatResolutionService = new CombatResolutionService();
        combatInputHandler = new CombatInputHandler(turnManager, actionResolverService, combatResolutionService, combatStateModel);
        combatTurnResolver = new CombatTurnResolver(combatResolutionService);
        enemyActionPlanner = new EnemyActionPlanner();
        enemyTurnHandler = new EnemyTurnHandler(combatTurnService, enemyActionPlanner, combatTurnResolver);
        combatPresenter = new CombatPresenter(combatUI, combatInputHandler, combatTurnService, combatTurnResolver, enemyTurnHandler, turnManager);
        combatEndService = new CombatEndService();

        combatPresenter.OnTurnStart(playerModel, enemyModel);
    }

    public ActionResult PlayerRecharge(bool boosted)
    {
        return combatPresenter.OnRecharge(playerModel, boosted);
    }

    public ActionResult PlayerInvestigate()
    {
        return combatPresenter.OnInvestigate(playerModel);
    }

    public ActionResult PlayerFlee(int dice)
    {
        return combatPresenter.OnFlee(playerModel, dice);
    }

    public ActionResult PlayerAttack()
    {
        return combatPresenter.OnDamage(playerModel, enemyModel, playerModel.attack, enemyModel.defense);
    }

    public ActionResult EndPlayerTurn()
    {
        return combatPresenter.OnEndTurn(playerModel);
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
