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
    public int GetEffectiveAgility(Battler actor, Battler opponent, ActionType actionType) => effectResolver.GetEffectiveAgility(actor, opponent, actionType);
    public int GetExtraPowerDiceAfterAccuracy(Battler actor, DiceResult accuracyResult, ActionType actionType, out DiceStatType extraDiceStatType) 
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
    
    public void EvaluateManualActivationTriggers(Battler battler, ActionType type, PerkRuntimeInstance appliedPerk)
    {
        triggerEvaluator.EvaluateManualActivationTriggers(battler, type, appliedPerk);
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

    // ==========================================
    // REMOVAL
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
}