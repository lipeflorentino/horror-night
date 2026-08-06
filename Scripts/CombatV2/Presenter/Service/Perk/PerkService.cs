using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço orquestrador principal para gerenciar Perks. Coordena o ciclo de vida dos efeitos, interliga a avaliação de gatilhos com a aplicação de mutações e resolve instâncias ativas.
/// </summary>
public class PerkService
{
    // ==========================================
    // DEPENDENCIES, STATE & EVENTS
    // ==========================================
    private readonly PerkDatabase database;
    private readonly PerkTriggerEvaluator triggerEvaluator;
    private readonly PerkEffectResolver effectResolver;
    private readonly PerkStateApplicator stateApplicator;

    public event System.Action<Battler, PerkRuntimeInstance> OnPerkApplied;
    public event System.Action<Battler, string> OnPerkRemoved;
    public event System.Action<PerkTriggeredEvent> OnPerkTriggered;
    public event System.Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateApplied;
    public event System.Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateRemoved;
    public event System.Action<Battler, DrawbackRuntimeInstance> OnDrawbackApplied;
    public event System.Action<Battler, DrawbackRuntimeInstance> OnDrawbackRemoved;


    // ==========================================
    // INITIALIZATION & EVENT HANDLERS
    // ==========================================
    public PerkService()
    {
        database = PerkDatabase.GetOrCreateRuntimeDatabase();
        database.EnsureLoaded();
        
        triggerEvaluator = new PerkTriggerEvaluator();
        stateApplicator = new PerkStateApplicator();
        effectResolver = new PerkEffectResolver(GetEffectivePerks); 

        triggerEvaluator.OnPerkTriggered += HandlePerkTriggered;
    }

    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        stateApplicator.HandlePerkTriggered(evt);
        OnPerkTriggered?.Invoke(evt);
    }


    // ==========================================
    // PERK LOOKUP & APPLICATION
    // ==========================================
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

    public PerkRuntimeInstance ApplyPerkFromTrick(Battler target, string perkId, TrickRuntimeInstance sourceTrick, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        return ApplyPerkInternal(target, GetPerkDefinition(perkId), source, durationTurns, stacks, sourceTrick);
    }

    private PerkRuntimeInstance ApplyPerkInternal(Battler target, PerkSO definition, Battler source, int durationTurns, int stacks, TrickRuntimeInstance sourceTrick)
    {
        if (target == null || definition == null) return null;

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
                if (sourceTrick != null) existing.SetSourceTrick(sourceTrick);
                break;
        }

        return existing;
    }


    // ==========================================
    // TURN LIFECYCLE & EXPIRATION
    // ==========================================
    public void TickTurnEnd(Battler battler)
    {
        if (battler == null) return;

        for (int i = battler.Perks.Count - 1; i >= 0; i--)
        {
            PerkRuntimeInstance perk = battler.Perks[i];
            if (perk == null || perk.Definition == null)
            {
                battler.Perks.RemoveAt(i);
                continue;
            }

            if (perk.SourceTrick != null) continue;
            if (perk.RemainingTurns < 0) continue;

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

                if (state.RemainingTurns < 0) continue;

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

                if (drawback.RemainingTurns < 0) continue;

                drawback.DecreaseDuration();
                if (drawback.RemainingTurns == 0) 
                {
                    OnDrawbackRemoved?.Invoke(battler, drawback);
                    battler.Drawbacks.RemoveAt(i);
                }
            }
        }
    }


    // ==========================================
    // DRAWBACKS & BATTLER STATES
    // ==========================================
    public DrawbackRuntimeInstance ApplyDrawback(Battler target, string drawbackId, Battler source = null, int durationTurns = -1)
    {
        if (target == null || string.IsNullOrWhiteSpace(drawbackId)) return null;

        DrawbackDatabase drawbackDb = DrawbackDatabase.GetOrCreateRuntimeDatabase();
        DrawbackSO definition = drawbackDb.GetById(drawbackId);
        if (definition == null) return null;

        DrawbackRuntimeInstance existing = target.Drawbacks.Find(drawback => drawback != null && drawback.Definition != null &&
            drawback.Definition.Id.Equals(drawbackId, System.StringComparison.OrdinalIgnoreCase));
        if (existing != null) return existing;

        int resolvedDuration = durationTurns >= 0 ? durationTurns : definition.DurationTurns;
        DrawbackRuntimeInstance drawbackInstance = new(definition, target, resolvedDuration, source);
        target.Drawbacks.Add(drawbackInstance);
        OnDrawbackApplied?.Invoke(target, drawbackInstance);

        if (definition.PerkIds != null)
        {
            for (int i = 0; i < definition.PerkIds.Count; i++)
            {
                PerkRuntimeInstance appliedPerk = ApplyPerk(target, definition.PerkIds[i], source, resolvedDuration);
                if (appliedPerk != null) drawbackInstance.ActivePerks.Add(appliedPerk);
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
        if (target == null || definition == null) return null;

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
            if (existing.ActivePerks[i] != null) existing.ActivePerks[i].RemainingTurns = existing.RemainingTurns;
        }
        return existing;
    }

    public void RemoveBattlerState(Battler target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId)) return;

        for (int i = target.ActiveStates.Count - 1; i >= 0; i--)
        {
            BattlerStateRuntimeInstance state = target.ActiveStates[i];
            if (state?.Definition == null || !state.Definition.Id.Equals(stateId, System.StringComparison.OrdinalIgnoreCase)) continue;

            RemoveBattlerStatePerks(target, state);
            OnBattlerStateRemoved?.Invoke(target, state);
            target.ActiveStates.RemoveAt(i);
        }
    }

    private void ApplyBattlerStatePerks(Battler target, Battler source, BattlerStateRuntimeInstance stateInstance, int durationTurns)
    {
        if (stateInstance?.Definition?.PerkIds == null) return;

        for (int i = 0; i < stateInstance.Definition.PerkIds.Count; i++)
        {
            PerkRuntimeInstance appliedPerk = ApplyPerk(target, stateInstance.Definition.PerkIds[i], source, durationTurns);
            if (appliedPerk != null && !stateInstance.ActivePerks.Contains(appliedPerk)) stateInstance.ActivePerks.Add(appliedPerk);
        }
    }

    private void RemoveBattlerStatePerks(Battler target, BattlerStateRuntimeInstance stateInstance)
    {
        if (stateInstance?.ActivePerks == null) return;

        for (int i = stateInstance.ActivePerks.Count - 1; i >= 0; i--)
            RemovePerkInstance(target, stateInstance.ActivePerks[i]);

        stateInstance.ActivePerks.Clear();
    }


    // ==========================================
    // EFFECT CALCULATION & MODIFIERS (DELEGATION)
    // ==========================================
    public int GetEffectiveAttack(Battler actor) => effectResolver.GetEffectiveAttack(actor);
    public int GetEffectiveDefense(Battler actor) => effectResolver.GetEffectiveDefense(actor);
    public int GetEffectiveMind(Battler battler) => effectResolver.GetEffectiveMind(battler);
    public int GetEffectiveHeart(Battler battler) => effectResolver.GetEffectiveHeart(battler);
    public int GetEffectiveBody(Battler battler) => effectResolver.GetEffectiveBody(battler);
    public int GetEffectiveFocus(Battler actor, Battler opponent, ActionType actionType) => effectResolver.GetEffectiveFocus(actor, opponent, actionType);
    public int GetEffectiveStrength(Battler actor, Battler opponent, ActionType actionType) => effectResolver.GetEffectiveStrength(actor, opponent, actionType);
    public int GetExtraPowerDiceAfterAccuracy(Battler actor, Battler opponent, DiceResult accuracyResult, ActionType actionType, out DiceStatType extraDiceStatType) 
        => triggerEvaluator.EvaluateAfterAccuracyTriggers(actor, accuracyResult, actionType, GetEffectivePerks(actor), out extraDiceStatType);
    public (float low, float high) GetModifiedRollThresholds(Battler actor, Battler opponent, CombatRollContext context, float low, float high) 
        => effectResolver.GetModifiedRollThresholds(actor, opponent, context, low, high);
    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context, bool evaluateTriggers = true)
    {
        if (evaluateTriggers)
        {
            triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
            if (opponent != null) triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));
        }
        return effectResolver.GetExtraDiceCount(actor, opponent, context);
    }
    public int GetMinimumRollValue(Battler actor, Battler opponent, CombatRollContext context, int currentMinValue, bool evaluateTriggers = true)
    {
        if (evaluateTriggers)
        {
            triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll, GetEffectivePerks(actor));
            if (opponent != null) triggerEvaluator.EvaluateRollTriggers(opponent, context, PerkTrigger.BeforeRoll, GetEffectivePerks(opponent));
        }
        return effectResolver.GetMinimumRollValue(actor, opponent, context, currentMinValue);
    }
    public float GetPowerMultiplier(float baseMultiplier, ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action?.PowerDice != null)
        {
            CombatActionContext actionContext = new(actor, opponent, actionType);
            triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(actor));
            if (opponent != null) triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.PowerMultiplier, GetEffectivePerks(opponent));
        }
        return effectResolver.GetPowerMultiplier(baseMultiplier, action, actor, opponent, actionType);
    }
    public int ApplyDamageModifiers(int damage, ActionInstance action, Battler actor, Battler opponent, ActionType actionType, ActionInstance opposingAction = null)
    {
        if (damage > 0 && action != null)
        {
            CombatActionContext actionContext = new(actor, opponent, actionType);
            
            List<DiceResult> actionDice = PerkEffectResolver.GetActionDice(action);
            List<DiceResult> opposingActionDice = PerkEffectResolver.GetActionDice(opposingAction);

            if (action.PowerDice != null)
            {
                triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor), actionDice, opposingActionDice);
                if (opponent != null) triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.PowerDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent), actionDice, opposingActionDice);
            }

            if (action.AccuracyDice != null)
            {
                triggerEvaluator.EvaluateDiceTriggers(actor, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(actor), actionDice, opposingActionDice);
                if (opponent != null) triggerEvaluator.EvaluateDiceTriggers(opponent, actionContext, action.AccuracyDice, PerkTrigger.AfterResolve, GetEffectivePerks(opponent), actionDice, opposingActionDice);
            }
        }
        return effectResolver.ApplyDamageModifiers(damage, action, actor, opponent, actionType, opposingAction);
    }


    // ==========================================
    // TRIGGER EVALUATION (DELEGATION)
    // ==========================================
    public void EvaluateActionResolutionTriggers(Battler actor, Battler opponent, ActionType actionType, ActionOutcome outcome, ActionResolutionVariation variation = ActionResolutionVariation.None, int damage = 0, Battler finalTarget = null)
    {
        ActionResolutionContext actionResolutionContext = new()
        {
            Actor = actor,
            Opponent = opponent,
            ActionType = actionType,
            Outcome = outcome,
            ResolutionVariation = variation,
            Damage = damage,
            FinalTarget = finalTarget
        };

        triggerEvaluator.EvaluateActionResolutionTriggers(actor, actionResolutionContext, GetEffectivePerks(actor));
        if (opponent != null) triggerEvaluator.EvaluateActionResolutionTriggers(opponent, actionResolutionContext, GetEffectivePerks(opponent));
    }


    // ==========================================
    // UTILITY & HELPER METHODS
    // ==========================================
    public List<PerkRuntimeInstance> GetEffectivePerks(Battler battler) => PerkRuntimeHelper.GetEffectivePerks(battler);

    private static bool IsSamePerkInstance(PerkRuntimeInstance perk, PerkSO definition, TrickRuntimeInstance sourceTrick)
    {
        if (perk == null || definition == null || !(perk.Definition == definition || perk.Definition?.Id == definition.Id)) return false;
        if (sourceTrick == null) return perk.SourceTrick == null;
        return perk.SourceTrickInstanceId == sourceTrick.InstanceId;
    }

    private static int ResolveDuration(PerkSO definition, int durationTurns, int currentDuration) => PerkRuntimeHelper.ResolveDuration(durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns, definition.DefaultDurationTurns, currentDuration);
    private static int ResolveDuration(int defaultDurationTurns, int durationTurns, int currentDuration) => PerkRuntimeHelper.ResolveDuration(durationTurns, defaultDurationTurns, currentDuration);


    // ==========================================
    // REMOVAL & MANUAL ACTIVATION
    // ==========================================
    public void RemovePerk(Battler target, string perkId)
    {
        if (target == null || string.IsNullOrWhiteSpace(perkId)) return;
        PerkRuntimeInstance instance = target.Perks.Find(perk => perk != null && perk.SourceTrick == null && perk.Definition != null && !string.IsNullOrWhiteSpace(perk.Definition.Id) && perk.Definition.Id.Equals(perkId, System.StringComparison.OrdinalIgnoreCase));
        if (instance == null) return;
        target.Perks.Remove(instance);
        OnPerkRemoved?.Invoke(target, perkId);
    }

    public void RemovePerkInstance(Battler target, PerkRuntimeInstance instance)
    {
        if (target == null || instance == null) return;
        if (target.Perks.Remove(instance)) OnPerkRemoved?.Invoke(target, instance.Definition?.Id);
    }

    public void ExecuteManualActivation(Battler battler, ActionType type, TrickRuntimeInstance trickInstance)
    {
        if (battler == null || trickInstance == null || trickInstance.Definition == null) return;

        int chargesToUse = 1;

        if (trickInstance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            chargesToUse = Mathf.FloorToInt(trickInstance.CurrentCharges);
            if (chargesToUse < 1) return;
        }
        else
        {
            if (trickInstance.IsCoolingDown) return;
        }

        for (int i = 0; i < trickInstance.Definition.PerkIds.Count; i++)
        {
            string perkId = trickInstance.Definition.PerkIds[i];
            PerkSO perkDef = GetPerkDefinition(perkId);
            if (perkDef == null) continue;

            bool isChargeGenerator = false;
            bool hasManualTrigger = false;

            if (perkDef.Rules != null)
            {
                for (int j = 0; j < perkDef.Rules.Count; j++)
                {
                    if (perkDef.Rules[j].ModifierTarget == PerkModifierTarget.TrickCharges)
                        isChargeGenerator = true;

                    if (perkDef.Rules[j].Trigger == PerkTrigger.OnManualActivation)
                        hasManualTrigger = true;
                }
            }

            // 1. Se for o Perk que gera cargas, nós o ignoramos aqui, 
            // pois o TrickInventory já o aplicou no momento do Cast.
            if (isChargeGenerator) continue;

            // 2. Se NÃO for gerador de carga (ex: Buff de Attack), nós aplicamos agora!
            // Duração de 1 turno para que o delta expire no final da rodada atual.
            PerkRuntimeInstance appliedPerk = ApplyPerk(battler, perkDef, battler, 1, chargesToUse);

            // 3. Se esse Perk tiver algum efeito instantâneo acoplado (ex: causar dano imediato),
            // disparamos o gatilho para o PerkStateApplicator agir.
            if (hasManualTrigger && appliedPerk != null)
            {
                triggerEvaluator.EvaluateManualActivationTriggers(battler, type, appliedPerk);
            }
        }

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

        if (trickInstance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            trickInstance.ConsumeCharges();
        }
        else if (trickInstance.Definition.ActivationMode == TrickActivationMode.Active)
        {
            trickInstance.RemainingTurns = 0;
            trickInstance.MarkExpired();
        }

        Logger.Log($"[PerkService] Starting cooldown for trick '{trickInstance.Definition.Id}'.");
        
        trickInstance.StartCooldown(trickInstance.Definition.CooldownTurns);
    }
}
