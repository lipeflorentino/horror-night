using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço centralizado para gerenciar Tricks.
/// Responsável por:
/// - Validar e aplicar tricks
/// - Remover tricks expirados
/// - Gerenciar duração de tricks
/// - Disparar eventos quando tricks são castados/removidos
/// </summary>
public class TrickService
{
    private readonly TrickDatabase database;
    private readonly PerkService perkService;
    
    public event Action<Battler, TrickRuntimeInstance> OnTrickCasted;
    public event Action<Battler, string> OnTrickRemoved;
    public event Action<Battler, TrickRuntimeInstance> OnTrickExpired;
    
    public TrickService(PerkService perkService = null)
    {
        database = TrickDatabase.GetOrCreateRuntimeDatabase();
        this.perkService = perkService;
    }
    
    /// <summary>
    /// Tenta fazer cast de um trick no battler
    /// </summary>
    public bool TryCastTrick(Battler target, string trickId, Battler source = null)
    {
        if (target == null)
            return false;
        
        TrickSO definition = database.GetById(trickId);
        if (definition == null)
        {
            Debug.LogWarning($"[TrickService] Trick '{trickId}' não encontrado!");
            return false;
        }
        
        // Validar se pode fazer cast
        if (!definition.CanCast(target))
        {
            Debug.LogWarning($"[TrickService] {target.Name} não pode fazer cast de '{trickId}'. Level: {target.Level}/{definition.Level}, " +
                           $"Mind: {target.Mind}/{definition.MindCost}, Body: {target.Body}/{definition.BodyCost}, Heart: {target.Heart}/{definition.HeartCost}");
            return false;
        }
        
        // Consumir custo
        target.Mind -= definition.MindCost;
        target.Body -= definition.BodyCost;
        target.Heart -= definition.HeartCost;
        
        // Aplicar trick
        ApplyTrick(target, definition, source);
        
        return true;
    }
    
    /// <summary>
    /// Aplica um trick ao battler, ativando todos os seus perks
    /// </summary>
    private void ApplyTrick(Battler target, TrickSO definition, Battler source = null)
    {
        if (target == null || definition == null)
            return;
        
        TrickRuntimeInstance trickInstance = new(definition, target, definition.DurationTurns);
        
        // Ativar todos os perks do trick
        if (perkService != null)
        {
            for (int i = 0; i < definition.PerkIds.Count; i++)
            {
                string perkId = definition.PerkIds[i];
                perkService.ApplyPerk(target, perkId, source, definition.DurationTurns);
            }
        }
        
        // Adicionar trick ao battler
        target.Tricks.Add(trickInstance);
        
        OnTrickCasted?.Invoke(target, trickInstance);
        
        Debug.Log($"[TrickService] Trick '{definition.DisplayName}' castado em {target.Name}. " +
                  $"Duração: {definition.DurationTurns}, Timing: {definition.Timing}");
    }
    
    /// <summary>
    /// Remove um trick manualmente (antes de expirar)
    /// </summary>
    public void RemoveTrick(Battler target, string trickId)
    {
        if (target == null || target.Tricks.Count == 0)
            return;
        
        TrickRuntimeInstance instance = target.Tricks.Find(t => t.Definition.Id == trickId);
        if (instance == null)
            return;
        
        // Remover perks associados
        if (perkService != null)
        {
            for (int i = 0; i < instance.Definition.PerkIds.Count; i++)
            {
                perkService.RemovePerk(target, instance.Definition.PerkIds[i]);
            }
        }
        
        target.Tricks.Remove(instance);
        OnTrickRemoved?.Invoke(target, trickId);
        
        Debug.Log($"[TrickService] Trick '{instance.Definition.DisplayName}' removido de {target.Name}");
    }
    
    /// <summary>
    /// Processa fim de turno: reduz duração de tricks e remove expirados
    /// </summary>
    public void TickTrickEnd(Battler battler)
    {
        if (battler == null || battler.Tricks.Count == 0)
            return;
        
        for (int i = battler.Tricks.Count - 1; i >= 0; i--)
        {
            TrickRuntimeInstance trick = battler.Tricks[i];
            if (trick == null || trick.Definition == null)
            {
                battler.Tricks.RemoveAt(i);
                continue;
            }
            
            // Tricks permanentes não expiram
            if (trick.RemainingTurns < 0)
                continue;
            
            trick.DecreaseDuration();
            
            // Se expirou, remover
            if (trick.RemainingTurns <= 0)
            {
                RemoveTrick(battler, trick.Definition.Id);
                OnTrickExpired?.Invoke(battler, trick);
            }
        }
    }
    
    /// <summary>
    /// Retorna todos os tricks ativos do battler
    /// </summary>
    public List<TrickRuntimeInstance> GetActiveTricks(Battler battler)
    {
        if (battler == null)
            return new List<TrickRuntimeInstance>();
        
        return battler.Tricks.FindAll(t => t != null && t.IsActive());
    }
    
    /// <summary>
    /// Retorna todos os perks que vieram de tricks (agregado)
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
}
