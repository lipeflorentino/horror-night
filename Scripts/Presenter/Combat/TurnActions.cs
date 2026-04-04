using UnityEngine;

public enum CombatActionIntensity
{
    Low,
    Normal,
    High
}

public struct TurnActionResult
{
    public int diceSpent;
    public int roll;
    public int damage;
    public int recoveredPerResource;
    public int revealedInfoLevel;
    public bool success;
    public string message;
}

public static class TurnActions
{
    public static int RollD6()
    {
        return Random.Range(1, 7);
    }

    public static TurnActionResult ResolveRecharge(bool boosted)
    {
        TurnActionResult result = new TurnActionResult
        {
            diceSpent = boosted ? 1 : 0,
            recoveredPerResource = 1,
            success = true,
            roll = 0,
            message = "Recarga básica: +1 Mente/Corpo/Coração."
        };

        if (!boosted)
            return result;

        int roll = RollD6();
        int bonus = roll switch
        {
            <= 2 => 0,
            <= 5 => 1,
            _ => 2
        };

        result.roll = roll;
        result.recoveredPerResource += bonus;
        result.message = bonus > 0
            ? $"Recarga potencializada ({roll}): +{result.recoveredPerResource} por recurso."
            : $"Recarga potencializada ({roll}): sem bônus extra.";

        return result;
    }

    public static TurnActionResult ResolveInvestigate()
    {
        int roll = RollD6();
        int infoLevel = roll switch
        {
            <= 2 => 0,
            <= 5 => 1,
            _ => 2
        };

        return new TurnActionResult
        {
            diceSpent = 1,
            roll = roll,
            revealedInfoLevel = infoLevel,
            success = infoLevel > 0,
            message = infoLevel switch
            {
                0 => "Investigação falhou: nenhuma informação descoberta.",
                1 => "Investigação parcial: uma informação descoberta.",
                _ => "Investigação crítica: todas as informações descobertas."
            }
        };
    }

    public static TurnActionResult ResolveFlee(int diceToRoll)
    {
        int best = 0;
        for (int i = 0; i < diceToRoll; i++)
            best = Mathf.Max(best, RollD6());

        bool success = best >= 5;

        return new TurnActionResult
        {
            diceSpent = Mathf.Max(1, diceToRoll),
            roll = best,
            success = success,
            message = success
                ? $"Fuga bem-sucedida com rolagem {best}."
                : $"Fuga falhou com melhor rolagem {best}."
        };
    }

    public static TurnActionResult ResolveActionRoll(CombatActionIntensity intensity)
    {
        int roll = RollD6();
        float multiplier = roll switch
        {
            <= 2 => 0.7f,
            <= 4 => 1f,
            _ => 1.35f
        };

        multiplier *= intensity switch
        {
            CombatActionIntensity.Low => 0.8f,
            CombatActionIntensity.Normal => 1f,
            _ => 1.25f
        };

        return new TurnActionResult
        {
            diceSpent = 1,
            roll = roll,
            success = true,
            damage = Mathf.Max(1, Mathf.RoundToInt(multiplier * 10f))
        };
    }
}
