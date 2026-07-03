using System;
using System.Collections.Generic;
using UnityEngine;

public class PerkEffectResolver
{
    private readonly PerkTriggerEvaluator triggerEvaluator;
    private readonly Func<Battler, List<PerkRuntimeInstance>> effectivePerksProvider;

    public PerkEffectResolver(PerkTriggerEvaluator triggerEvaluator, Func<Battler, List<PerkRuntimeInstance>> effectivePerksProvider)
    {
        this.triggerEvaluator = triggerEvaluator;
        this.effectivePerksProvider = effectivePerksProvider;
    }

    public int GetEffectiveActionPower(Battler actor, Battler opponent, ActionType actionType)
    {
        if (actor == null)
            return 0;

        PerkModifierTarget target = actionType == ActionType.Attack ? PerkModifierTarget.AttackPower : PerkModifierTarget.DefensePower;
        float value = actionType == ActionType.Attack ? actor.Attack : actor.Defense;
        CombatActionContext context = new(actor, opponent, actionType);
        ApplyContextualModifiers(actor, context, PerkTrigger.BeforeRoll, target, ref value);
        ApplyContextualModifiers(opponent, context, PerkTrigger.BeforeRoll, target, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetEffectiveFocus(Battler actor, Battler opponent, ActionType actionType)
    {
        return GetEffectiveStat(actor, opponent, actionType, PerkModifierTarget.Focus, actor?.Focus ?? 0);
    }

    public int GetEffectiveStrength(Battler actor, Battler opponent, ActionType actionType)
    {
        return GetEffectiveStat(actor, opponent, actionType, PerkModifierTarget.Strength, actor?.Strength ?? 0);
    }

    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context)
    {
        triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));

        float value = 0f;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.ExtraDice, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetExtraPowerDiceAfterAccuracy(
        Battler actor,
        Battler opponent,
        DiceResult accuracyResult,
        ActionType actionType,
        out DiceStatType extraDiceStatType)
    {
        extraDiceStatType = DiceStatType.Body;
        if (accuracyResult == null)
            return 0;

        int count = triggerEvaluator.EvaluateAfterAccuracyTriggers(
            actor, accuracyResult, actionType, GetEffectivePerks(actor), out extraDiceStatType);

        return Mathf.Max(0, count);
    }

    public int GetMinimumRollValue(Battler actor, Battler opponent, CombatRollContext context, int currentMinValue)
    {
        triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));

        float minValue = currentMinValue;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.MinRollPercent, ref minValue, context.MaxValue);
        return Mathf.Clamp(Mathf.CeilToInt(minValue), 1, Mathf.Max(1, context.MaxValue));
    }

    public (float low, float high) GetModifiedRollThresholds(Battler actor, Battler opponent, CombatRollContext context, float low, float high)
    {
        float modifiedLow = low;
        float modifiedHigh = high;
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.MinRollPercent, ref modifiedLow);
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.MaxRollPercent, ref modifiedHigh);
        return (modifiedLow, modifiedHigh);
    }

    public float GetPowerMultiplier(float baseMultiplier, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action?.PowerDice == null)
            return baseMultiplier;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(actor));

        if (opponent != null)
            triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(opponent));

        float multiplier = baseMultiplier;
        ApplyDiceModifiers(actor, opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, PerkModifierTarget.PowerMultiplier, ref multiplier);
        return Mathf.Max(0f, multiplier);
    }

    public int ApplyDamageModifiers(int damage, ActionInstance action, Battler actor, Battler opponent, ActionType actionType, ActionInstance opposingAction = null)
    {
        if (damage <= 0 || action == null)
            return damage;

        CombatActionContext actionContext = new(actor, opponent, actionType);
        List<DiceResult> actionDice = GetActionDice(action);
        List<DiceResult> opposingActionDice = GetActionDice(opposingAction);

        if (action.PowerDice != null)
        {
            triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor), actionDice, opposingActionDice);
            if (opponent != null)
                triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent), actionDice, opposingActionDice);
        }

        if (action.AccuracyDice != null)
        {
            triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor), actionDice, opposingActionDice);
            if (opponent != null)
                triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent), actionDice, opposingActionDice);
        }

        float modifiedDamage = damage;
        ApplyDiceModifiers(actor, opponent, actionContext, action.PowerDice, PerkTrigger.AfterResolve, PerkModifierTarget.DamagePercent, ref modifiedDamage, actionDice, opposingActionDice);
        ApplyDiceModifiers(actor, opponent, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, PerkModifierTarget.DamagePercent, ref modifiedDamage, actionDice, opposingActionDice);
        return Mathf.Max(0, Mathf.RoundToInt(modifiedDamage));
    }

    private int GetEffectiveStat(Battler actor, Battler opponent, ActionType actionType, PerkModifierTarget target, int baseValue)
    {
        CombatActionContext context = new(actor, opponent, actionType);
        float value = baseValue;
        ApplyContextualModifiers(actor, context, PerkTrigger.OnActionResolved, target, ref value);
        ApplyContextualModifiers(opponent, context, PerkTrigger.OnActionResolved, target, ref value);
        ApplyContextualModifiers(actor, context, PerkTrigger.BeforeRoll, target, ref value);
        ApplyContextualModifiers(opponent, context, PerkTrigger.BeforeRoll, target, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    private void ApplyContextualModifiers(Battler owner, CombatActionContext context, PerkTrigger trigger, PerkModifierTarget target, ref float value)
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
                if (rule == null || rule.Trigger != trigger || rule.ModifierTarget != target || !rule.MatchesAction(context) || !IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                value = ApplyModifier(value, rule.Operation, rule.Value, Mathf.Max(1, perk.Stacks));
            }
        }
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

                float ruleValue = target == PerkModifierTarget.MinRollPercent && maxValue > 0 ? Mathf.Max(1, maxValue) * rule.Value : rule.Value;
                value = ApplyModifier(value, rule.Operation, ruleValue, Mathf.Max(1, perk.Stacks));
            }
        }
    }

    private void ApplyDiceModifiers(Battler actor, Battler opponent, CombatActionContext context, DiceResult dice, PerkTrigger trigger, PerkModifierTarget target, ref float value, List<DiceResult> actionDice = null, List<DiceResult> opposingActionDice = null)
    {
        ApplyDiceModifiersFromOwner(actor, context, dice, trigger, target, ref value, actionDice, opposingActionDice);
        ApplyDiceModifiersFromOwner(opponent, context, dice, trigger, target, ref value, actionDice, opposingActionDice);
    }

    private void ApplyDiceModifiersFromOwner(Battler owner, CombatActionContext context, DiceResult dice, PerkTrigger trigger, PerkModifierTarget target, ref float value, List<DiceResult> actionDice = null, List<DiceResult> opposingActionDice = null)
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
                if (rule == null || rule.Trigger != trigger || rule.ModifierTarget != target || !rule.MatchesAction(context) || !rule.MatchesDice(dice) || !MatchesDiceCondition(rule, dice, actionDice, opposingActionDice) || !IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                float ruleValue = target == PerkModifierTarget.DamagePercent ? 1f + rule.Value : rule.Value;
                value = ApplyModifier(value, rule.Operation, ruleValue, Mathf.Max(1, perk.Stacks));
            }
        }
    }

    private static bool MatchesDiceCondition(PerkRule rule, DiceResult dice, List<DiceResult> actionDice, List<DiceResult> opposingActionDice)
    {
        if (rule.ConditionKey == PerkConditionKey.RollValueEquals || rule.ConditionKey == PerkConditionKey.RollTierEquals)
            return true;

        if (actionDice == null || actionDice.Count == 0 || dice != actionDice[0])
            return false;

        if (rule.ConditionKey == PerkConditionKey.RollSumEquals)
            return PerkConditionFactory.Evaluate(rule.ConditionKey, new DiceRollSumContext { TotalSum = SumDice(actionDice), Dices = actionDice }, rule.ConditionValue);

        if (rule.ConditionKey == PerkConditionKey.RollSumEqualsAttackersRollSum)
            return PerkConditionFactory.Evaluate(rule.ConditionKey, new DefenseRollComparisonContext { DefenderRollSum = SumDice(actionDice), AttackerRollSum = SumDice(opposingActionDice) }, rule.ConditionValue);

        return rule.ConditionKey == PerkConditionKey.Always;
    }

    private static List<DiceResult> GetActionDice(ActionInstance action)
    {
        List<DiceResult> dices = new();
        if (action?.PowerDice != null)
            dices.Add(action.PowerDice);
        if (action?.AccuracyDice != null)
            dices.Add(action.AccuracyDice);
        return dices;
    }

    private static int SumDice(List<DiceResult> dices)
    {
        int sum = 0;
        if (dices == null)
            return sum;

        for (int i = 0; i < dices.Count; i++)
            sum += dices[i]?.Value ?? 0;

        return sum;
    }

    private List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
    {
        return effectivePerksProvider?.Invoke(battler) ?? new List<PerkRuntimeInstance>();
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

    private static bool IsRoleMatch(Battler owner, CombatRollContext context, PerkRole role)
    {
        return IsRoleMatch(owner, context.ToActionContext(), role);
    }

    private static bool IsRoleMatch(Battler owner, CombatActionContext context, PerkRole role)
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
