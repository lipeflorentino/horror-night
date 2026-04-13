using System;
using UnityEngine;

public class TurnManager
{
    private PlayerActionType? primaryAction;
    private int allocatedDiceAttack;
    private int allocatedDiceInvestigate;
    private int allocatedDiceDefend;
    private bool hasUsedSecondaryAction;

    public int availableDice;
    public int availableHeart;
    public int availableBody;
    public int availableMind;
    public CombatActionQueue actionQueue;

    public event Action<PlayerActionType> OnPrimaryActionSet;
    public event Action OnPrimaryActionReset;

    public void StartTurn(int diceAmount = 3, int heartAmount = 0, int bodyAmount = 0, int mindAmount = 0)
    {
        availableDice = Mathf.Max(0, diceAmount);
        availableHeart = Mathf.Max(0, heartAmount);
        availableBody = Mathf.Max(0, bodyAmount);
        availableMind = Mathf.Max(0, mindAmount);
        actionQueue = new CombatActionQueue();
        
        ResetActionState();
    }

    /// <summary>
    /// Define a ação primária do turno. Retorna true se bem-sucedido.
    /// Ações primárias são: Attack, Investigate, Defend
    /// Apenas uma ação primária é permitida por turno.
    /// </summary>
    public bool SetPrimaryAction(PlayerActionType actionType)
    {
        // Validar que é uma ação primária
        if (actionType != PlayerActionType.Attack && 
            actionType != PlayerActionType.Investigate && 
            actionType != PlayerActionType.Defend)
        {
            return false;
        }

        // Se já foi definida uma ação primária diferente, retornar false
        if (primaryAction.HasValue && primaryAction.Value != actionType)
        {
            return false;
        }

        primaryAction = actionType;
        OnPrimaryActionSet?.Invoke(actionType);
        return true;
    }

    /// <summary>
    /// Retorna a ação primária atual. Null se nenhuma foi definida.
    /// </summary>
    public PlayerActionType? GetPrimaryAction()
    {
        return primaryAction;
    }

    /// <summary>
    /// Verifica se uma ação primária foi definida.
    /// </summary>
    public bool HasPrimaryAction()
    {
        return primaryAction.HasValue;
    }

    /// <summary>
    /// Verifica se a ação informada é a ação primária atual.
    /// </summary>
    public bool IsPrimaryAction(PlayerActionType actionType)
    {
        return primaryAction.HasValue && primaryAction.Value == actionType;
    }

    /// <summary>
    /// Adiciona dados alocados à ação específica. Retorna true se bem-sucedido.
    /// Valida limites de dados disponíveis.
    /// </summary>
    public bool TryAddDiceToAction(PlayerActionType actionType, int amountToAdd)
    {
        int amountSafe = Mathf.Max(0, amountToAdd);
        int currentAllocated = GetAllocatedDiceForAction(actionType);
        int newAmount = currentAllocated + amountSafe;

        // Não pode alocar mais dados que os disponíveis
        if (newAmount > availableDice)
        {
            return false;
        }

        switch (actionType)
        {
            case PlayerActionType.Attack:
                allocatedDiceAttack = newAmount;
                break;
            case PlayerActionType.Investigate:
                allocatedDiceInvestigate = newAmount;
                break;
            case PlayerActionType.Defend:
                allocatedDiceDefend = newAmount;
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove dados alocados da ação específica. Retorna true se bem-sucedido.
    /// Não permite valores negativos.
    /// </summary>
    public bool TryRemoveDiceFromAction(PlayerActionType actionType, int amountToRemove)
    {
        int amountSafe = Mathf.Max(0, amountToRemove);
        int currentAllocated = GetAllocatedDiceForAction(actionType);
        int newAmount = Mathf.Max(0, currentAllocated - amountSafe);

        switch (actionType)
        {
            case PlayerActionType.Attack:
                allocatedDiceAttack = newAmount;
                break;
            case PlayerActionType.Investigate:
                allocatedDiceInvestigate = newAmount;
                break;
            case PlayerActionType.Defend:
                allocatedDiceDefend = newAmount;
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Retorna a quantidade de dados alocados para uma ação específica.
    /// </summary>
    public int GetAllocatedDiceForAction(PlayerActionType actionType)
    {
        return actionType switch
        {
            PlayerActionType.Attack => allocatedDiceAttack,
            PlayerActionType.Investigate => allocatedDiceInvestigate,
            PlayerActionType.Defend => allocatedDiceDefend,
            _ => 0
        };
    }

    /// <summary>
    /// Marca que uma ação secundária foi usada (UseItem ou UseSkill).
    /// Retorna true se foi bem-sucedido.
    /// </summary>
    public bool TryUseSecondaryAction()
    {
        if (hasUsedSecondaryAction)
            return false;

        hasUsedSecondaryAction = true;
        return true;
    }

    /// <summary>
    /// Verifica se uma ação secundária já foi usada neste turno.
    /// </summary>
    public bool HasUsedSecondaryAction()
    {
        return hasUsedSecondaryAction;
    }

    public bool TrySpendDice(int amount)
    {
        int spendAmount = Mathf.Max(0, amount);

        if (spendAmount > availableDice)
        {
            return false;
        }

        availableDice -= spendAmount;
        return true;
    }

    public bool TrySpendResources(int heartCost, int bodyCost, int mindCost)
    {
        int safeHeart = Mathf.Max(0, heartCost);
        int safeBody = Mathf.Max(0, bodyCost);
        int safeMind = Mathf.Max(0, mindCost);

        if (safeHeart > availableHeart || safeBody > availableBody || safeMind > availableMind)
        {
            return false;
        }

        availableHeart -= safeHeart;
        availableBody -= safeBody;
        availableMind -= safeMind;
        return true;
    }

    public bool CanAfford(ActionInstance action)
    {
        if (action == null || action.definition == null)
        {
            return false;
        }

        int totalDice = action.TotalDiceCost();
        int totalHeart = action.definition.heartCost + action.allocatedHeart;
        int totalBody = action.definition.bodyCost + action.allocatedBody;
        int totalMind = action.definition.mindCost + action.allocatedMind;

        return totalDice <= availableDice
            && totalHeart <= availableHeart
            && totalBody <= availableBody
            && totalMind <= availableMind;
    }

    public void ResetDice()
    {
        availableDice = 0;
        availableHeart = 0;
        availableBody = 0;
        availableMind = 0;
    }

    /// <summary>
    /// Reseta o estado de ações do turno (mas não os recursos).
    /// Chamado no início de cada novo turno.
    /// </summary>
    private void ResetActionState()
    {
        primaryAction = null;
        allocatedDiceAttack = 0;
        allocatedDiceInvestigate = 0;
        allocatedDiceDefend = 0;
        hasUsedSecondaryAction = false;
        OnPrimaryActionReset?.Invoke();
    }

    public void QueueAction(ActionInstance action)
    {
        actionQueue ??= new CombatActionQueue();
        actionQueue.Add(action);
    }
}

