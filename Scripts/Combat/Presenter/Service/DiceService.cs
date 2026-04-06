using UnityEngine;

public class DiceService
{
    public int RollD6()
    {
        return Random.Range(1, 7);
    }

    public int[] RollMultiple(int count)
    {
        if (count <= 0)
        {
            return System.Array.Empty<int>();
        }

        int[] rolls = new int[count];

        for (int i = 0; i < count; i++)
        {
            rolls[i] = RollD6();
        }

        return rolls;
    }

    public int TruncateDice()
    {
        int roll;

        do
        {
            roll = RollD6();
        }
        while (roll < 3);

        return roll;
    }
}
