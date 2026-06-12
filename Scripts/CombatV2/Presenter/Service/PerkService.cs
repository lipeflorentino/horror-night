using System.Collections.Generic;
using UnityEngine;

public class PerkService
{
    private readonly PerkDatabase database;
    private readonly PerkTriggerEvaluator triggerEvaluator;

    public event System.Action<Battler, PerkRuntimeInstance> OnPerkApplied;
    public event System.Action<Battler, string> OnPerkRemoved;
    public event System.Action<PerkTriggeredEvent> OnPerkTriggered;

    public PerkService()
    {
        database = PerkDatabase.GetOrCreateRuntimeDatabase();
        database.EnsureLoaded();
        
        triggerEvaluator = new PerkTriggerEvaluator(database);
        triggerEvaluator.OnPerkTriggered += (evt) => OnPerkTriggered?.Invoke(evt);
        
        OnPerkApplied += (b, p) => Debug.Log($"Perk {p.Definition.Id} aplicado!");
        OnPerkRemoved += (b, id) => Debug.Log($"Perk {id} removido!");
        OnPerkTriggered += (evt) => Debug.Log($"Perk {evt.PerkId} acionado com trigger {evt.Trigger}!");
    }

    public PerkSO GetPerkDefinition(string perkId)
    {
        return database.GetById(perkId);
    }

    public PerkRuntimeInstance ApplyPerk(Battler target, string perkId, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        return ApplyPerk(target, GetPerkDefinition(perkId), source, durationTurns, stacks);
    }

    public PerkRuntimeInstance ApplyPerk(Battler target, PerkSO definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        return ApplyPerkInternal(target, definition, source, durationTurns, stacks, null);
    }

    public PerkRuntimeInstance ApplyPerkFromTrick(
        Battler target,
        string perkId,
        TrickRuntimeInstance sourceTrick,
        Battler source = null,
        int durationTurns = -1,
        int stacks = 1)
    {
        return ApplyPerkInternal(target, GetPerkDefinition(perkId), source, durationTurns, stacks, sourceTrick);
    }

    private PerkRuntimeInstance ApplyPerkInternal(
        Battler target,
        PerkSO definition,
        Battler source,
        int durationTurns,
        int stacks,
        TrickRuntimeInstance sourceTrick)
    {
        if (target == null || definition == null)
            return null;

        int maxStacks = Mathf.Max(1, definition.MaxStacks);
        PerkRuntimeInstance existing = target.Perks.Find(perk => IsSamePerkInstance(perk, definition, sourceTrick));
        if (existing == null)
        {
            PerkRuntimeInstance newPerk = new(definition, source, durationTurns, Mathf.Clamp(stacks, 1, maxStacks), sourceTrick);
            target.Perks.Add(newPerk);
            OnPerkApplied?.Invoke(target, newPerk);
            return newPerk;
        }

        switch (definition.StackMode)
        {
            case BattlerStateStackMode.AddStack:
                existing.Stacks = Mathf.Clamp(existing.Stacks + Mathf.Max(1, stacks), 1, maxStacks);
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
            case BattlerStateStackMode.Replace:
                existing.Source = source;
                existing.SetSourceTrick(sourceTrick);
                existing.Stacks = Mathf.Clamp(stacks, 1, maxStacks);
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
            default:
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                if (sourceTrick != null)
                    existing.SetSourceTrick(sourceTrick);
                break;
        }

        return existing;
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

            if (perk.SourceTrick != null)
                continue;

            if (perk.RemainingTurns < 0)
                continue;

            perk.RemainingTurns--;
            if (perk.RemainingTurns <= 0)
            {
                battler.Perks.RemoveAt(i);
                OnPerkRemoved?.Invoke(battler, perk.Definition.Id);
            }
        }
    }

    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context)
    {
        // ✅ Dispara triggers ANTES de aplicar modificadores (BeforeRoll)
        triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));
        
        float value = 0f;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.ExtraDice, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetMinimumRollValue(Battler actor, Battler opponent, CombatRollContext context, int currentMinValue)
    {
        // ✅ Dispara triggers ANTES de aplicar modificadores (BeforeRoll)
        triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));
        
        float minValue = currentMinValue;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.MinRollPercent, ref minValue, context.MaxValue);
        return Mathf.Clamp(Mathf.CeilToInt(minValue), 1, Mathf.Max(1, context.MaxValue));
    }

    public float GetPowerMultiplier(float baseMultiplier, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action?.PowerDice == null)
            return baseMultiplier;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        
        // ✅ Dispara triggers quando dado é resolvido (PowerMultiplier)
        triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(opponent));
        
        float multiplier = baseMultiplier;
        ApplyDiceModifiers(actor, opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, PerkModifierTarget.PowerMultiplier, ref multiplier);
        return Mathf.Max(0f, multiplier);
    }

    public int ApplyDamageModifiers(int damage, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (damage <= 0 || action == null)
            return damage;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        
        // ✅ Dispara triggers quando dados são analisados para dano (AfterResolve)
        if (action.PowerDice != null)
        {
            triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor));
            if (opponent != null)
                triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent));
        }
        
        if (action.AccuracyDice != null)
        {
            triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor));
            if (opponent != null)
                triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent));
        }
        
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

    public List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
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

    private static bool IsSamePerkInstance(PerkRuntimeInstance perk, PerkSO definition, TrickRuntimeInstance sourceTrick)
    {
        if (perk == null || definition == null || !(perk.Definition == definition || perk.Definition?.Id == definition.Id))
            return false;

        if (sourceTrick == null)
            return perk.SourceTrick == null;

        return perk.SourceTrickInstanceId == sourceTrick.InstanceId;
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

    private static int ResolveDuration(PerkSO definition, int durationTurns, int currentDuration)
    {
        int newDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }

    public void RemovePerk(Battler target, string perkId)
    {
        if (target == null || string.IsNullOrWhiteSpace(perkId))
            return;

        PerkRuntimeInstance instance = target.Perks.Find(perk => perk != null &&
            perk.SourceTrick == null &&
            perk.Definition != null &&
            !string.IsNullOrWhiteSpace(perk.Definition.Id) &&
            perk.Definition.Id.Equals(perkId, System.StringComparison.OrdinalIgnoreCase));
        if (instance == null)
            return;

        target.Perks.Remove(instance);
        OnPerkRemoved?.Invoke(target, perkId);
    }

    public void RemovePerkInstance(Battler target, PerkRuntimeInstance instance)
    {
        if (target == null || instance == null)
            return;

        if (target.Perks.Remove(instance))
            OnPerkRemoved?.Invoke(target, instance.Definition?.Id);
    }
}
