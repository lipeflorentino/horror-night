using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private CombatUI combatUI;

    private CombatBattlerModel playerModel;
    private CombatBattlerModel enemyModel;

    private CombatStateModel combatStateModel;
    private TurnManager turnManager;
    private DiceService diceService;
    private CombatTurnService combatTurnService;
    private ActionResolverService actionResolverService;
    private CombatResolutionService combatResolutionService;
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

        CombatSessionData sessionData = CombatSessionStore.Consume();
        if (sessionData == null || sessionData.enemyInstance == null)
        {
            Debug.LogError("Combat session data was not found. CombatManager requires CombatSessionStore data to initialize.");
            return;
        }

        playerModel = combatModelFactory.CreatePlayer(sessionData.playerSnapshot);
        enemyModel = combatModelFactory.CreateEnemy(sessionData.enemyInstance);

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
            combatStateModel.EndCombat(CombatOutcome.Fled);
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
            combatStateModel.EndCombat(outcome.Value);
        }
    }
}
