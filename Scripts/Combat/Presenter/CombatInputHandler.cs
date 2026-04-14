public class CombatInputHandler
{
    private readonly TurnManager turnManager;
    private readonly CombatStateModel combatStateModel;
    private readonly ActionDefinitionFactory actionDefinitionFactory;
    private readonly ActionValidator actionValidator;

    public CombatInputHandler(
        TurnManager turnManager,
        ActionResolverService actionResolverService,
        CombatResolutionService combatResolutionService,
        CombatStateModel combatStateModel)
    {
        this.turnManager = turnManager;
        this.combatStateModel = combatStateModel;
        actionDefinitionFactory = new ActionDefinitionFactory();
        actionValidator = new ActionValidator(turnManager, combatStateModel);
    }

    private ActionResult TryModifyActionDice(PlayerActionType actionType, int amount, bool isAdding)
    {
        var validationError = isAdding
            ? actionValidator.ValidateDiceAllocation(actionType, amount)
            : actionValidator.ValidateDiceRemoval(actionType, amount);

        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        var success = isAdding
            ? turnManager.TryAddDiceToAction(actionType, amount)
            : turnManager.TryRemoveDiceFromAction(actionType, amount);

        if (!success)
        {
            string operation = isAdding ? "allocate" : "remove";
            return Fail($"Failed to {operation} dice from {actionType}.");
        }

        int allocated = turnManager.GetAllocatedDiceForAction(actionType);
        string verb = isAdding ? "Added" : "Removed";

        return new ActionResult
        {
            success = true,
            message = $"{verb} {amount} to {actionType}. Total: {allocated}",
            diceSpent = 0
        };
    }

    public ActionResult TryAddAttackDice(int diceToAdd) => TryModifyActionDice(PlayerActionType.Attack, diceToAdd, true);
    public ActionResult TryRemoveAttackDice(int diceToRemove) => TryModifyActionDice(PlayerActionType.Attack, diceToRemove, false);
    public ActionResult TryAddInvestigateDice(int diceToAdd) => TryModifyActionDice(PlayerActionType.Investigate, diceToAdd, true);
    public ActionResult TryRemoveInvestigateDice(int diceToRemove) => TryModifyActionDice(PlayerActionType.Investigate, diceToRemove, false);
    public ActionResult TryAddDefendDice(int diceToAdd) => TryModifyActionDice(PlayerActionType.Defend, diceToAdd, true);
    public ActionResult TryRemoveDefendDice(int diceToRemove) => TryModifyActionDice(PlayerActionType.Defend, diceToRemove, false);

    public ActionResult HandleRecharge(CombatBattlerModel player, bool boosted)
    {
        string validationError = actionValidator.ValidatePrimaryAction(PlayerActionType.Defend);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.SetPrimaryAction(PlayerActionType.Defend))
            return Fail("Cannot change from current action.");

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
        string validationError = actionValidator.ValidatePrimaryAction(PlayerActionType.Investigate);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.SetPrimaryAction(PlayerActionType.Investigate))
            return Fail("Cannot change from current action.");

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
        string validationError = actionValidator.ValidatePrimaryAction(PlayerActionType.Defend);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.SetPrimaryAction(PlayerActionType.Defend))
            return Fail("Cannot change from current action.");

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
        string validationError = actionValidator.ValidatePrimaryAction(PlayerActionType.Attack);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.SetPrimaryAction(PlayerActionType.Attack))
            return Fail("Cannot change from current action.");

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
        string validationError = actionValidator.ValidateSecondaryAction(PlayerActionType.UseItem);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.TryUseSecondaryAction())
            return Fail("Cannot use secondary action.");

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
        string validationError = actionValidator.ValidateSecondaryAction(PlayerActionType.UseSkill);
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

        if (!turnManager.TryUseSecondaryAction())
            return Fail("Cannot use secondary action.");

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
        string validationError = actionValidator.ValidateEndTurn();
        if (!string.IsNullOrEmpty(validationError))
            return Fail(validationError);

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
        if (!combatStateModel.IsPlayerTurn())
        {
            return Fail("Not player turn.");
        }

        if (player == null)
        {
            return Fail("Player not found.");
        }

        if (turnManager.availableDice <= 0 && action.definition.type != PlayerActionType.UseItem && action.definition.type != PlayerActionType.UseSkill)
        {
            return Fail("No dice available.");
        }

        if (player.heart <= 0 && player.body <= 0 && player.mind <= 0)
        {
            return Fail("No resources available.");
        }

        string costError = actionValidator.ValidateResourceCost(action, player);
        if (!string.IsNullOrEmpty(costError))
            return Fail(costError);

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
