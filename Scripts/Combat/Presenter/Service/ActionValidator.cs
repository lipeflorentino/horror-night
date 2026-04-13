using UnityEngine;

public class ActionValidator
{
    private readonly TurnManager turnManager;
    private readonly CombatStateModel combatStateModel;

    public ActionValidator(TurnManager turnManager, CombatStateModel combatStateModel)
    {
        this.turnManager = turnManager;
        this.combatStateModel = combatStateModel;
    }
    public string ValidatePrimaryAction(PlayerActionType actionType)
    {
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        if (actionType != PlayerActionType.Attack && 
            actionType != PlayerActionType.Investigate && 
            actionType != PlayerActionType.Defend)
            return "Invalid primary action.";

        if (turnManager.HasPrimaryAction())
        {
            PlayerActionType? current = turnManager.GetPrimaryAction();
            if (current.HasValue && current.Value != actionType)
                return $"Action already chosen: {current.Value}. Cannot change to {actionType}.";
        }

        if (turnManager.availableDice <= 0)
            return "No dice available.";

        return "";
    }

    public string ValidateDiceAllocation(PlayerActionType actionType, int diceToAdd)
    {
        int amountSafe = Mathf.Max(0, diceToAdd);

        if (amountSafe > turnManager.availableDice)
            return $"Cannot allocate {amountSafe} dice. Only {turnManager.availableDice} available.";

        if (!turnManager.IsPrimaryAction(actionType))
            return $"{actionType} not selected as primary action.";

        return "";
    }

    public string ValidateDiceRemoval(PlayerActionType actionType, int diceToRemove)
    {
        int amountSafe = Mathf.Max(0, diceToRemove);
        int currentAllocated = turnManager.GetAllocatedDiceForAction(actionType);

        if (amountSafe > currentAllocated)
            return $"Cannot remove {amountSafe} dice from {actionType}. Only {currentAllocated} allocated.";

        if (!turnManager.IsPrimaryAction(actionType))
            return $"{actionType} not selected as primary action.";

        return "";
    }

    public string ValidateSecondaryAction(PlayerActionType actionType)
    {
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        if (actionType != PlayerActionType.UseItem && actionType != PlayerActionType.UseSkill)
            return "Invalid secondary action.";

        if (turnManager.HasUsedSecondaryAction())
            return "Secondary action already used this turn.";

        return "";
    }

    public string ValidateEndTurn()
    {
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        if (!turnManager.HasPrimaryAction())
            return "Must select an action before ending turn.";

        return "";
    }

    public string ValidateResourceCost(ActionInstance action, CombatBattlerModel player)
    {
        if (action == null || action.definition == null)
            return "Invalid action.";

        if (player == null)
            return "Player not found.";

        int totalHeart = action.definition.heartCost + action.allocatedHeart;
        int totalBody = action.definition.bodyCost + action.allocatedBody;
        int totalMind = action.definition.mindCost + action.allocatedMind;

        if (totalHeart > turnManager.availableHeart)
            return $"Not enough Heart. Need {totalHeart}, have {turnManager.availableHeart}.";

        if (totalBody > turnManager.availableBody)
            return $"Not enough Body. Need {totalBody}, have {turnManager.availableBody}.";

        if (totalMind > turnManager.availableMind)
            return $"Not enough Mind. Need {totalMind}, have {turnManager.availableMind}.";

        return "";
    }

    public string FullValidatePrimaryAction(PlayerActionType actionType, ActionInstance action, CombatBattlerModel player)
    {
        string primaryError = ValidatePrimaryAction(actionType);
        if (!string.IsNullOrEmpty(primaryError))
            return primaryError;

        string costError = ValidateResourceCost(action, player);
        if (!string.IsNullOrEmpty(costError))
            return costError;

        return "";
    }
}
