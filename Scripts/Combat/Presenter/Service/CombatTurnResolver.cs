using System.Collections.Generic;

public class CombatTurnResolver
{
    private readonly CombatResolutionService combatResolutionService;
    private readonly DiceService diceService;

    public CombatTurnResolver(CombatResolutionService combatResolutionService, DiceService diceService)
    {
        this.combatResolutionService = combatResolutionService;
        this.diceService = diceService;
    }

    public List<ActionResult> ResolvePlayerTurn(
        CombatBattlerModel player,
        CombatBattlerModel enemy,
        IReadOnlyList<ActionInstance> actions)
    {
        List<ActionResult> results = new List<ActionResult>();

        if (player == null || enemy == null || actions == null)
        {
            return results;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            ActionInstance action = actions[i];
            results.Add(ResolveAction(player, enemy, action));
        }

        return results;
    }

    private ActionResult ResolveAction(CombatBattlerModel actor, CombatBattlerModel target, ActionInstance action)
    {
        if (action == null || action.definition == null)
        {
            return new ActionResult { success = false, message = "Invalid action." };
        }

        switch (action.definition.type)
        {
            case PlayerActionType.Attack:
            {
                int roll = diceService.RollD6();
                int rawDamage = actor.attack + roll;
                int appliedDamage = combatResolutionService.ApplyDamage(target, DamageCalculator.CalculateDamage(rawDamage, 0, target.defense));
                return new ActionResult { success = true, roll = roll, damage = appliedDamage, message = $"Attack dealt {appliedDamage}." };
            }
            case PlayerActionType.Defend:
                combatResolutionService.ApplyRecovery(actor, 1);
                return new ActionResult { success = true, message = "Defend recovered 1 resource." };
            case PlayerActionType.Investigate:
            {
                int roll = diceService.RollD6();
                string info = roll >= 5 ? "full info" : roll >= 3 ? "partial info" : "no useful info";
                return new ActionResult { success = roll >= 3, roll = roll, message = $"Investigate: {info}." };
            }
            case PlayerActionType.UseItem:
                combatResolutionService.ApplyRecovery(actor, 2);
                return new ActionResult { success = true, message = $"Item {action.itemId} used and consumed." };
            case PlayerActionType.UseSkill:
            {
                int roll = diceService.RollD6();
                int skillDamage = actor.attack + 2 + roll;
                int appliedDamage = combatResolutionService.ApplyDamage(target, DamageCalculator.CalculateDamage(skillDamage, 0, target.defense));
                return new ActionResult { success = true, roll = roll, damage = appliedDamage, message = $"Skill {action.skillId} dealt {appliedDamage}." };
            }
            case PlayerActionType.EndTurn:
                return new ActionResult { success = true, message = "Turn ended." };
            default:
                return new ActionResult { success = false, message = "Unknown action." };
        }
    }
}
