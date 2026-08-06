using System.Collections.Generic;

public static class DiceRuntimeHelper
{
    public static int SumDice(List<DiceResult> dices)
    {
        int sum = 0;
        if (dices == null)
            return sum;

        for (int i = 0; i < dices.Count; i++)
            sum += dices[i]?.Value ?? 0;

        return sum;
    }
}