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

    /// <summary>
    /// Tenta adicionar dado a uma ação de ataque.
    /// </summary>
    public ActionResult TryAddAttackDice(int diceToAdd)
    {
        string diceError = actionValidator.ValidateDiceAllocation(PlayerActionType.Attack, diceToAdd);
        if (!string.IsNullOrEmpty(diceError))
            return Fail(diceError);

        if (!turnManager.TryAddDiceToAction(PlayerActionType.Attack, diceToAdd))
            return Fail("Failed to allocate dice to attack.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack);
        return new ActionResult
        {
            success = true,
            message = $"Added {diceToAdd} to Attack. Total: {allocated}",
            diceSpent = 0
        };
    }

    /// <summary>
    /// Tenta remover dado de uma ação de ataque.
    /// </summary>
    public ActionResult TryRemoveAttackDice(int diceToRemove)
    {
        string removalError = actionValidator.ValidateDiceRemoval(PlayerActionType.Attack, diceToRemove);
        if (!string.IsNullOrEmpty(removalError))
            return Fail(removalError);

        if (!turnManager.TryRemoveDiceFromAction(PlayerActionType.Attack, diceToRemove))
            return Fail("Failed to remove dice from attack.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Attack);
        return new ActionResult
        {
            success = true,
            message = $"Removed {diceToRemove} from Attack. Total: {allocated}",
            diceSpent = 0
        };
    }

    /// <summary>
    /// Tenta adicionar dado a uma ação de investigação.
    /// </summary>
    public ActionResult TryAddInvestigateDice(int diceToAdd)
    {
        string diceError = actionValidator.ValidateDiceAllocation(PlayerActionType.Investigate, diceToAdd);
        if (!string.IsNullOrEmpty(diceError))
            return Fail(diceError);

        if (!turnManager.TryAddDiceToAction(PlayerActionType.Investigate, diceToAdd))
            return Fail("Failed to allocate dice to investigate.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate);
        return new ActionResult
        {
            success = true,
            message = $"Added {diceToAdd} to Investigate. Total: {allocated}",
            diceSpent = 0
        };
    }

    /// <summary>
    /// Tenta remover dado de uma ação de investigação.
    /// </summary>
    public ActionResult TryRemoveInvestigateDice(int diceToRemove)
    {
        string removalError = actionValidator.ValidateDiceRemoval(PlayerActionType.Investigate, diceToRemove);
        if (!string.IsNullOrEmpty(removalError))
            return Fail(removalError);

        if (!turnManager.TryRemoveDiceFromAction(PlayerActionType.Investigate, diceToRemove))
            return Fail("Failed to remove dice from investigate.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Investigate);
        return new ActionResult
        {
            success = true,
            message = $"Removed {diceToRemove} from Investigate. Total: {allocated}",
            diceSpent = 0
        };
    }

    /// <summary>
    /// Tenta adicionar dado a uma ação de defesa.
    /// </summary>
    public ActionResult TryAddDefendDice(int diceToAdd)
    {
        string diceError = actionValidator.ValidateDiceAllocation(PlayerActionType.Defend, diceToAdd);
        if (!string.IsNullOrEmpty(diceError))
            return Fail(diceError);

        if (!turnManager.TryAddDiceToAction(PlayerActionType.Defend, diceToAdd))
            return Fail("Failed to allocate dice to defend.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend);
        return new ActionResult
        {
            success = true,
            message = $"Added {diceToAdd} to Defend. Total: {allocated}",
            diceSpent = 0
        };
    }

    /// <summary>
    /// Tenta remover dado de uma ação de defesa.
    /// </summary>
    public ActionResult TryRemoveDefendDice(int diceToRemove)
    {
        string removalError = actionValidator.ValidateDiceRemoval(PlayerActionType.Defend, diceToRemove);
        if (!string.IsNullOrEmpty(removalError))
            return Fail(removalError);

        if (!turnManager.TryRemoveDiceFromAction(PlayerActionType.Defend, diceToRemove))
            return Fail("Failed to remove dice from defend.");

        int allocated = turnManager.GetAllocatedDiceForAction(PlayerActionType.Defend);
        return new ActionResult
        {
            success = true,
            message = $"Removed {diceToRemove} from Defend. Total: {allocated}",
            diceSpent = 0
        };
    }

    public ActionResult HandleRecharge(CombatBattlerModel player, bool boosted)
    {
        // Validar que é ação de defesa
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
        // Validar ação primária
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
        // Validar ação primária
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
        // Validar ação primária
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
        // Validar ação secundária
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
        // Validar ação secundária
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
        // Validar que pode terminar turno
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

        // Validar custos de recursos
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
