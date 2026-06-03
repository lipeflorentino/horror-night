using System.Collections.Generic;
using UnityEngine;

public class PerkService
{
    private readonly PerkDatabase database;

    public PerkService(PerkDatabase database = null)
    {
        this.database = database != null ? database : PerkDatabase.GetOrCreateRuntimeDatabase();
        this.database.EnsureLoaded();
    }

    public PerkDefinition GetPerkDefinition(string perkId)
    {
        return database.GetById(perkId);
    }

    public void ApplyPerk(Battler target, string perkId, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        ApplyPerk(target, GetPerkDefinition(perkId), source, durationTurns, stacks);
    }

    public void ApplyPerk(Battler target, PerkDefinition definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        if (target == null || definition == null)
            return;

        int maxStacks = Mathf.Max(1, definition.MaxStacks);
        PerkRuntimeInstance existing = target.Perks.Find(perk => perk.Definition == definition || perk.Definition?.Id == definition.Id);
        if (existing == null)
        {
            target.Perks.Add(new PerkRuntimeInstance(definition, source, durationTurns, Mathf.Clamp(stacks, 1, maxStacks)));
            return;
        }

        switch (definition.StackMode)
        {
            case BattlerStateStackMode.AddStack:
                existing.Stacks = Mathf.Clamp(existing.Stacks + Mathf.Max(1, stacks), 1, maxStacks);
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
            case BattlerStateStackMode.Replace:
                existing.Source = source;
                existing.Stacks = Mathf.Clamp(stacks, 1, maxStacks);
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
            default:
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
        }
    }

    public void TickTurnEnd(Battler battler)
    {
        if (battler == null || battler.Perks.Count == 0)
            return;

        for (int i = battler.Perks.Count - 1; i >= 0; i--)
        {
            PerkRuntimeInstance perk = battler.Perks[i];
            if (perk == null || perk.Definition == null)
            {
                battler.Perks.RemoveAt(i);
                continue;
            }

            if (perk.RemainingTurns < 0)
                continue;

            perk.RemainingTurns--;
            if (perk.RemainingTurns <= 0)
                battler.Perks.RemoveAt(i);
        }
    }

    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context)
    {
        float value = 0f;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.ExtraDice, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetMinimumRollValue(Battler actor, Battler opponent, CombatRollContext context, int currentMinValue)
    {
        float minValue = currentMinValue;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.MinRollPercent, ref minValue, context.MaxValue);
        return Mathf.Clamp(Mathf.CeilToInt(minValue), 1, Mathf.Max(1, context.MaxValue));
    }

    public float GetPowerMultiplier(float baseMultiplier, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action?.PowerDice == null)
            return baseMultiplier;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        float multiplier = baseMultiplier;
        ApplyDiceModifiers(actor, opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, PerkModifierTarget.PowerMultiplier, ref multiplier);
        return Mathf.Max(0f, multiplier);
    }

    public int ApplyDamageModifiers(int damage, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (damage <= 0 || action == null)
            return damage;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        float modifiedDamage = damage;
        ApplyDiceModifiers(actor, opponent, actionContext, action.PowerDice, PerkTrigger.AfterResolve, PerkModifierTarget.DamagePercent, ref modifiedDamage);
        ApplyDiceModifiers(actor, opponent, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, PerkModifierTarget.DamagePercent, ref modifiedDamage);
        return Mathf.Max(0, Mathf.RoundToInt(modifiedDamage));
    }

    private void ApplyRollModifiers(Battler actor, Battler opponent, CombatRollContext context, PerkTrigger trigger, PerkModifierTarget target, ref float value, int maxValue = 0)
    {
        ApplyRollModifiersFromOwner(actor, actor, context, trigger, target, ref value, maxValue);
        ApplyRollModifiersFromOwner(opponent, actor, context, trigger, target, ref value, maxValue);
    }

    private void ApplyRollModifiersFromOwner(Battler owner, Battler actor, CombatRollContext context, PerkTrigger trigger, PerkModifierTarget target, ref float value, int maxValue)
    {
        if (owner == null)
            return;

        List<PerkRuntimeInstance> perks = GetEffectivePerks(owner);
        for (int i = 0; i < perks.Count; i++)
        {
            PerkRuntimeInstance perk = perks[i];
            IReadOnlyList<PerkRule> rules = perk.Definition?.Rules;
            if (rules == null)
                continue;

            for (int j = 0; j < rules.Count; j++)
            {
                PerkRule rule = rules[j];
                if (rule == null || rule.Trigger != trigger || rule.ModifierTarget != target || !rule.MatchesRoll(context) || !IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                float ruleValue = target == PerkModifierTarget.MinRollPercent ? Mathf.Max(1, maxValue) * rule.Value : rule.Value;
                value = ApplyModifier(value, rule.Operation, ruleValue, Mathf.Max(1, perk.Stacks));
            }
        }
    }

    private void ApplyDiceModifiers(Battler actor, Battler opponent, CombatActionContext context, DiceResult dice, PerkTrigger trigger, PerkModifierTarget target, ref float value)
    {
        ApplyDiceModifiersFromOwner(actor, context, dice, trigger, target, ref value);
        ApplyDiceModifiersFromOwner(opponent, context, dice, trigger, target, ref value);
    }

    private void ApplyDiceModifiersFromOwner(Battler owner, CombatActionContext context, DiceResult dice, PerkTrigger trigger, PerkModifierTarget target, ref float value)
    {
        if (owner == null || dice == null)
            return;

        List<PerkRuntimeInstance> perks = GetEffectivePerks(owner);
        for (int i = 0; i < perks.Count; i++)
        {
            PerkRuntimeInstance perk = perks[i];
            IReadOnlyList<PerkRule> rules = perk.Definition?.Rules;
            if (rules == null)
                continue;

            for (int j = 0; j < rules.Count; j++)
            {
                PerkRule rule = rules[j];
                if (rule == null || rule.Trigger != trigger || rule.ModifierTarget != target || !rule.MatchesAction(context) || !rule.MatchesDice(dice) || !IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                float ruleValue = target == PerkModifierTarget.DamagePercent ? 1f + rule.Value : rule.Value;
                value = ApplyModifier(value, rule.Operation, ruleValue, Mathf.Max(1, perk.Stacks));
            }
        }
    }

    private List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
    {
        List<PerkRuntimeInstance> perks = new();
        List<PerkDefinition> identityPerks = database.GetIdentityPerks();
        for (int i = 0; i < identityPerks.Count; i++)
            perks.Add(new PerkRuntimeInstance(identityPerks[i], null, -1, 1));

        if (battler?.Perks != null)
            perks.AddRange(battler.Perks);

        return perks;
    }

    private static float ApplyModifier(float current, PerkOperation operation, float value, int stacks)
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

        return current + value * stacks;
    }

    private static bool IsRoleMatch(Battler owner, CombatRollContext context, BattlerStateRole role)
    {
        return IsRoleMatch(owner, context.ToActionContext(), role);
    }

    private static bool IsRoleMatch(Battler owner, CombatActionContext context, BattlerStateRole role)
    {
        return role switch
        {
            BattlerStateRole.OwnerAsActor => owner == context.Actor,
            BattlerStateRole.OwnerAsOpponent => owner == context.Opponent,
            BattlerStateRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
    }

    private static int ResolveDuration(PerkDefinition definition, int durationTurns, int currentDuration)
    {
        int newDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }
}
