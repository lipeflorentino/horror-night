public class CombatInputHandler
{
    private readonly TurnManager turnManager;
    private readonly ActionResolverService actionResolverService;
    private readonly CombatResolutionService combatResolutionService;
    private readonly CombatStateModel combatStateModel;

    public CombatInputHandler(
        TurnManager turnManager,
        ActionResolverService actionResolverService,
        CombatResolutionService combatResolutionService,
        CombatStateModel combatStateModel)
    {
        this.turnManager = turnManager;
        this.actionResolverService = actionResolverService;
        this.combatResolutionService = combatResolutionService;
        this.combatStateModel = combatStateModel;
    }

    public ActionResult HandleRecharge(CombatBattlerModel player, bool boosted)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        int diceCost = boosted ? 1 : 0;
        if (!turnManager.TrySpendDice(diceCost))
        {
            return Fail("Not enough dice.");
        }

        ActionResult result = actionResolverService.ResolveRecharge(boosted);
        combatResolutionService.ApplyRecovery(player, result.damage);
        return result;
    }

    public ActionResult HandleInvestigate()
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        if (!turnManager.TrySpendDice(1))
        {
            return Fail("Not enough dice.");
        }

        return actionResolverService.ResolveInvestigate();
    }

    public ActionResult HandleFlee(int dice)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        int diceCost = dice < 0 ? 0 : dice;
        if (!turnManager.TrySpendDice(diceCost))
        {
            return Fail("Not enough dice.");
        }

        return actionResolverService.ResolveFlee(diceCost);
    }

    public ActionResult HandleAttack(CombatBattlerModel target, int baseAttack, int defense)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        if (!turnManager.TrySpendDice(1))
        {
            return Fail("Not enough dice.");
        }

        ActionResult result = actionResolverService.ResolveAttack(baseAttack);
        int mitigatedDamage = DamageCalculator.CalculateDamage(result.damage, 0, defense);
        int appliedDamage = combatResolutionService.ApplyDamage(target, mitigatedDamage);

        result.damage = appliedDamage;
        return result;
    }

    public ActionResult HandleEndTurn()
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        combatStateModel.SetEnemyTurn();

        return new ActionResult
        {
            diceSpent = 0,
            roll = 0,
            success = true,
            damage = 0,
            message = "Turn ended."
        };
    }

    private bool IsPlayerTurn()
    {
        return combatStateModel.currentState == CombatFlowState.PlayerTurn;
    }

    private static ActionResult Fail(string message)
    {
        return new ActionResult
        {
            diceSpent = 0,
            roll = 0,
            success = false,
            damage = 0,
            message = message
        };
    }
}
