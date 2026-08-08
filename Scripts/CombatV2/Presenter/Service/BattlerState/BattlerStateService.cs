using System;
using System.Collections.Generic;

public class BattlerStateService
{
    private readonly PerkService perkService;

    public event Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateApplied;
    public event Action<Battler, BattlerStateRuntimeInstance> OnBattlerStateRemoved;

    public BattlerStateService(PerkService perkService)
    {
        this.perkService = perkService;
    }

    public BattlerStateRuntimeInstance ApplyBattlerState(Battler target, string stateId, Battler source = null, int durationTurns = -1)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId)) return null;

        BattlerStateSO definition = BattlerStateDatabase.GetOrCreateRuntimeDatabase().GetById(stateId);
        if (definition == null) return null;

        BattlerStateRuntimeInstance existing = target.ActiveStates.Find(state => state != null &&
            state.Definition != null &&
            !string.IsNullOrWhiteSpace(state.Definition.Id) &&
            state.Definition.Id.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));

        int resolvedDuration = durationTurns >= 0 ? durationTurns : definition.DefaultDurationTurns;
        
        if (existing == null || definition.StackMode == PerkStackMode.Replace)
        {
            if (existing != null)
            {
                RemoveBattlerStatePerks(target, existing);
                target.ActiveStates.Remove(existing);
            }

            BattlerStateRuntimeInstance stateInstance = new BattlerStateRuntimeInstance(definition, target, source, resolvedDuration);
            target.ActiveStates.Add(stateInstance);
            ApplyBattlerStatePerks(target, source, stateInstance, resolvedDuration);
            OnBattlerStateApplied?.Invoke(target, stateInstance);
            return stateInstance;
        }

        existing.Source = source;
        existing.RemainingTurns = PerkRuntimeHelper.ResolveDuration(definition.DefaultDurationTurns, resolvedDuration, existing.RemainingTurns);
        
        if (existing.ActivePerks != null)
        {
            for (int i = 0; i < existing.ActivePerks.Count; i++)
            {
                if (existing.ActivePerks[i] != null) 
                    existing.ActivePerks[i].RemainingTurns = existing.RemainingTurns;
            }
        }
        return existing;
    }

    public void RemoveBattlerState(Battler target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId)) return;

        for (int i = target.ActiveStates.Count - 1; i >= 0; i--)
        {
            BattlerStateRuntimeInstance state = target.ActiveStates[i];
            if (state?.Definition == null || !state.Definition.Id.Equals(stateId, StringComparison.OrdinalIgnoreCase)) continue;

            RemoveBattlerStatePerks(target, state);
            OnBattlerStateRemoved?.Invoke(target, state);
            target.ActiveStates.RemoveAt(i);
        }
    }

    public void TickTurnEnd(Battler battler)
    {
        if (battler == null || battler.ActiveStates.Count == 0) return;

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
                RemoveBattlerStatePerks(battler, state);
                OnBattlerStateRemoved?.Invoke(battler, state);
                battler.ActiveStates.RemoveAt(i);
            }
        }
    }

    private void ApplyBattlerStatePerks(Battler target, Battler source, BattlerStateRuntimeInstance stateInstance, int durationTurns)
    {
        if (perkService == null || stateInstance?.Definition?.PerkIds == null) return;

        for (int i = 0; i < stateInstance.Definition.PerkIds.Count; i++)
        {
            PerkRuntimeInstance appliedPerk = perkService.ApplyPerk(target, stateInstance.Definition.PerkIds[i], source, durationTurns);
            if (appliedPerk != null && !stateInstance.ActivePerks.Contains(appliedPerk)) 
            {
                stateInstance.ActivePerks.Add(appliedPerk);
            }
        }
    }

    private void RemoveBattlerStatePerks(Battler target, BattlerStateRuntimeInstance stateInstance)
    {
        if (perkService == null || stateInstance?.ActivePerks == null) return;

        for (int i = stateInstance.ActivePerks.Count - 1; i >= 0; i--)
        {
            perkService.RemovePerkInstance(target, stateInstance.ActivePerks[i]);
        }

        stateInstance.ActivePerks.Clear();
    }
}