public class CombatInputHandler
{
    private readonly TurnManager turnManager;
    private readonly CombatStateModel combatStateModel;
    private readonly ActionDefinitionFactory actionDefinitionFactory;

    public CombatInputHandler(
        TurnManager turnManager,
        ActionResolverService actionResolverService,
        CombatResolutionService combatResolutionService,
        CombatStateModel combatStateModel)
    {
        this.turnManager = turnManager;
        this.combatStateModel = combatStateModel;
        actionDefinitionFactory = new ActionDefinitionFactory();
    }

    public ActionResult HandleRecharge(CombatBattlerModel player, bool boosted)
    {
        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateDefend(),
            allocatedDice = boosted ? 1 : 0,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0
        };

        return TryQueueAction(player, action, "Defend queued.");
    }

    public ActionResult QueueInvestigate(CombatBattlerModel player, int diceAmount)
    {
        int allocatedDice = diceAmount < 1 ? 1 : diceAmount;

        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateInvestigate(),
            allocatedDice = allocatedDice - 1,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0
        };

        return TryQueueAction(player, action, "Investigate queued.");
    }

    public ActionResult QueueDefend(CombatBattlerModel player, int diceAmount)
    {
        int allocatedDice = diceAmount < 1 ? 1 : diceAmount;

        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateDefend(),
            allocatedDice = allocatedDice - 1,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0
        };

        return TryQueueAction(player, action, "Defend queued.");
    }

    public ActionResult HandleFlee(CombatBattlerModel player, int dice)
    {
        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateDefend(),
            allocatedDice = dice < 1 ? 0 : dice - 1,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0
        };

        return TryQueueAction(player, action, "Defend queued.");
    }

    public ActionResult QueueAttack(CombatBattlerModel player, int diceAmount)
    {
        int allocatedDice = diceAmount < 1 ? 1 : diceAmount;

        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateAttack(),
            allocatedDice = allocatedDice - 1,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0
        };

        return TryQueueAction(player, action, "Attack queued.");
    }

    public ActionResult QueueUseItemSelection(CombatBattlerModel player, int itemId)
    {
        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateUseItem(),
            allocatedDice = 0,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0,
            itemId = itemId
        };

        return TryQueueAction(player, action, $"Use Item queued (item {itemId}).");
    }

    public ActionResult QueueUseSkillSelection(CombatBattlerModel player, int skillId)
    {
        ActionInstance action = new ActionInstance
        {
            definition = actionDefinitionFactory.CreateUseSkill(),
            allocatedDice = 0,
            allocatedHeart = 0,
            allocatedBody = 0,
            allocatedMind = 0,
            skillId = skillId
        };

        return TryQueueAction(player, action, $"Use Skill queued (skill {skillId}).");
    }

    public ActionResult HandleEndTurn()
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        return new ActionResult
        {
            diceSpent = 0,
            roll = 0,
            success = true,
            damage = 0,
            message = "Turn ended."
        };
    }

    private ActionResult TryQueueAction(CombatBattlerModel player, ActionInstance action, string successMessage)
    {
        if (!IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        if (player == null)
        {
            return Fail("Player not found.");
        }

        if (turnManager.availableDice <= 0)
        {
            return Fail("No dice available.");
        }

        if (player.heart <= 0 && player.body <= 0 && player.mind <= 0)
        {
            return Fail("No resources available.");
        }

        if (!turnManager.CanAfford(action))
        {
            return Fail("Cannot afford action.");
        }

        int totalDice = action.TotalDiceCost();
        int heartCost = action.definition.heartCost + action.allocatedHeart;
        int bodyCost = action.definition.bodyCost + action.allocatedBody;
        int mindCost = action.definition.mindCost + action.allocatedMind;

        if (!turnManager.TrySpendDice(totalDice))
        {
            return Fail("Not enough dice.");
        }

        if (!turnManager.TrySpendResources(heartCost, bodyCost, mindCost))
        {
            return Fail("Not enough resources.");
        }

        if (!player.SpendResources(heartCost, bodyCost, mindCost))
        {
            return Fail("Not enough resources.");
        }

        turnManager.QueueAction(action);

        return new ActionResult
        {
            diceSpent = totalDice,
            roll = 0,
            success = true,
            damage = 0,
            message = successMessage
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
