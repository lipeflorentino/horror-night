using UnityEngine;

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

    public ActionResult Resolve(ActionInstance action, CombatContext context)
    {
        if (action?.definition == null || context?.Actor == null)
        {
            return new ActionResult { success = false, message = "Invalid action or context." };
        }

        return action.definition.type switch
        {
            PlayerActionType.Attack => ResolvePlayerAttack(action, context),
            PlayerActionType.Defend => ResolvePlayerDefend(action, context),
            PlayerActionType.Investigate => ResolvePlayerInvestigate(action, context),
            PlayerActionType.UseItem => ResolveUseItem(action, context),
            PlayerActionType.UseSkill => ResolveUseSkill(action, context),
            PlayerActionType.Flee => ResolveFlee(action.allocatedDice),
            PlayerActionType.EndTurn => new ActionResult { success = true, message = "Turn ended." },
            _ => new ActionResult { success = false, message = "Unknown action type." }
        };
    }

    private ActionResult ResolvePlayerAttack(ActionInstance action, CombatContext context)
    {
        int roll = diceService.RollD6();
        int totalDamage = context.Actor.attack + roll + action.allocatedDice;
        int finalDamage = Mathf.Max(1, totalDamage - context.Target.defense);

        context.Target.TakeDamage(finalDamage);

        return new ActionResult
        {
            success = true,
            roll = roll,
            damage = finalDamage,
            diceSpent = 1,
            message = $"Attack deals {finalDamage} damage."
        };
    }

    private ActionResult ResolvePlayerDefend(ActionInstance action, CombatContext context)
    {
        int recovery = 1 + action.allocatedDice;
        context.Actor.RecoverResources(recovery);

        return new ActionResult
        {
            success = true,
            damage = recovery,
            diceSpent = 1,
            message = $"Defend recovered {recovery} resource(s)."
        };
    }

    private ActionResult ResolvePlayerInvestigate(ActionInstance action, CombatContext context)
    {
        int roll = diceService.RollD6();
        int investigateValue = roll + action.allocatedDice;

        string message;
        if (investigateValue >= 5)
        {
            message = "Full information discovered.";
        }
        else if (investigateValue >= 3)
        {
            message = "Partial information discovered.";
        }
        else
        {
            message = "No useful clues found.";
        }

        return new ActionResult
        {
            success = investigateValue >= 3,
            roll = roll,
            damage = 0,
            diceSpent = 1,
            message = message
        };
    }

    private ActionResult ResolveUseItem(ActionInstance action, CombatContext context)
    {
        int healAmount = 2;
        context.Actor.RecoverResources(healAmount);

        return new ActionResult
        {
            success = true,
            damage = healAmount,
            message = $"Item #{action.itemId} used and consumed."
        };
    }

    private ActionResult ResolveUseSkill(ActionInstance action, CombatContext context)
    {
        int roll = diceService.RollD6();
        int skillDamage = context.Actor.attack + 2 + roll;
        int finalDamage = Mathf.Max(1, skillDamage - context.Target.defense);

        context.Target.TakeDamage(finalDamage);

        return new ActionResult
        {
            success = true,
            roll = roll,
            damage = finalDamage,
            diceSpent = 1,
            message = $"Skill #{action.skillId} dealt {finalDamage} damage."
        };
    }

}
