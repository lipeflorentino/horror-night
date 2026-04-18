using System;

public class ActionResolverService
{
    public int Resolve(ActionInstance attack, ActionInstance defense)
    {
        float attackPower = CalculatePower(attack);
        float defensePower = CalculatePower(defense);

        int damage = (int)(attackPower - defensePower);
        if (damage < 0) damage = 0;

        Console.WriteLine($"[Resolve] AttackPower: {attackPower} | DefensePower: {defensePower} | Damage: {damage}");

        return damage;
    }

    private float CalculatePower(ActionInstance action)
    {
        float multiplier = GetMultiplier(action.Dice.Tier);
        return action.Definition.BasePower * multiplier;
    }

    private float GetMultiplier(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => 0.5f,
            DiceTier.Medium => 1f,
            DiceTier.High => 1.5f,
            _ => 1f,
        };
    }
}