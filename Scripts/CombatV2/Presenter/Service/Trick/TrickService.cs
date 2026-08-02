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
    private readonly TrickDatabase database;
    private readonly PerkService perkService;

    public event Action<Battler, TrickRuntimeInstance> OnTrickCasted;
    public event Action<Battler, string> OnTrickRemoved;
    public event Action<Battler, TrickRuntimeInstance> OnTrickExpired;
    public event Action<Battler, TrickRuntimeInstance> OnTrickChanged;

    public TrickService(PerkService perkService = null)
    {
        database = TrickDatabase.GetOrCreateRuntimeDatabase();
        this.perkService = perkService;
    }

    /// <summary>
    /// Processa fim de turno: reduz duração/cooldown e expira perks quando necessário.
    /// Precisa receber o inventário para limpar corretamente slots castados quando tricks expiram.
    /// É OBRIGATÓRIO passar o inventário para evitar que tricks castados bloqueiem slots permanentemente.
    /// </summary>
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

            if (trick.WasTriggeredThisTurn)
            {
                trick.ClearTriggeredState();
                changed = true;
            }

            // CORREÇÃO AQUI: O cooldown só diminui se a trick já tiver expirado (duração finalizada)
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
            
            // Expiração do Efeito (Duração chega a zero)
            // Note que como isso ocorre DEPOIS da checagem do cooldown lá em cima, 
            // no turno em que ela expira, o cooldown ainda não conta. Só começa no próximo.
            if (trick.RemainingTurns == 0 && !trick.WasExpired)
            {
                RemoveActivePerks(battler, trick);
                trick.MarkExpired();
                OnTrickExpired?.Invoke(battler, trick);
                changed = true;
            }

            // Remoção do Slot (Duração 0 E Cooldown 0)
            if (trick.RemainingTurns == 0 && !trick.IsCoolingDown)
            {
                tricksToRemove.Add(trick);
            }

            if (changed)
                OnTrickChanged?.Invoke(battler, trick);
        }

        foreach (var trickToRemove in tricksToRemove)
        {
            // Garantia (caso a trick tenha duração 0 e cooldown 0 e caia aqui direto)
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

    /// <summary>
    /// Tenta fazer cast usando o TrickInventory, respeitando slots castados, recursos e cooldown.
    /// </summary>
    public bool TryCastTrick(Battler target, ITrickInventory trickInventory, string trickId, Battler source = null)
    {
        TrickSO definition = database.GetById(trickId);
        return TryCastTrick(target, trickInventory, definition, source);
    }

    /// <summary>
    /// Tenta fazer cast usando o TrickInventory, respeitando slots castados, recursos e cooldown.
    /// </summary>
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

    /// <summary>
    /// Ativa os perks de uma instância runtime já criada/alocada em slot.
    /// </summary>
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

        Logger.Log($"[TrickService] Trick '{trickInstance.Definition.DisplayName}' castado em {target.Name}. " +
                  $"Duração: {trickInstance.Definition.DurationTurns}, Cooldown: {trickInstance.CooldownTurnsRemaining}, TimingTurns: {trickInstance.Definition.TimingTurns}");

        return trickInstance;
    }

    /// <summary>
    /// Remove um trick manualmente (antes de expirar).
    /// </summary>
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



    /// <summary>
    /// Retorna todos os tricks ativos do battler.
    /// </summary>
    public List<TrickRuntimeInstance> GetActiveTricks(Battler battler)
    {
        if (battler == null)
            return new List<TrickRuntimeInstance>();

        return battler.Tricks.FindAll(t => t != null && t.IsActive());
    }

    /// <summary>
    /// Retorna todos os perks que vieram de tricks (agregado).
    /// </summary>
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
