using System;
using System.Collections.Generic;
using UnityEngine;

public class TrickInventory : ITrickInventory
{
    public const int DefaultIdentitySlotCount = 4;
    public const int DefaultCastedSlotCount = 4;

    private readonly Battler owner;
    private readonly TrickDatabase trickDatabase;
    private readonly PerkService perkService;
    private readonly List<TrickSlot> identitySlots = new();
    private readonly List<TrickSO> learnedTricks = new();
    private readonly List<TrickSlot> castedSlots = new();

    public event Action OnChanged;

    public TrickInventory(
        Battler owner,
        TrickDatabase trickDatabase,
        TrickInventorySnapshot snapshot = null,
        int identitySlotCount = DefaultIdentitySlotCount,
        int castedSlotCount = DefaultCastedSlotCount,
        PerkService perkService = null)
    {
        this.owner = owner;
        this.trickDatabase = trickDatabase ?? TrickDatabase.GetOrCreateRuntimeDatabase();
        this.perkService = perkService;
        
        InitializeSlots(Mathf.Max(1, identitySlotCount), Mathf.Max(1, castedSlotCount));
        RestoreSnapshot(snapshot);
    }

    public IReadOnlyList<TrickSlot> IdentitySlots => identitySlots;
    public IReadOnlyList<TrickSO> LearnedTricks => learnedTricks;
    public IReadOnlyList<TrickSlot> CastedSlots => castedSlots;

    public bool LearnTrick(TrickSO trick)
    {
        if (trick == null || string.IsNullOrWhiteSpace(trick.Id) || HasLearnedTrick(trick.Id) || HasIdentityTrick(trick.Id))
            return false;

        learnedTricks.Add(trick);
        NotifyChanged();
        return true;
    }

    public bool DischardTrick(TrickSO trick)
    {
        if (trick == null || HasIdentityTrick(trick.Id) || IsTrickCasted(trick.Id))
            return false;

        bool removed = learnedTricks.Remove(trick) || learnedTricks.RemoveAll(t => IsSameTrick(t, trick.Id)) > 0;
        if (removed)
            NotifyChanged();

        return removed;
    }

    public bool CastTrick(TrickSO trick, out TrickRuntimeInstance instance)
    {
        instance = null;
        
        Logger.Log($"[TrickInventory] CastTrick: Tentando castar '{trick?.DisplayName}' (ID: {trick?.Id}) para {owner?.Name}");
        
        if (owner == null || trick == null || !HasLearnedTrick(trick.Id) || IsTrickCasted(trick.Id) || IsTrickCoolingDown(trick.Id) || !trick.CanCast(owner))
        {
            Logger.Log($"[TrickInventory] CastTrick falhou: owner={owner != null}, trick={trick != null}, hasLearned={HasLearnedTrick(trick?.Id)}, notCasted={!IsTrickCasted(trick?.Id)}, notCooling={!IsTrickCoolingDown(trick?.Id)}, canCast={trick?.CanCast(owner) ?? false}");
            return false;
        }

        TrickSlot freeSlot = castedSlots.Find(slot => slot != null && slot.IsEmpty && !slot.IsLocked);
        if (freeSlot == null)
        {
            Logger.Log($"[TrickInventory] CastTrick: Nenhum slot vazio disponível para castar '{trick.DisplayName}'.");
            return false;
        }
        
        Logger.Log($"[TrickInventory] CastTrick: Slot encontrado no índice {freeSlot.SlotIndex} para '{trick.DisplayName}'.");

        owner.Mind -= trick.MindCost;
        owner.Body -= trick.BodyCost;
        owner.Heart -= trick.HeartCost;

        Logger.Log($"[TrickInventory] CastTrick: Custos consumidos. Mind-={trick.MindCost}, Body-={trick.BodyCost}, Heart-={trick.HeartCost}.");
        
        instance = new TrickRuntimeInstance(trick, owner, trick.DurationTurns, trick.CooldownTurns, TrickSlotType.Casted, freeSlot.SlotIndex, owner);
        freeSlot.BindRuntimeInstance(instance);
        if (owner.Tricks != null && !owner.Tricks.Contains(instance))
            owner.Tricks.Add(instance);

        // Aplicar os perks da trick aqui para garantir efeito gameplay
        ApplyPerksToInstance(instance);

        Logger.Log($"[TrickInventory] CastTrick: '{trick.DisplayName}' castado com sucesso no slot {freeSlot.SlotIndex}. Perks aplicados.");
        
        NotifyChanged();
        return true;
    }

    public bool RemoveCastedTrick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= castedSlots.Count)
            return false;

        TrickSlot slot = castedSlots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.IsLocked)
            return false;

        if (owner?.Perks != null && slot.RuntimeInstance?.ActivePerks != null)
        {
            for (int i = slot.RuntimeInstance.ActivePerks.Count - 1; i >= 0; i--)
                owner.Perks.Remove(slot.RuntimeInstance.ActivePerks[i]);

            slot.RuntimeInstance.ActivePerks.Clear();
        }

        if (owner?.Tricks != null && slot.RuntimeInstance != null)
            owner.Tricks.Remove(slot.RuntimeInstance);

        slot.Clear();
        NotifyChanged();
        return true;
    }

    public TrickInventorySnapshot GetSnapshot()
    {
        TrickInventorySnapshot snapshot = new();

        for (int i = 0; i < learnedTricks.Count; i++)
        {
            TrickSO trick = learnedTricks[i];
            if (trick != null && !string.IsNullOrWhiteSpace(trick.Id))
                snapshot.learnedTrickIds.Add(trick.Id);
        }

        for (int i = 0; i < identitySlots.Count; i++)
        {
            TrickSO trick = identitySlots[i]?.Definition;
            if (trick != null && !string.IsNullOrWhiteSpace(trick.Id))
                snapshot.identityTrickIds.Add(trick.Id);
        }

        for (int i = 0; i < castedSlots.Count; i++)
        {
            TrickSlot slot = castedSlots[i];
            TrickRuntimeInstance runtimeInstance = slot?.RuntimeInstance;
            TrickSO trick = slot?.Definition;
            if (trick == null || string.IsNullOrWhiteSpace(trick.Id))
                continue;

            snapshot.castedSlots.Add(new CastedTrickSlotSnapshot
            {
                slotIndex = i,
                trickId = trick.Id,
                remainingTurns = runtimeInstance?.RemainingTurns ?? trick.DurationTurns,
                cooldownTurnsRemaining = runtimeInstance?.CooldownTurnsRemaining ?? 0
            });
        }

        return snapshot;
    }

    private void InitializeSlots(int identitySlotCount, int castedSlotCount)
    {
        identitySlots.Clear();
        castedSlots.Clear();

        for (int i = 0; i < identitySlotCount; i++)
            identitySlots.Add(new TrickSlot(TrickSlotType.Identity, i));

        for (int i = 0; i < castedSlotCount; i++)
            castedSlots.Add(new TrickSlot(TrickSlotType.Casted, i));
    }

    private void RestoreSnapshot(TrickInventorySnapshot snapshot)
    {
        learnedTricks.Clear();
        ClearSlots(identitySlots);
        ClearSlots(castedSlots);

        RestoreIdentitySlots(snapshot?.identityTrickIds);
        RestoreLearnedTricks(snapshot?.learnedTrickIds);
        RestoreCastedSlots(snapshot?.castedSlots);
    }

    private void RestoreIdentitySlots(List<string> trickIds)
    {
        if (trickIds == null)
        {
            return;
        }
        
        int count = Math.Min(trickIds.Count, identitySlots.Count);
        for (int i = 0; i < count; i++)
        {
            TrickSO trick = FindTrick(trickIds[i]);
            if (trick != null)
            {
                TrickRuntimeInstance instance = new(trick, owner, trick.DurationTurns, 0, TrickSlotType.Identity, i, owner);
                identitySlots[i].BindRuntimeInstance(instance);
                if (owner?.Tricks != null && !owner.Tricks.Contains(instance))
                    owner.Tricks.Add(instance);
            }
        }        
    }

    private void RestoreLearnedTricks(List<string> trickIds)
    {
        if (trickIds == null)
        {
            return;
        }

        for (int i = 0; i < trickIds.Count; i++)
        {
            TrickSO trick = FindTrick(trickIds[i]);
            if (trick != null)
            {
                LearnTrick(trick);
            }
        }
    }

    private void RestoreCastedSlots(List<CastedTrickSlotSnapshot> snapshots)
    {
        if (snapshots == null)
            return;

        for (int i = 0; i < snapshots.Count; i++)
        {
            CastedTrickSlotSnapshot slotSnapshot = snapshots[i];
            if (slotSnapshot.slotIndex < 0 || slotSnapshot.slotIndex >= castedSlots.Count)
                continue;

            TrickSO trick = FindTrick(slotSnapshot.trickId);
            if (trick == null)
                continue;

            TrickRuntimeInstance instance = new(trick, owner, slotSnapshot.remainingTurns, slotSnapshot.cooldownTurnsRemaining, TrickSlotType.Casted, slotSnapshot.slotIndex, owner);
            castedSlots[slotSnapshot.slotIndex].BindRuntimeInstance(instance);
            if (owner?.Tricks != null && !owner.Tricks.Contains(instance))
                owner.Tricks.Add(instance);
            
            ApplyPerksToInstance(instance);
        }
    }

    private TrickSO FindTrick(string trickId)
    {
        if (string.IsNullOrWhiteSpace(trickId))
            return null;

        return trickDatabase != null ? trickDatabase.GetById(trickId) : null;
    }

    private bool HasLearnedTrick(string trickId)
    {
        return learnedTricks.Exists(trick => IsSameTrick(trick, trickId));
    }

    private bool HasIdentityTrick(string trickId)
    {
        return identitySlots.Exists(slot => IsSameTrick(slot?.Definition, trickId));
    }

    private bool IsTrickCasted(string trickId)
    {
        return castedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId));
    }

    private bool IsTrickCoolingDown(string trickId)
    {
        return castedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId) && slot.RuntimeInstance != null && slot.RuntimeInstance.IsCoolingDown);
    }

    private void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    private static bool IsSameTrick(TrickSO trick, string trickId)
    {
        return trick != null && !string.IsNullOrWhiteSpace(trick.Id) && trick.Id.Equals(trickId, StringComparison.OrdinalIgnoreCase);
    }

    private static void ClearSlots(List<TrickSlot> slots)
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.Clear();
    }

    /// <summary>
    /// Aplica os perks de uma trick à instância, garantindo que efeitos gameplay sejam ativados.
    /// Chamado tanto em CastTrick quanto em RestoreCastedSlots para manter consistência.
    /// NOTA: O cooldown já está definido no construtor via nullable default, não precisa chamar StartCooldown aqui.
    /// </summary>
    private void ApplyPerksToInstance(TrickRuntimeInstance instance)
    {
        if (instance == null || instance.Definition == null || instance.ActivationDelayTurnsRemaining > 0)
            return;

        instance.ActivePerks.Clear();

        if (perkService != null)
        {
            for (int i = 0; i < instance.Definition.PerkIds.Count; i++)
            {
                string perkId = instance.Definition.PerkIds[i];
                PerkRuntimeInstance perk = perkService.ApplyPerkFromTrick(
                    owner, perkId, instance, instance.Source ?? owner, instance.Definition.DurationTurns);
                if (perk != null && !instance.ActivePerks.Contains(perk))
                    instance.ActivePerks.Add(perk);
            }

            instance.MarkPerksApplied();
        }
    }
}
