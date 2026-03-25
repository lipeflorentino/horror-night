using UnityEngine;

public enum RollType
{
    Life,
    Physical,
    Mental
}

[System.Serializable]
public struct TurnManagerStats
{
    public int attack;
    public int defense;
    public int criticalHitChance;
    public int parryChance;
    public int fleeChance;
    public int instantKillChance;
    public int learnChance;

    public static TurnManagerStats BuildDefault(int life, int physical, int mental)
    {
        int scaledPhysicalChance = Mathf.Clamp((Mathf.Max(0, physical) - 1) * 10, 0, 95);
        int scaledMentalChance = Mathf.Clamp((Mathf.Max(0, mental) - 1) * 10, 0, 95);

        return new TurnManagerStats
        {
            attack = Mathf.Max(1, physical),
            defense = Mathf.Max(0, Mathf.RoundToInt((life + physical) * 0.25f)),
            criticalHitChance = Mathf.Clamp(Mathf.RoundToInt(mental * 0.8f), 0, 60),
            parryChance = Mathf.Clamp(scaledPhysicalChance, 0, 90),
            fleeChance = Mathf.Clamp(scaledPhysicalChance, 0, 95),
            instantKillChance = Mathf.Clamp(Mathf.RoundToInt(scaledMentalChance * 0.25f), 1, 35),
            learnChance = Mathf.Clamp(Mathf.RoundToInt(scaledMentalChance * 0.75f), 5, 95)
        };
    }

    public void Normalize()
    {
        attack = Mathf.Max(1, attack);
        defense = Mathf.Max(0, defense);
        criticalHitChance = Mathf.Clamp(criticalHitChance, 0, 100);
        parryChance = Mathf.Clamp(parryChance, 0, 100);
        fleeChance = Mathf.Clamp(fleeChance, 0, 100);
        instantKillChance = Mathf.Clamp(instantKillChance, 0, 100);
        learnChance = Mathf.Clamp(learnChance, 0, 100);
    }
}
