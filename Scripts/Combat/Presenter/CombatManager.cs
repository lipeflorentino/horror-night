using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private CombatUI combatUI;

    [SerializeField] private CombatBattlerModel playerModel;
    [SerializeField] private CombatBattlerModel enemyModel;
    [SerializeField] private PlayerStatusSnapshot basePlayerSnapshot;

    [SerializeField] private CombatStateModel combatStateModel;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DiceService diceService;
    [SerializeField] private CombatTurnService combatTurnService;
    [SerializeField] private ActionResolverService actionResolverService;
    [SerializeField] private CombatResolutionService combatResolutionService;
    [SerializeField] private CombatInputHandler combatInputHandler;
    [SerializeField] private CombatPresenter combatPresenter;
    [SerializeField] private CombatEndService combatEndService;
    [SerializeField] private CombatModelFactory combatModelFactory;

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
        combatInputHandler = new CombatInputHandler(turnManager, actionResolverService, combatResolutionService, combatStateModel);
        combatPresenter = new CombatPresenter(combatUI, combatInputHandler, combatTurnService, turnManager);
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
        ActionResult result = combatPresenter.OnInvestigate();
        ResolveCombatEnd();
        return result;
    }

    public ActionResult PlayerFlee(int dice)
    {
        ActionResult result = combatPresenter.OnFlee(dice);

        if (result.success)
        {
            EndCombat(CombatOutcome.Fled);
        }

        return result;
    }

    public ActionResult PlayerAttack()
    {
        ActionResult result = combatPresenter.OnDamage(enemyModel, playerModel.attack, enemyModel.defense);
        ResolveCombatEnd();
        return result;
    }

    public ActionResult EndPlayerTurn()
    {
        ActionResult result = combatPresenter.OnEndTurn();
        combatTurnService.StartEnemyTurn();
        combatTurnService.StartPlayerTurn();
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
