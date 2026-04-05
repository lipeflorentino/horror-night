public class ActionResolverService
{
    private readonly DiceService diceService;

    public ActionResolverService(DiceService diceService)
    {
        this.diceService = diceService;
    }

    public ActionResult ResolveRecharge(bool boosted)
    {
        int baseRecovery = 1;
        int bonusRecovery = 0;
        int boostRoll = 0;

        if (boosted)
        {
            boostRoll = diceService.RollD6();

            if (boostRoll >= 6)
            {
                bonusRecovery = 2;
            }
            else if (boostRoll >= 3)
            {
                bonusRecovery = 1;
            }
        }

        int totalRecovery = baseRecovery + bonusRecovery;

        return new ActionResult
        {
            diceSpent = boosted ? 1 : 0,
            roll = boostRoll,
            success = true,
            damage = totalRecovery,
            message = $"Recovered {totalRecovery} resource(s)."
        };
    }

    public ActionResult ResolveFlee(int diceCount)
    {
        int attempts = diceCount < 0 ? 0 : diceCount;
        int highestRoll = 0;
        bool escaped = false;

        for (int i = 0; i < attempts; i++)
        {
            int roll = diceService.RollD6();
            if (roll > highestRoll)
            {
                highestRoll = roll;
            }

            if (roll >= 5)
            {
                escaped = true;
            }
        }

        return new ActionResult
        {
            diceSpent = attempts,
            roll = highestRoll,
            success = escaped,
            damage = 0,
            message = escaped ? "Flee succeeded." : "Flee failed."
        };
    }

    public ActionResult ResolveInvestigate()
    {
        int roll = diceService.RollD6();
        string message;

        if (roll <= 2)
        {
            message = "No useful clues found.";
        }
        else if (roll <= 5)
        {
            message = "Partial information discovered.";
        }
        else
        {
            message = "Full information discovered.";
        }

        return new ActionResult
        {
            diceSpent = 1,
            roll = roll,
            success = roll >= 3,
            damage = 0,
            message = message
        };
    }

    public ActionResult ResolveAttack(int baseAttack)
    {
        int safeBaseAttack = baseAttack < 0 ? 0 : baseAttack;
        int roll = diceService.RollD6();
        int bonusDamage;

        if (roll <= 2)
        {
            bonusDamage = 0;
        }
        else if (roll <= 5)
        {
            bonusDamage = roll;
        }
        else
        {
            bonusDamage = roll * 2;
        }

        int totalDamage = safeBaseAttack + bonusDamage;

        return new ActionResult
        {
            diceSpent = 1,
            roll = roll,
            success = true,
            damage = totalDamage,
            message = $"Attack deals {totalDamage} damage."
        };
    }

    public ActionResult ResolveDefense()
    {
        int roll = diceService.RollD6();
        int defenseBonus;

        if (roll <= 2)
        {
            defenseBonus = 1;
        }
        else if (roll <= 5)
        {
            defenseBonus = 2;
        }
        else
        {
            defenseBonus = 3;
        }

        return new ActionResult
        {
            diceSpent = 1,
            roll = roll,
            success = true,
            damage = defenseBonus,
            message = $"Temporary defense increased by {defenseBonus}."
        };
    }

}
