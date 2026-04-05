using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private CombatUI combatUI;
    [SerializeField] private PlayerBattler playerBattler;
    [SerializeField] private EnemyBattler enemyBattler;

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

    private void Awake()
    {
        InitializeCombat();
    }

    public void InitializeCombat()
    {
        playerModel = CreateModelFromPlayer(playerBattler);
        enemyModel = CreateModelFromEnemy(enemyBattler);

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

    private static CombatBattlerModel CreateModelFromPlayer(PlayerBattler source)
    {
        return new CombatBattlerModel
        {
            heart = source.heart,
            body = source.body,
            mind = source.mind,
            maxHeart = source.heart,
            maxBody = source.body,
            maxMind = source.mind,
            hp = source.body,
            maxHp = source.body,
            attack = source.attack,
            defense = source.defense,
            initiative = source.initiative
        };
    }

    private static CombatBattlerModel CreateModelFromEnemy(EnemyBattler source)
    {
        return new CombatBattlerModel
        {
            heart = source.heart,
            body = source.body,
            mind = source.mind,
            maxHeart = source.heart,
            maxBody = source.body,
            maxMind = source.mind,
            hp = source.body,
            maxHp = source.body,
            attack = source.attack,
            defense = source.defense,
            initiative = source.initiative
        };
    }
}