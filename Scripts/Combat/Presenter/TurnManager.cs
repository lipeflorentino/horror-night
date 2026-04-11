using UnityEngine;

public class TurnManager
{
    public int availableDice;
    public CombatActionQueue actionQueue;

    public void StartTurn(int diceAmount = 3)
    {
        availableDice = Mathf.Max(0, diceAmount);
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

    public void QueueAction(CombatActionData action)
    {
        actionQueue.Add(action);
    }

    public void ResetDice()
    {
        availableDice = 0;
    }
}
