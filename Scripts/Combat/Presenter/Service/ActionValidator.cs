using UnityEngine;

/// <summary>
/// Valida se uma ação pode ser executada conforme as regras do combate.
/// Verifica conflitos de ações, alocação de dados, custos de recursos, etc.
/// </summary>
public class ActionValidator
{
    private readonly TurnManager turnManager;
    private readonly CombatStateModel combatStateModel;

    public ActionValidator(TurnManager turnManager, CombatStateModel combatStateModel)
    {
        this.turnManager = turnManager;
        this.combatStateModel = combatStateModel;
    }

    /// <summary>
    /// Valida se uma ação primária pode ser executada.
    /// Retorna uma descrição do erro se houver, ou string vazia se for válido.
    /// </summary>
    public string ValidatePrimaryAction(PlayerActionType actionType)
    {
        // Apenas durante turno do jogador
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        // Validar que é ação primária
        if (actionType != PlayerActionType.Attack && 
            actionType != PlayerActionType.Investigate && 
            actionType != PlayerActionType.Defend)
            return "Invalid primary action.";

        // Se já foi definida uma ação primária diferente, não permite mudar
        if (turnManager.HasPrimaryAction())
        {
            PlayerActionType? current = turnManager.GetPrimaryAction();
            if (current.HasValue && current.Value != actionType)
                return $"Action already chosen: {current.Value}. Cannot change to {actionType}.";
        }

        // Validar dados disponíveis
        if (turnManager.availableDice <= 0)
            return "No dice available.";

        return "";
    }

    /// <summary>
    /// Valida se may adicionar mais dados à ação.
    /// Retorna true se pode adicionar, false caso contrário.
    /// </summary>
    public string ValidateDiceAllocation(PlayerActionType actionType, int diceToAdd)
    {
        int amountSafe = Mathf.Max(0, diceToAdd);

        // Validar que não tenta alocar mais que disponível
        if (amountSafe > turnManager.availableDice)
            return $"Cannot allocate {amountSafe} dice. Only {turnManager.availableDice} available.";

        // Validar que a ação foi definida como primária
        if (!turnManager.IsPrimaryAction(actionType))
            return $"{actionType} not selected as primary action.";

        return "";
    }

    /// <summary>
    /// Valida se pode remover dados da ação.
    /// Retorna true se pode remover.
    /// </summary>
    public string ValidateDiceRemoval(PlayerActionType actionType, int diceToRemove)
    {
        int amountSafe = Mathf.Max(0, diceToRemove);
        int currentAllocated = turnManager.GetAllocatedDiceForAction(actionType);

        // Validar que não tenta remover mais que foi alocado
        if (amountSafe > currentAllocated)
            return $"Cannot remove {amountSafe} dice from {actionType}. Only {currentAllocated} allocated.";

        // Validar que a ação foi definida como primária
        if (!turnManager.IsPrimaryAction(actionType))
            return $"{actionType} not selected as primary action.";

        return "";
    }

    /// <summary>
    /// Valida se pode usar uma ação secundária (UseItem ou UseSkill).
    /// Ações secundárias podem ser usadas junto com ações primárias.
    /// </summary>
    public string ValidateSecondaryAction(PlayerActionType actionType)
    {
        // Apenas durante turno do jogador
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        // Validar que é ação secundária
        if (actionType != PlayerActionType.UseItem && actionType != PlayerActionType.UseSkill)
            return "Invalid secondary action.";

        // Validar que não foi usada outra ação secundária neste turno
        if (turnManager.HasUsedSecondaryAction())
            return "Secondary action already used this turn.";

        return "";
    }

    /// <summary>
    /// Valida se pode usar a ação EndTurn.
    /// </summary>
    public string ValidateEndTurn()
    {
        if (!combatStateModel.IsPlayerTurn())
            return "It is not your turn.";

        // O jogador deve ter selecionado uma ação primária antes de terminar
        if (!turnManager.HasPrimaryAction())
            return "Must select an action before ending turn.";

        return "";
    }

    /// <summary>
    /// Valida custos totais de recursos de uma ação.
    /// </summary>
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

    /// <summary>
    /// Valida todas as condições de uma ação primária antes de executar.
    /// Retorna string vazia se válido, ou descrição do erro.
    /// </summary>
    public string FullValidatePrimaryAction(PlayerActionType actionType, ActionInstance action, CombatBattlerModel player)
    {
        // Validar ação primária
        string primaryError = ValidatePrimaryAction(actionType);
        if (!string.IsNullOrEmpty(primaryError))
            return primaryError;

        // Validar custos
        string costError = ValidateResourceCost(action, player);
        if (!string.IsNullOrEmpty(costError))
            return costError;

        return "";
    }
}
