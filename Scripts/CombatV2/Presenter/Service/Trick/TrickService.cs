using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço centralizado para gerenciar Tricks.
/// Responsável por:
/// - Validar e aplicar tricks
/// - Remover tricks expirados
/// - Gerenciar duração/cooldown de tricks
/// - Disparar eventos quando tricks são castados/removidos/alterados
/// </summary>
public class TrickService
{
    // ==========================================
    // DEPENDENCIES, STATE & EVENTS
    // ==========================================
    private readonly PerkService perkService;
    private readonly DrawbackService drawbackService;
    
    public event Action<Battler, TrickRuntimeInstance> OnTrickCasted, OnTrickActivated;
    public event Action<Battler, string> OnTrickRemoved;
    public event Action<Battler, TrickRuntimeInstance> OnTrickExpired;
    public event Action<Battler, TrickRuntimeInstance> OnTrickChanged;


    // ==========================================
    // INITIALIZATION
    // ==========================================
    public TrickService(PerkService perkService = null, DrawbackService drawbackService = null)
    {
        this.perkService = perkService;
        this.drawbackService = drawbackService;
    }


    // ==========================================
    // TRICK CASTING & ACTIVATION
    // ==========================================
    public bool TryCastTrick(Battler target, ITrickInventory trickInventory, TrickSO definition, Battler source = null)
    {
        if (target == null || trickInventory == null || definition == null)
        {
            Logger.Log($"[TrickService] Não foi possível castar o trick '{definition?.Id ?? "null"}' para {target?.Name ?? "null"}.");
            return false;
        }

        if (!trickInventory.CastTrick(definition, out TrickRuntimeInstance instance) || instance == null)
        {
            Logger.Log($"[TrickService] Falha ao castar o trick '{definition.DisplayName}' para {target.Name}. Verifique slots castados, cooldown e recursos.");
            return false;
        }

        instance.SetSource(source ?? target);
        ApplyTrick(target, instance, source ?? target);
        return true;
    }

    public bool TryManualActivation(Battler target, ActionType actionType, TrickRuntimeInstance instance)
    {
        if (target == null || instance == null || instance.Definition == null) return false;
        if (!instance.IsReadyToTrigger) return false;

        int chargesToUse = 1;

        if (instance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            chargesToUse = Mathf.FloorToInt(instance.CurrentCharges);
            if (chargesToUse < 1) return false;
        }
        else
        {
            if (instance.IsCoolingDown) return false;
        }

        bool activatedAny = false;

        if (perkService != null)
        {
            for (int i = 0; i < instance.Definition.PerkIds.Count; i++)
            {
                string perkId = instance.Definition.PerkIds[i];
                PerkSO perkDef = perkService.GetPerkDefinition(perkId);
                if (perkDef == null) continue;

                bool isChargeGenerator = false;
                bool hasManualTrigger = false;

                if (perkDef.Rule != null)
                {
                    if (perkDef.Rule.ModifierTarget == PerkModifierTarget.TrickCharges)
                        isChargeGenerator = true;

                    if (perkDef.Rule.Trigger == PerkTrigger.OnManualActivation)
                        hasManualTrigger = true;
                }

                if (isChargeGenerator) continue;

                PerkRuntimeInstance appliedPerk = perkService.ApplyPerk(target, perkDef, target, 1, chargesToUse);

                if (hasManualTrigger && appliedPerk != null)
                {
                    perkService.EvaluateManualActivationTriggers(target, actionType, appliedPerk);
                }
                
                activatedAny = true;
            }
        }

        if (drawbackService != null && instance.Definition.DrawbackIds != null && instance.Definition.DrawbackIds.Count > 0)
        {
            DrawbackDatabase drawbackDb = DrawbackDatabase.GetOrCreateRuntimeDatabase();
            for (int i = 0; i < instance.Definition.DrawbackIds.Count; i++)
            {
                DrawbackSO drawback = drawbackDb.GetById(instance.Definition.DrawbackIds[i]);
                if (drawback != null)
                {
                    int rolledDuration = drawback.RollDuration();
                    drawbackService.ApplyDrawback(target, drawback.Id, target, rolledDuration);
                }
            }
        }

        if (instance.Definition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            instance.ConsumeCharges();
        }
        else if (instance.Definition.ActivationMode == TrickActivationMode.Active)
        {
            instance.RemainingTurns = 0;
            instance.MarkExpired();
        }

        Logger.Log($"[TrickService] Starting cooldown for trick '{instance.Definition.Id}'.");
        instance.StartCooldown(instance.Definition.CooldownTurns);
        
        if (activatedAny)
        {
            OnTrickActivated?.Invoke(target, instance);
        }

        return true;
    }

    public TrickRuntimeInstance ApplyTrick(Battler target, TrickRuntimeInstance trickInstance, Battler source = null)
    {
        if (target == null || trickInstance?.Definition == null)
        {
            Logger.Log($"[TrickService] Não foi possível aplicar o trick '{trickInstance?.Definition?.Id ?? "null"}' para {target?.Name ?? "null"}.");
            return null;
        }

        trickInstance.StartCooldown(trickInstance.Definition.CooldownTurns);

        if (trickInstance.ActivationDelayTurnsRemaining == 0)
            ApplyPerks(target, trickInstance, source ?? target);

        if (target.Tricks != null && !target.Tricks.Contains(trickInstance))
            target.Tricks.Add(trickInstance);

        OnTrickCasted?.Invoke(target, trickInstance);
        OnTrickChanged?.Invoke(target, trickInstance);

        return trickInstance;
    }


    // ==========================================
    // TRICK REMOVAL
    // ==========================================
    public void RemoveTrick(Battler target, string trickId)
    {
        if (target == null || target.Tricks.Count == 0)
            return;

        TrickRuntimeInstance instance = target.Tricks.Find(t => t.Definition.Id == trickId);
        if (instance == null)
            return;

        RemoveActivePerks(target, instance);
        target.Tricks.Remove(instance);
        OnTrickRemoved?.Invoke(target, trickId);
        OnTrickChanged?.Invoke(target, instance);
    }


    // ==========================================
    // TURN LIFECYCLE & EXPIRATION
    // ==========================================
    public void TickTrickEnd(Battler battler, ITrickInventory trickInventory)
    {
        if (battler == null)
            return;

        if (trickInventory == null)
        {
            Logger.Log($"[TrickService] TickTrickEnd chamado para {battler.Name} sem inventário. Slots castados não serão limpos quando tricks expirarem!");
        }
        else
        {
            trickInventory.TickCooldowns();
        }

        if (battler.Tricks.Count == 0)
            return;

        List<TrickRuntimeInstance> tricksToRemove = new();

        for (int i = battler.Tricks.Count - 1; i >= 0; i--)
        {
            TrickRuntimeInstance trick = battler.Tricks[i];
            if (trick == null || trick.Definition == null)
            {
                battler.Tricks.RemoveAt(i);
                continue;
            }

            bool changed = false;

            if (trick.IsNew)
            {
                trick.IsNew = false;
                if (trick.WasTriggeredThisTurn)
                {
                    trick.ClearTriggeredState();
                    changed = true;
                }
                if (changed) OnTrickChanged?.Invoke(battler, trick);
                continue;
            }

            if (trick.WasTriggeredThisTurn)
            {
                trick.ClearTriggeredState();
                changed = true;
            }

            if (trick.IsCoolingDown && trick.WasExpired)
            {
                trick.DecreaseCooldown();
                changed = true;
            }

            if (!trick.HasAppliedPerks && trick.ActivationDelayTurnsRemaining > 0)
            {
                trick.DecreaseActivationDelay();
                changed = true;
            }

            if (!trick.HasAppliedPerks && trick.ActivationDelayTurnsRemaining == 0)
            {
                ApplyPerks(target: battler, trickInstance: trick, source: trick.Source ?? battler);
                changed = true;
            }

            if (trick.RemainingTurns > 0)
            {
                trick.DecreaseDuration();
                changed = true;
            }
            
            if (trick.RemainingTurns == 0 && !trick.WasExpired)
            {
                RemoveActivePerks(battler, trick);
                trick.MarkExpired();
                OnTrickExpired?.Invoke(battler, trick);
                changed = true;
            }

            if (trick.RemainingTurns == 0 && !trick.IsCoolingDown)
            {
                tricksToRemove.Add(trick);
            }

            if (changed)
                OnTrickChanged?.Invoke(battler, trick);
        }

        foreach (var trickToRemove in tricksToRemove)
        {
            if (!trickToRemove.WasExpired)
            {
                RemoveActivePerks(battler, trickToRemove);
                trickToRemove.MarkExpired();
                OnTrickExpired?.Invoke(battler, trickToRemove);
            }

            if ((trickToRemove.SlotType == TrickSlotType.CastedActive || trickToRemove.SlotType == TrickSlotType.CastedPassive) && trickToRemove.SlotIndex >= 0)
            {
                if (trickInventory != null)
                {
                    trickInventory.RemoveCastedTrick(trickToRemove.SlotType, trickToRemove.SlotIndex);
                }
            }
            
            if (battler.Tricks.Contains(trickToRemove))
            {
                battler.Tricks.Remove(trickToRemove);
            }

            if (trickToRemove.Definition != null)
            {
                OnTrickRemoved?.Invoke(battler, trickToRemove.Definition.Id);
            }
        }
    }


    // ==========================================
    // UTILITY & HELPER METHODS
    // ==========================================
    public List<TrickRuntimeInstance> GetActiveTricks(Battler battler)
    {
        if (battler == null)
            return new List<TrickRuntimeInstance>();

        return battler.Tricks.FindAll(t => t != null && t.IsActive());
    }

    public List<PerkRuntimeInstance> GetPerksFromTricks(Battler battler)
    {
        List<PerkRuntimeInstance> perks = new();

        if (battler == null)
            return perks;

        for (int i = 0; i < battler.Tricks.Count; i++)
        {
            if (battler.Tricks[i]?.ActivePerks != null)
                perks.AddRange(battler.Tricks[i].ActivePerks);
        }

        return perks;
    }


    // ==========================================
    // INTERNAL PERK MANAGEMENT
    // ==========================================
    private void ApplyPerks(Battler target, TrickRuntimeInstance trickInstance, Battler source)
    {
        if (target == null || trickInstance?.Definition == null || trickInstance.HasAppliedPerks)
        {
            return;
        }

        trickInstance.ActivePerks.Clear();

        if (perkService != null)
        {
            for (int i = 0; i < trickInstance.Definition.PerkIds.Count; i++)
            {
                string perkId = trickInstance.Definition.PerkIds[i];
                PerkRuntimeInstance perk = perkService.ApplyPerkFromTrick(target, perkId, trickInstance, source ?? target, trickInstance.Definition.DurationTurns);
                if (perk != null && !trickInstance.ActivePerks.Contains(perk))
                    trickInstance.ActivePerks.Add(perk);
            }
        }

        trickInstance.MarkPerksApplied();
    }

    private void RemoveActivePerks(Battler target, TrickRuntimeInstance trick)
    {
        if (target == null || trick == null || trick.ActivePerks == null)
            return;

        if (perkService != null)
        {
            for (int i = trick.ActivePerks.Count - 1; i >= 0; i--)
                perkService.RemovePerkInstance(target, trick.ActivePerks[i]);
        }

        trick.ActivePerks.Clear();
    }
}