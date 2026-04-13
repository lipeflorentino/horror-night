using UnityEngine;

public class TurnManager
{
    public int availableDice;
    public int availableHeart;
    public int availableBody;
    public int availableMind;
    public CombatActionQueue actionQueue;

    public void StartTurn(int diceAmount = 3, int heartAmount = 0, int bodyAmount = 0, int mindAmount = 0)
    {
        availableDice = Mathf.Max(0, diceAmount);
        availableHeart = Mathf.Max(0, heartAmount);
        availableBody = Mathf.Max(0, bodyAmount);
        availableMind = Mathf.Max(0, mindAmount);
        actionQueue = new CombatActionQueue();
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

    public void QueueAction(ActionInstance action)
    {
        actionQueue ??= new CombatActionQueue();
        actionQueue.Add(action);
    }
}
