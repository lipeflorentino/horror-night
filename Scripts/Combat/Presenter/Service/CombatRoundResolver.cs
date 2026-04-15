using UnityEngine;

public class CombatRoundResolver
{
    private readonly DiceService diceService;

    public CombatRoundResolver(DiceService diceService)
    {
        this.diceService = diceService;
    }

    public RoundResolutionResult ResolveAttackVsDefense(ActionInstance attackAction, ActionInstance defenseAction, CombatBattlerModel attacker, CombatBattlerModel defender)
    {
        int attackRoll = diceService.RollD6();
        int defenseRoll = diceService.RollD6();

        int attackTotal = attacker.attack + attackRoll + attackAction.allocatedDice;
        int defenseTotal = defender.defense + defenseRoll + defenseAction.allocatedDice;

        int damageReduction = Mathf.Max(0, defenseTotal - attackTotal);
        int baseDamage = attacker.attack + attackRoll;
        int finalDamage = Mathf.Max(1, baseDamage - damageReduction);

        bool attackSucceeded = attackTotal > defenseTotal;

        return new RoundResolutionResult
        {
            attackSucceeded = attackSucceeded,
            attackTotal = attackTotal,
            defenseTotal = defenseTotal,
            finalDamage = finalDamage,
            damageReduced = damageReduction,
            attackRoll = attackRoll,
            defenseRoll = defenseRoll,
            message = attackSucceeded 
                ? $"Attack succeeded! {finalDamage} damage dealt (Defense reduced {damageReduction} damage)."
                : $"Attack blocked! {damageReduction} damage prevented."
        };
    }

    public RoundResolutionResult ResolveAttackVsFlee(ActionInstance attackAction, ActionInstance fleeAction, CombatBattlerModel attacker, CombatBattlerModel defender)
    {
        int attackRoll = diceService.RollD6();
        int fleeRoll = diceService.RollD6();

        int attackTotal = attacker.attack + attackRoll + attackAction.allocatedDice;
        int fleeChance = 50 + (fleeAction.allocatedDice * 10) + fleeRoll;

        int finalDamage = Mathf.Max(1, attacker.attack + attackRoll - (fleeChance > 75 ? 5 : 0));
        bool fleeSucceeded = fleeChance > 75;

        return new RoundResolutionResult
        {
            attackSucceeded = !fleeSucceeded,
            attackTotal = attackTotal,
            defenseTotal = fleeChance,
            finalDamage = fleeSucceeded ? 0 : finalDamage,
            damageReduced = fleeSucceeded ? finalDamage : 0,
            attackRoll = attackRoll,
            defenseRoll = fleeRoll,
            message = fleeSucceeded 
                ? "Combat ended! Player escaped successfully."
                : $"Escape failed! Attack still connects ({finalDamage} damage)."
        };
    }
}

public class RoundResolutionResult
{
    public bool attackSucceeded;
    public int attackTotal;
    public int defenseTotal;
    public int finalDamage;
    public int damageReduced;
    public int attackRoll;
    public int defenseRoll;
    public string message;
}
