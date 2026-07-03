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
            case PerkStackMode.AddStack:
                existing.Stacks = Mathf.Clamp(existing.Stacks + Mathf.Max(1, stacks), 1, maxStacks);
                existing.RemainingTurns = ResolveDuration(definition, durationTurns, existing.RemainingTurns);
                break;
            case PerkStackMode.Replace:
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
        if (battler == null)
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

        if (battler.ActiveStates.Count > 0)
        {
            for (int i = battler.ActiveStates.Count - 1; i >= 0; i--)
            {
                BattlerStateRuntimeInstance state = battler.ActiveStates[i];
                if (state == null || state.Definition == null)
                {
                    battler.ActiveStates.RemoveAt(i);
                    continue;
                }

                if (state.RemainingTurns < 0)
                    continue;

                state.DecreaseDuration();
                if (state.RemainingTurns == 0)
                    battler.ActiveStates.RemoveAt(i);
            }
        }

        if (battler.Drawbacks.Count > 0)
        {
            for (int i = battler.Drawbacks.Count - 1; i >= 0; i--)
            {
                DrawbackRuntimeInstance drawback = battler.Drawbacks[i];
                if (drawback == null)
                {
                    battler.Drawbacks.RemoveAt(i);
                    continue;
                }

                if (drawback.RemainingTurns < 0)
                    continue;

                drawback.DecreaseDuration();
                if (drawback.RemainingTurns == 0) // Remove at exactly 0. 
                {
                    battler.Drawbacks.RemoveAt(i);
                }
            }
        }
    }



    public BattlerStateRuntimeInstance ApplyBattlerState(Battler target, string stateId, Battler source = null, int durationTurns = -1)
    {
        BattlerStateSO definition = BattlerStateDatabase.GetOrCreateRuntimeDatabase().GetById(stateId);
        return ApplyBattlerState(target, definition, source, durationTurns);
    }

    public BattlerStateRuntimeInstance ApplyBattlerState(Battler target, BattlerStateSO definition, Battler source = null, int durationTurns = -1)
    {
        if (target == null || definition == null)
            return null;

        BattlerStateRuntimeInstance existing = target.ActiveStates.Find(state => state != null &&
            state.Definition != null &&
            !string.IsNullOrWhiteSpace(state.Definition.Id) &&
            state.Definition.Id.Equals(definition.Id, System.StringComparison.OrdinalIgnoreCase));

        int resolvedDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        if (existing == null || definition.StackMode == PerkStackMode.Replace)
        {
            if (existing != null)
            {
                RemoveBattlerStatePerks(target, existing);
                target.ActiveStates.Remove(existing);
            }

            BattlerStateRuntimeInstance stateInstance = new(definition, target, source, resolvedDuration);
            target.ActiveStates.Add(stateInstance);
            ApplyBattlerStatePerks(target, source, stateInstance, resolvedDuration);
            return stateInstance;
        }

        existing.Source = source;
        existing.RemainingTurns = ResolveDuration(definition.DefaultDurationTurns, resolvedDuration, existing.RemainingTurns);
        for (int i = 0; i < existing.ActivePerks.Count; i++)
        {
            if (existing.ActivePerks[i] != null)
                existing.ActivePerks[i].RemainingTurns = existing.RemainingTurns;
        }
        return existing;
    }

    public void RemoveBattlerState(Battler target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
            return;

        for (int i = target.ActiveStates.Count - 1; i >= 0; i--)
        {
            BattlerStateRuntimeInstance state = target.ActiveStates[i];
            if (state?.Definition == null || !state.Definition.Id.Equals(stateId, System.StringComparison.OrdinalIgnoreCase))
                continue;

            RemoveBattlerStatePerks(target, state);
            target.ActiveStates.RemoveAt(i);
        }
    }

    private void ApplyBattlerStatePerks(Battler target, Battler source, BattlerStateRuntimeInstance stateInstance, int durationTurns)
    {
        if (stateInstance?.Definition?.PerkIds == null)
            return;

        for (int i = 0; i < stateInstance.Definition.PerkIds.Count; i++)
        {
            PerkRuntimeInstance appliedPerk = ApplyPerk(target, stateInstance.Definition.PerkIds[i], source, durationTurns);
            if (appliedPerk != null && !stateInstance.ActivePerks.Contains(appliedPerk))
                stateInstance.ActivePerks.Add(appliedPerk);
        }
    }

    private void RemoveBattlerStatePerks(Battler target, BattlerStateRuntimeInstance stateInstance)
    {
        if (stateInstance?.ActivePerks == null)
            return;

        for (int i = stateInstance.ActivePerks.Count - 1; i >= 0; i--)
            RemovePerkInstance(target, stateInstance.ActivePerks[i]);

        stateInstance.ActivePerks.Clear();
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

    public void EvaluateActionResolutionTriggers(Battler actor, Battler opponent, ActionType actionType, ActionOutcome outcome)
    {
        ActionResolutionContext actionResolutionContext = new()
        {
            Actor = actor,
            Opponent = opponent,
            ActionType = actionType,
            Outcome = outcome
        };

        triggerEvaluator.EvaluateActionResolutionTriggers(actor, actionResolutionContext, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateActionResolutionTriggers(opponent, actionResolutionContext, GetEffectivePerks(opponent));
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

    private static int ResolveDuration(PerkSO definition, int durationTurns, int currentDuration)
    {
        int newDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }

    private static int ResolveDuration(int defaultDurationTurns, int durationTurns, int currentDuration)
    {
        int newDuration = durationTurns >= 0 ? durationTurns : defaultDurationTurns;
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

    public void ExecuteManualActivation(Battler battler, TrickRuntimeInstance trickInstance)
    {
        if (battler == null || trickInstance == null || trickInstance.Definition == null)
            return;

        int charges = Mathf.FloorToInt(trickInstance.CurrentCharges);
        if (charges <= 0)
            return;

        // ETAPA A: Positive Release & ETAPA C (partial): Cleanup charge perks
        for (int i = 0; i < trickInstance.Definition.PerkIds.Count; i++)
        {
            string perkId = trickInstance.Definition.PerkIds[i];
            PerkSO perkDef = GetPerkDefinition(perkId);
            if (perkDef == null)
                continue;

            bool hasManualTrigger = false;
            if (perkDef.Rules != null)
            {
                for (int j = 0; j < perkDef.Rules.Count; j++)
                {
                    if (perkDef.Rules[j].Trigger == PerkTrigger.OnManualActivation)
                    {
                        hasManualTrigger = true;
                        break;
                    }
                }
            }

            if (hasManualTrigger)
            {
                ApplyPerk(battler, perkDef, battler, -1, charges);
            }
            else
            {
                PerkRuntimeInstance existing = battler.Perks.Find(p => p.Definition == perkDef && p.SourceTrickInstanceId == trickInstance.InstanceId);
                if (existing != null)
                {
                    RemovePerkInstance(battler, existing);
                }
            }
        }

        // ETAPA B: Drawback
        if (trickInstance.Definition.DrawbackIds != null && trickInstance.Definition.DrawbackIds.Count > 0)
        {
            DrawbackDatabase drawbackDb = DrawbackDatabase.GetOrCreateRuntimeDatabase();
            for (int i = 0; i < trickInstance.Definition.DrawbackIds.Count; i++)
            {
                DrawbackSO drawback = drawbackDb.GetById(trickInstance.Definition.DrawbackIds[i]);
                if (drawback != null && drawback.PerkIds != null)
                {
                    DrawbackRuntimeInstance drawbackInstance = new(drawback, battler, drawback.DurationTurns, battler);
                    battler.Drawbacks.Add(drawbackInstance);

                    for (int j = 0; j < drawback.PerkIds.Count; j++)
                    {
                        PerkRuntimeInstance appliedPerk = ApplyPerk(battler, drawback.PerkIds[j], battler, drawback.DurationTurns, 1);
                        if (appliedPerk != null)
                        {
                            drawbackInstance.ActivePerks.Add(appliedPerk);
                        }
                    }
                }
            }
        }

        // ETAPA C: Cleanup
        trickInstance.ConsumeCharges();
        trickInstance.StartCooldown(trickInstance.Definition.CooldownTurns);
    }
}
