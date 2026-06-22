using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa uma instância de Trick em tempo de jogo (runtime).
/// Armazena referência ao SO e estado atual (duração, perks ativos, slot, cooldown, etc).
/// </summary>
public class TrickRuntimeInstance
{
    public string InstanceId { get; private set; }
    public TrickSO Definition { get; private set; }
    public Battler Owner { get; private set; }
    public Battler Source { get; private set; }
    public int RemainingTurns { get; set; }
    public int CooldownTurnsRemaining { get; set; }
    public TrickSlotType SlotType { get; private set; }
    public int SlotIndex { get; private set; }
    public bool WasTriggeredThisTurn { get; private set; }
    public float LastTriggeredTime { get; private set; }
    public bool WasExpired { get; private set; }
    public int ActivationDelayTurnsRemaining { get; private set; }
    public bool HasAppliedPerks { get; private set; }
    public float CastTime { get; private set; }
    public float CurrentCharges { get; private set; }
    public bool IsReadyToTrigger => CurrentCharges >= 1f;

    /// <summary>
    /// Perks que foram ativados por este Trick.
    /// </summary>
    public List<PerkRuntimeInstance> ActivePerks { get; set; }

    public bool IsCoolingDown => CooldownTurnsRemaining > 0;

    public TrickRuntimeInstance(
        TrickSO definition,
        Battler owner,
        int durationTurns,
        int? cooldownTurnsRemaining = null,
        TrickSlotType slotType = TrickSlotType.Casted,
        int slotIndex = -1,
        Battler source = null,
        string instanceId = null)
    {
        InstanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
        Definition = definition;
        Owner = owner;
        Source = source;
        RemainingTurns = durationTurns;
        CooldownTurnsRemaining = Mathf.Max(0, cooldownTurnsRemaining != null ? cooldownTurnsRemaining.Value : definition.CooldownTurns > 0 ? definition.CooldownTurns : 0);
        ActivationDelayTurnsRemaining = Mathf.Max(0, definition != null ? definition.TimingTurns : 0);
        SlotType = slotType;
        SlotIndex = slotIndex;
        CastTime = Time.time;
        LastTriggeredTime = -1f;
        WasTriggeredThisTurn = false;
        WasExpired = false;
        ActivePerks = new List<PerkRuntimeInstance>();
    }

    /// <summary>
    /// Retorna true se o trick ainda está ativo.
    /// </summary>
    public bool IsActive()
    {
        return RemainingTurns < 0 || RemainingTurns > 0;
    }

    public void AddCharges(float amount)
    {
        CurrentCharges += amount;
    }

    public void ConsumeCharges()
    {
        CurrentCharges = 0;
    }

    /// <summary>
    /// Compatibilidade com chamadas antigas que consultavam cooldown por método.
    /// </summary>
    public bool HasCooldown()
    {
        return IsCoolingDown;
    }

    public void BindSlot(TrickSlotType slotType, int slotIndex)
    {
        SlotType = slotType;
        SlotIndex = slotIndex;
    }

    public void SetSource(Battler source)
    {
        Source = source;
    }

    public void MarkTriggered()
    {
        WasTriggeredThisTurn = true;
        LastTriggeredTime = Time.time;
    }

    public void ClearTriggeredState()
    {
        WasTriggeredThisTurn = false;
    }

    public void MarkExpired()
    {
        WasExpired = true;
    }

    public void MarkPerksApplied()
    {
        HasAppliedPerks = true;
        ActivationDelayTurnsRemaining = 0;
    }

    public void DecreaseActivationDelay()
    {
        if (ActivationDelayTurnsRemaining > 0)
            ActivationDelayTurnsRemaining--;
    }

    /// <summary>
    /// Reduz a duração em 1 turno.
    /// </summary>
    public void DecreaseDuration()
    {
        if (RemainingTurns > 0)
            RemainingTurns--;
    }

    public void StartCooldown(int cooldownTurns)
    {
        CooldownTurnsRemaining = Mathf.Max(CooldownTurnsRemaining, cooldownTurns);
    }

    /// <summary>
    /// Reduz o cooldown em 1 turno.
    /// </summary>
    public void DecreaseCooldown()
    {
        if (CooldownTurnsRemaining > 0)
            CooldownTurnsRemaining--;
    }

    /// <summary>
    /// Retorna o tempo decorrido desde o cast em segundos.
    /// </summary>
    public float GetElapsedTime()
    {
        return Time.time - CastTime;
    }
}
