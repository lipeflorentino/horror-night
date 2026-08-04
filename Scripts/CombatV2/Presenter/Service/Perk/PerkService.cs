using System.Collections.Generic;
using UnityEngine;

public class PerkService
{
    // =========================
    // Dependencies and state
    // =========================
    private readonly PerkDatabase database;
    private readonly PerkTriggerEvaluator triggerEvaluator;
    private readonly PerkEffectResolver effectResolver;

    public event System.Action<Battler, PerkRuntimeInstance> OnPerkApplied;
    public event System.Action<Battler, string> OnPerkRemoved;
    public event System.Action<PerkTriggeredEvent> OnPerkTriggered;
    public event System.Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateApplied;
    public event System.Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateRemoved;
    public event System.Action<Battler, DrawbackRuntimeInstance> OnDrawbackApplied;
    public event System.Action<Battler, DrawbackRuntimeInstance> OnDrawbackRemoved;

    public PerkService()
    {
        database = PerkDatabase.GetOrCreateRuntimeDatabase();
        database.EnsureLoaded();
        
        triggerEvaluator = new PerkTriggerEvaluator(database);
        triggerEvaluator.OnPerkTriggered += (evt) => OnPerkTriggered?.Invoke(evt);
        effectResolver = new PerkEffectResolver(triggerEvaluator, GetEffectivePerks);
    }

    // =========================
    // Perk lookup and application
    // =========================
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

    // =========================
    // Turn lifecycle and expiration
    // =========================
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
                {
                    OnBattlerStateRemoved?.Invoke(battler, state);
                    battler.ActiveStates.RemoveAt(i);
                }
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
                    OnDrawbackRemoved?.Invoke(battler, drawback);
                    battler.Drawbacks.RemoveAt(i);
                }
            }
        }
    }



    // =========================
    // Battler states and drawbacks
    // =========================
    public DrawbackRuntimeInstance ApplyDrawback(Battler target, string drawbackId, Battler source = null, int durationTurns = -1)
    {
        if (target == null || string.IsNullOrWhiteSpace(drawbackId))
            return null;

        DrawbackDatabase drawbackDb = DrawbackDatabase.GetOrCreateRuntimeDatabase();
        DrawbackSO definition = drawbackDb.GetById(drawbackId);
        if (definition == null)
        {
            Logger.Log($"[PerkService] Drawback '{drawbackId}' not found.");
            return null;
        }

        DrawbackRuntimeInstance existing = target.Drawbacks.Find(drawback => drawback != null && drawback.Definition != null &&
            drawback.Definition.Id.Equals(drawbackId, System.StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        int resolvedDuration = durationTurns >= 0 ? durationTurns : definition.DurationTurns;
        DrawbackRuntimeInstance drawbackInstance = new(definition, target, resolvedDuration, source);
        target.Drawbacks.Add(drawbackInstance);
        OnDrawbackApplied?.Invoke(target, drawbackInstance);

        if (definition.PerkIds != null)
        {
            for (int i = 0; i < definition.PerkIds.Count; i++)
            {
                PerkRuntimeInstance appliedPerk = ApplyPerk(target, definition.PerkIds[i], source, resolvedDuration);
                if (appliedPerk != null)
                    drawbackInstance.ActivePerks.Add(appliedPerk);
            }
        }

        return drawbackInstance;
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
            OnBattlerStateApplied?.Invoke(target, stateInstance);
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
            OnBattlerStateRemoved?.Invoke(target, state);
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

    // =========================
    // Effect calculation and modifiers
    // =========================
    public int GetEffectiveActionPower(Battler actor, Battler opponent, ActionType actionType)
    {
        return effectResolver.GetEffectiveActionPower(actor, opponent, actionType);
    }

    public int GetEffectiveMind(Battler battler)
    {
        return effectResolver.GetEffectiveMind(battler);
    }

    public int GetEffectiveHeart(Battler battler)
    {
        return effectResolver.GetEffectiveHeart(battler);
    }

    public int GetEffectiveBody(Battler battler)
    {
        return effectResolver.GetEffectiveBody(battler);
    }

    public int GetEffectiveFocus(Battler actor, Battler opponent, ActionType actionType)
    {
        return effectResolver.GetEffectiveFocus(actor, opponent, actionType);
    }

    public int GetEffectiveStrength(Battler actor, Battler opponent, ActionType actionType)
    {
        return effectResolver.GetEffectiveStrength(actor, opponent, actionType);
    }

    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context, bool evaluateTriggers = true)
    {
        return effectResolver.GetExtraDiceCount(actor, opponent, context, evaluateTriggers);
    }
    
    public int GetExtraPowerDiceAfterAccuracy(
        Battler actor,
        Battler opponent,
        DiceResult accuracyResult,
        ActionType actionType,
        out DiceStatType extraDiceStatType)
    {
        return effectResolver.GetExtraPowerDiceAfterAccuracy(actor, opponent, accuracyResult, actionType, out extraDiceStatType);
    }

    public int GetMinimumRollValue(Battler actor, Battler opponent, CombatRollContext context, int currentMinValue, bool evaluateTriggers = true)
    {
        return effectResolver.GetMinimumRollValue(actor, opponent, context, currentMinValue, evaluateTriggers);
    }

    public (float low, float high) GetModifiedRollThresholds(Battler actor, Battler opponent, CombatRollContext context, float low, float high)
    {
        return effectResolver.GetModifiedRollThresholds(actor, opponent, context, low, high);
    }

    public float GetPowerMultiplier(float baseMultiplier, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        return effectResolver.GetPowerMultiplier(baseMultiplier, action, actor, opponent, actionType);
    }

    public int ApplyDamageModifiers(int damage, ActionInstance action, Battler actor, Battler opponent, ActionType actionType, ActionInstance opposingAction = null)
    {
        return effectResolver.ApplyDamageModifiers(damage, action, actor, opponent, actionType, opposingAction);
    }

    // =========================
    // Trigger evaluation
    // =========================
    public void EvaluateActionResolutionTriggers(Battler actor, Battler opponent, ActionType actionType, ActionOutcome outcome, ActionResolutionVariation variation = ActionResolutionVariation.None)
    {
        ActionResolutionContext actionResolutionContext = new()
        {
            Actor = actor,
            Opponent = opponent,
            ActionType = actionType,
            Outcome = outcome,
            ResolutionVariation = variation
        };

        triggerEvaluator.EvaluateActionResolutionTriggers(actor, actionResolutionContext, GetEffectivePerks(actor));
        if (opponent != null)
            triggerEvaluator.EvaluateActionResolutionTriggers(opponent, actionResolutionContext, GetEffectivePerks(opponent));
    }

    public List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
    {
        return PerkRuntimeHelper.GetEffectivePerks(battler);
    }

    private static bool IsSamePerkInstance(PerkRuntimeInstance perk, PerkSO definition, TrickRuntimeInstance sourceTrick)
    {
        if (perk == null || definition == null || !(perk.Definition == definition || perk.Definition?.Id == definition.Id))
            return false;

        if (sourceTrick == null)
            return perk.SourceTrick == null;

        return perk.SourceTrickInstanceId == sourceTrick.InstanceId;
    }

    private static int ResolveDuration(PerkSO definition, int durationTurns, int currentDuration)
    {
        return PerkRuntimeHelper.ResolveDuration(durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns, definition.DefaultDurationTurns, currentDuration);
    }

    private static int ResolveDuration(int defaultDurationTurns, int durationTurns, int currentDuration)
    {
        return PerkRuntimeHelper.ResolveDuration(durationTurns, defaultDurationTurns, currentDuration);
    }

    // =========================
    // Removal and manual activation
    // =========================
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

        int chargesToUse = 1; // Valor padrão para Active normal

        if (trickInstance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            chargesToUse = Mathf.FloorToInt(trickInstance.CurrentCharges);
            if (chargesToUse <= 0)
                return; // Aborta apenas se for ActiveCharge e não tiver cargas suficientes
        }
        else
        {
            // Se for Active normal, aborta se estiver em Cooldown
            if (trickInstance.IsCoolingDown) return;
        }

        // ETAPA A: Positive Release & ETAPA C (partial): Cleanup charge perks
        for (int i = 0; i < trickInstance.Definition.PerkIds.Count; i++)
        {
            string perkId = trickInstance.Definition.PerkIds[i];
            PerkSO perkDef = GetPerkDefinition(perkId);
            if (perkDef == null) continue;

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
                // Usa as chargesToUse computadas
                ApplyPerk(battler, perkDef, battler, -1, chargesToUse);
            }
            else
            {
                PerkRuntimeInstance existing = battler.Perks.Find(p => p.Definition == perkDef && p.SourceTrickInstanceId == trickInstance.InstanceId);
                if (existing != null) RemovePerkInstance(battler, existing);
            }
        }

        // ETAPA B: Drawback (Mantém-se igual)
        if (trickInstance.Definition.DrawbackIds != null && trickInstance.Definition.DrawbackIds.Count > 0)
        {
            DrawbackDatabase drawbackDb = DrawbackDatabase.GetOrCreateRuntimeDatabase();
            for (int i = 0; i < trickInstance.Definition.DrawbackIds.Count; i++)
            {
                DrawbackSO drawback = drawbackDb.GetById(trickInstance.Definition.DrawbackIds[i]);
                if (drawback != null && drawback.PerkIds != null)
                {
                    int rolledDuration = drawback.RollDuration();
                    DrawbackRuntimeInstance drawbackInstance = new(drawback, battler, rolledDuration, battler);
                    battler.Drawbacks.Add(drawbackInstance);
                    OnDrawbackApplied?.Invoke(battler, drawbackInstance);

                    for (int j = 0; j < drawback.PerkIds.Count; j++)
                    {
                        PerkRuntimeInstance appliedPerk = ApplyPerk(battler, drawback.PerkIds[j], battler, rolledDuration, 1);
                        if (appliedPerk != null) drawbackInstance.ActivePerks.Add(appliedPerk);
                    }
                }
            }
        }

        // ETAPA C: Cleanup
        if (trickInstance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            trickInstance.ConsumeCharges();
            // Se ActiveCharge precisar entrar em cooldown entre usos sem expirar, você terá 
            // que ajustar o TrickService.TickTrickEnd para descer o cooldown mesmo se WasExpired for false.
        }
        else if (trickInstance.Definition.ActivationMode == TrickActivationMode.Active)
        {
            // Força a expiração do Active Trick normal para que o Cooldown inicie na UI e no TrickService
            trickInstance.RemainingTurns = 0;
            trickInstance.MarkExpired();
        }
        
        trickInstance.StartCooldown(trickInstance.Definition.CooldownTurns);
    }
}
