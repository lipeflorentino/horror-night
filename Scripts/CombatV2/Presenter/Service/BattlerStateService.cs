using System.Collections.Generic;
using UnityEngine;

public class BattlerStateService
{
    public void ApplyState(Battler target, BattlerStateDefinition definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        if (target == null || definition == null)
            return;

        int maxStacks = Mathf.Max(1, definition.MaxStacks);
        BattlerStateInstance existing = target.States.Find(state => state.Definition == definition || state.Definition?.Id == definition.Id);
        if (existing == null)
        {
            target.States.Add(new BattlerStateInstance(definition, source, durationTurns, Mathf.Clamp(stacks, 1, maxStacks)));
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

    public void RemoveState(Battler target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
            return;

        target.States.RemoveAll(state => state.Definition != null && state.Definition.Id == stateId);
    }

    public bool HasState(Battler target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
            return false;

        return target.States.Exists(state => state.Definition != null && state.Definition.Id == stateId);
    }

    public void TickTurnEnd(Battler battler)
    {
        if (battler == null || battler.States.Count == 0)
            return;

        for (int i = battler.States.Count - 1; i >= 0; i--)
        {
            BattlerStateInstance state = battler.States[i];
            if (state == null || state.Definition == null)
            {
                battler.States.RemoveAt(i);
                continue;
            }

            if (state.RemainingTurns < 0)
                continue;

            state.RemainingTurns--;
            if (state.RemainingTurns <= 0)
                battler.States.RemoveAt(i);
        }
    }

    public ThresholdPair ApplyThresholdModifiers(ThresholdPair thresholds, CombatRollContext context)
    {
        ThresholdPair modified = thresholds;
        ApplyThresholdModifiersFromOwner(context.Actor, context, ref modified);
        ApplyThresholdModifiersFromOwner(context.Opponent, context, ref modified);
        return modified;
    }

    public int GetEffectiveActionPower(Battler actor, Battler opponent, ActionType actionType)
    {
        if (actor == null)
            return 0;

        BattlerStateStatType statType = actionType == ActionType.Attack ? BattlerStateStatType.Attack : BattlerStateStatType.Defense;
        float value = actionType == ActionType.Attack ? actor.Attack : actor.Defense;
        CombatActionContext context = new(actor, opponent, actionType);

        value = ApplyStatModifiersFromOwner(actor, context, statType, value);
        value = ApplyStatModifiersFromOwner(opponent, context, statType, value);

        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetEffectiveFocus(Battler actor, Battler opponent, ActionType actionType)
    {
        return GetEffectiveStat(actor, opponent, actionType, BattlerStateStatType.Focus, actor?.Focus ?? 0);
    }

    public int GetEffectiveStrength(Battler actor, Battler opponent, ActionType actionType)
    {
        return GetEffectiveStat(actor, opponent, actionType, BattlerStateStatType.Strength, actor?.Strength ?? 0);
    }

    private int GetEffectiveStat(Battler actor, Battler opponent, ActionType actionType, BattlerStateStatType statType, int baseValue)
    {
        CombatActionContext context = new(actor, opponent, actionType);
        float value = baseValue;
        value = ApplyStatModifiersFromOwner(actor, context, statType, value);
        value = ApplyStatModifiersFromOwner(opponent, context, statType, value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    private void ApplyThresholdModifiersFromOwner(Battler owner, CombatRollContext context, ref ThresholdPair thresholds)
    {
        if (owner == null || owner.States.Count == 0)
            return;

        for (int i = 0; i < owner.States.Count; i++)
        {
            BattlerStateInstance state = owner.States[i];
            if (state?.Definition?.ThresholdModifiers == null)
                continue;

            int stacks = Mathf.Max(1, state.Stacks);
            IReadOnlyList<ThresholdModifier> modifiers = state.Definition.ThresholdModifiers;
            for (int j = 0; j < modifiers.Count; j++)
            {
                ThresholdModifier modifier = modifiers[j];
                if (modifier == null || !modifier.MatchesRoll(context) || !IsRoleMatch(owner, context, modifier.Role))
                    continue;

                float current = modifier.Band == ThresholdBand.Low ? thresholds.Low : thresholds.High;
                float value = ApplyModifier(current, modifier.Operation, modifier.Value, stacks);
                if (modifier.Band == ThresholdBand.Low)
                    thresholds.Low = value;
                else
                    thresholds.High = value;
            }
        }
    }

    private float ApplyStatModifiersFromOwner(Battler owner, CombatActionContext context, BattlerStateStatType statType, float value)
    {
        if (owner == null || owner.States.Count == 0)
            return value;

        float modified = value;
        for (int i = 0; i < owner.States.Count; i++)
        {
            BattlerStateInstance state = owner.States[i];
            if (state?.Definition?.StatModifiers == null)
                continue;

            int stacks = Mathf.Max(1, state.Stacks);
            IReadOnlyList<StatModifier> modifiers = state.Definition.StatModifiers;
            for (int j = 0; j < modifiers.Count; j++)
            {
                StatModifier modifier = modifiers[j];
                if (modifier == null || !modifier.MatchesStat(context, statType) || !IsRoleMatch(owner, context, modifier.Role))
                    continue;

                modified = ApplyModifier(modified, modifier.Operation, modifier.Value, stacks);
            }
        }

        return modified;
    }

    private static float ApplyModifier(float current, ModifierOperation operation, float value, int stacks)
    {
        if (operation == ModifierOperation.Multiply)
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
        CombatActionContext actionContext = context.ToActionContext();
        return IsRoleMatch(owner, actionContext, role);
    }

    private static bool IsRoleMatch(Battler owner, CombatActionContext context, BattlerStateRole role)
    {
        return role switch
        {
            BattlerStateRole.OwnerAsActor => owner == context.Actor,
            BattlerStateRole.OwnerAsOpponent => owner == context.Opponent,
            BattlerStateRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsTarget => IsTarget(owner, context),
            _ => false
        };
    }

    private static bool IsTarget(Battler owner, CombatActionContext context)
    {
        return context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor;
    }

    private static int ResolveDuration(BattlerStateDefinition definition, int durationTurns, int currentDuration)
    {
        int newDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }
}
