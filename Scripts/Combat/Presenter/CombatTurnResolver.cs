using System.Collections.Generic;

public class CombatTurnResolver
{
    private readonly CombatResolutionService combatResolutionService;

    public readonly List<string> lastResolutionLog = new List<string>();

    public CombatTurnResolver(CombatResolutionService combatResolutionService)
    {
        this.combatResolutionService = combatResolutionService;
    }

    public void ResolvePlayerTurn(
        CombatBattlerModel player,
        CombatBattlerModel enemy,
        IReadOnlyList<CombatActionData> actions)
    {
        lastResolutionLog.Clear();

        if (actions == null)
        {
            return;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            CombatActionData action = actions[i];
            if (action == null)
            {
                continue;
            }

            ApplyAction(player, enemy, action);
        }
    }

    private void ApplyAction(CombatBattlerModel actor, CombatBattlerModel target, CombatActionData action)
    {
        switch (action.actionType)
        {
            case PlayerActionType.Attack:
            {
                int baseDamage = action.predictedValue.HasValue ? action.predictedValue.Value : actor.attack;
                int mitigatedDamage = DamageCalculator.CalculateDamage(baseDamage, 0, target.defense);
                int appliedDamage = combatResolutionService.ApplyDamage(target, mitigatedDamage);
                lastResolutionLog.Add($"Attack resolved for {appliedDamage} damage.");
                break;
            }

            case PlayerActionType.Investigate:
                lastResolutionLog.Add("Investigate resolved.");
                break;

            case PlayerActionType.UseItem:
                combatResolutionService.ApplyRecovery(actor, 1);
                lastResolutionLog.Add("UseItem resolved.");
                break;

            case PlayerActionType.UseSkill:
                combatResolutionService.ApplyRecovery(actor, 1);
                lastResolutionLog.Add("UseSkill resolved.");
                break;

            case PlayerActionType.EndTurn:
                lastResolutionLog.Add("EndTurn resolved.");
                break;
        }
    }
}
