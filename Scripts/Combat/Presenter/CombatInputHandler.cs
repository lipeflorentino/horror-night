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

        var action = new CombatActionData
        {
            actionType = PlayerActionType.UseSkill,
            diceCost = boosted ? 1 : 0,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0
        };

        return TryReserveAction(player, action, "Recharge queued.");
    }

    public ActionResult HandleInvestigate(CombatBattlerModel player)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        var action = new CombatActionData
        {
            actionType = PlayerActionType.Investigate,
            diceCost = 1,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0
        };

        return TryReserveAction(player, action, "Investigate queued.");
    }

    public ActionResult HandleFlee(CombatBattlerModel player, int dice)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        int diceCost = dice < 0 ? 0 : dice;
        var action = new CombatActionData
        {
            actionType = PlayerActionType.UseSkill,
            diceCost = diceCost,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0
        };

        return TryReserveAction(player, action, "Flee queued.");
    }

    public ActionResult HandleAttack(CombatBattlerModel player, CombatBattlerModel target, int baseAttack, int defense)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        var action = new CombatActionData
        {
            actionType = PlayerActionType.Attack,
            diceCost = 1,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0,
            predictedValue = baseAttack
        };

        return TryReserveAction(player, action, "Attack queued.");
    }

    public ActionResult HandleEndTurn(CombatBattlerModel player)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        var action = new CombatActionData
        {
            actionType = PlayerActionType.EndTurn,
            diceCost = 0,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0
        };

        return TryReserveAction(player, action, "End turn queued.");
    }

    private ActionResult TryReserveAction(CombatBattlerModel player, CombatActionData action, string successMessage)
    {
        if (turnManager.availableDice <= 0)
        {
            return Fail("No dice available.");
        }

        if (!HasAnyResources(player))
        {
            return Fail("No resources available.");
        }

        if (action.diceCost > turnManager.availableDice)
        {
            return Fail("Not enough dice.");
        }

        if (!HasEnoughResources(player, action.heartCost, action.bodyCost, action.mindCost))
        {
            return Fail("Not enough resources.");
        }

        if (!turnManager.TrySpendDice(action.diceCost))
        {
            return Fail("Not enough dice.");
        }

        if (player != null && !player.SpendResources(action.heartCost, action.bodyCost, action.mindCost))
        {
            return Fail("Not enough resources.");
        }

        turnManager.QueueAction(action);

        return new ActionResult
        {
            diceSpent = action.diceCost,
            roll = 0,
            success = true,
            damage = 0,
            message = successMessage
        };
    }

    private static bool HasAnyResources(CombatBattlerModel player)
    {
        if (player == null)
        {
            return false;
        }

        return player.heart > 0 || player.body > 0 || player.mind > 0;
    }

    private static bool HasEnoughResources(CombatBattlerModel player, int heartCost, int bodyCost, int mindCost)
    {
        int safeHeartCost = heartCost < 0 ? 0 : heartCost;
        int safeBodyCost = bodyCost < 0 ? 0 : bodyCost;
        int safeMindCost = mindCost < 0 ? 0 : mindCost;

        if (player == null)
        {
            return safeHeartCost == 0 && safeBodyCost == 0 && safeMindCost == 0;
        }

        return player.heart >= safeHeartCost && player.body >= safeBodyCost && player.mind >= safeMindCost;
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
