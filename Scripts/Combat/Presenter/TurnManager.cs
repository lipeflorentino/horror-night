using UnityEngine;

public class TurnManager
{
    public int availableDice;

    public void StartTurn(int diceAmount = 3)
    {
        availableDice = Mathf.Max(0, diceAmount);
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

    public void ResetDice()
    {
        availableDice = 0;
    }
}
