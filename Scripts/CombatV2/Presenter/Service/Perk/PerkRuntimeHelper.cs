using System.Collections.Generic;
using UnityEngine;

public static class PerkRuntimeHelper
{
    public static int ResolveDuration(int requestedDuration, int defaultDuration, int currentDuration)
    {
        int newDuration = requestedDuration >= 0 ? requestedDuration : defaultDuration;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }

    public static List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
    {
        List<PerkRuntimeInstance> perks = new();
        HashSet<string> addedKeys = new();

        if (battler == null)
            return perks;

        List<PerkRuntimeInstance> battlerPerks = battler.GetEffectivePerks();
        for (int i = 0; i < battlerPerks.Count; i++)
            AddEffectivePerk(perks, addedKeys, battlerPerks[i]);

        return perks;
    }

    private static void AddEffectivePerk(List<PerkRuntimeInstance> perks, HashSet<string> addedKeys, PerkRuntimeInstance perk)
    {
        if (perk?.Definition == null)
            return;

        string key = GetEffectivePerkKey(perk);
        if (addedKeys.Add(key))
            perks.Add(perk);
    }

    private static string GetEffectivePerkKey(PerkRuntimeInstance perk)
    {
        string perkId = perk.Definition?.Id ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(perk.SourceTrickInstanceId))
            return $"trick:{perk.SourceTrickInstanceId}:{perkId}";

        return $"direct:{perkId}";
    }

    public static float ApplyModifier(float current, PerkOperation operation, float value, int stacks, ICombatContext context)
    {
        if (operation == PerkOperation.Override)
            return value;

        if (operation == PerkOperation.Multiply)
        {
            float multiplier = 1f;
            for (int i = 0; i < stacks; i++)
                multiplier *= value;
            return current * multiplier;
        }

        if (operation == PerkOperation.AddPer10Charges)
            return current + value * (stacks / 10);

        if (operation == PerkOperation.AddPer5Charges)
            return current + value * (stacks / 5);

        if (operation == PerkOperation.AddPer3Charges)
            return current + value * (stacks / 3);
            
        if (operation == PerkOperation.AddPerCharge)
            return current + value * stacks;

        if (operation == PerkOperation.AddPerDamage && context is ActionResolutionContext actionContext)
            return current + (value * actionContext.Damage * stacks); 

        return current + value * stacks;
    }

    public static bool IsRoleMatch(Battler owner, CombatRollContext context, PerkRole role)
    {
        return IsRoleMatch(owner, context.ToActionContext(), role); 
        // Nota: Garanta que ToActionContext() ou a lógica interna equivalente esteja acessível.
        // Ou copie a implementação direta que está no PerkTriggerEvaluator:
        /*
        return role switch
        {
            PerkRole.OwnerAsActor => owner == context.Actor,
            PerkRole.OwnerAsOpponent => owner == context.Opponent,
            PerkRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
        */
    }

    public static bool IsRoleMatch(Battler owner, CombatActionContext context, PerkRole role)
    {
        return role switch
        {
            PerkRole.OwnerAsActor => owner == context.Actor,
            PerkRole.OwnerAsOpponent => owner == context.Opponent,
            PerkRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
    }

    public static bool IsRoleMatch(Battler owner, ActionResolutionContext context, PerkRole role)
    {
        return role switch
        {
            PerkRole.OwnerAsActor => owner == context.Actor,
            PerkRole.OwnerAsOpponent => owner == context.Opponent,
            PerkRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            PerkRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
    }
}
