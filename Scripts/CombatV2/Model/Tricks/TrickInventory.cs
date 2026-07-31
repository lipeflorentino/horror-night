using System;
using System.Collections.Generic;
using UnityEngine;

public class TrickInventory : ITrickInventory
{
    public const int DefaultIdentitySlotCount = 4;
    public const int DefaultActiveCastedSlotCount = 4;
    public const int DefaultPassiveCastedSlotCount = 4;

    private readonly Battler owner;
    private readonly TrickDatabase trickDatabase;
    private readonly PerkService perkService;
    private readonly List<TrickSlot> identitySlots = new();
    private readonly List<TrickSO> learnedTricks = new();
    private readonly List<TrickSlot> activeCastedSlots = new();
    private readonly List<TrickSlot> passiveCastedSlots = new();
    private readonly Dictionary<string, int> cooldownTurnsByTrickId = new(StringComparer.OrdinalIgnoreCase);

    public event Action OnChanged;

    public TrickInventory(
        Battler owner,
        TrickDatabase trickDatabase,
        TrickInventorySnapshot snapshot = null,
        int identitySlotCount = DefaultIdentitySlotCount,
        int activeCastedSlotCount = DefaultActiveCastedSlotCount,
        int passiveCastedSlotCount = DefaultPassiveCastedSlotCount,
        PerkService perkService = null)
    {
        this.owner = owner;
        this.trickDatabase = trickDatabase ?? TrickDatabase.GetOrCreateRuntimeDatabase();
        this.perkService = perkService;
        
        InitializeSlots(Mathf.Max(1, identitySlotCount), Mathf.Max(1, activeCastedSlotCount), Mathf.Max(1, passiveCastedSlotCount));
        RestoreSnapshot(snapshot);
    }

    public IReadOnlyList<TrickSlot> IdentitySlots => identitySlots;
    public IReadOnlyList<TrickSO> LearnedTricks => learnedTricks;
    public IReadOnlyList<TrickSlot> ActiveCastedSlots => activeCastedSlots;
    public IReadOnlyList<TrickSlot> PassiveCastedSlots => passiveCastedSlots;

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
        ClearCooldown(trick.Id);
        if (removed)
            NotifyChanged();

        return removed;
    }

    public bool CastTrick(TrickSO trick, out TrickRuntimeInstance instance)
    {
        instance = null;

        if (owner == null || trick == null)
        {
            Logger.Log($"[TrickInventory] Não foi possível castar o trick '{trick?.Id ?? "null"}' para {owner?.Name ?? "null"}.");
            return false;
        }

        bool hasLearned = HasLearnedTrick(trick.Id);
        bool isCasted = IsTrickCasted(trick.Id);
        bool isCoolingDown = IsTrickCoolingDown(trick.Id);
        bool canCast = trick.CanCast(owner, perkService);

        Logger.Log($"[TrickInventory] CastTrick check '{trick.Id}': hasLearned={hasLearned}, isCasted={isCasted}, isCoolingDown={isCoolingDown}, canCast={canCast}");

        if (!hasLearned || isCasted || isCoolingDown || !canCast)
        {
            Logger.Log($"[TrickInventory] Não foi possível castar o trick '{trick.Id ?? "null"}' para {owner?.Name ?? "null"}. Verifique se o trick foi aprendido, se já está castado, se está em cooldown ou se os requisitos são atendidos.");
            return false;
        }

        List<TrickSlot> targetSlots = GetCastedSlotsForTrick(trick);
        TrickSlotType slotType = GetCastedSlotTypeForTrick(trick);
        TrickSlot freeSlot = targetSlots?.Find(slot => slot != null && slot.IsEmpty && !slot.IsLocked);

        if (freeSlot == null)
        {
            Logger.Log($"[TrickInventory] Não foi possível castar o trick '{trick.Id ?? "null"}' para {owner?.Name ?? "null"}. Não há slots livres.");
            return false;
        }

        owner.SpendMomentum(trick.MomentumCost);
        
        instance = new TrickRuntimeInstance(trick, owner, trick.DurationTurns, trick.CooldownTurns, slotType, freeSlot.SlotIndex, owner);
        freeSlot.BindRuntimeInstance(instance);

        if (owner.Tricks != null && !owner.Tricks.Contains(instance))
            owner.Tricks.Add(instance);

        ApplyPerksToInstance(instance);
        NotifyChanged();

        return true;
    }

    public bool RemoveCastedTrick(TrickSlotType slotType, int slotIndex)
    {
        List<TrickSlot> slots = GetCastedSlots(slotType);
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        TrickSlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.IsLocked)
            return false;

        if (owner?.Perks != null && slot.RuntimeInstance?.ActivePerks != null)
        {
            for (int i = slot.RuntimeInstance.ActivePerks.Count - 1; i >= 0; i--)
                owner.Perks.Remove(slot.RuntimeInstance.ActivePerks[i]);

            slot.RuntimeInstance.ActivePerks.Clear();
        }

        RegisterCooldown(slot.RuntimeInstance);

        if (owner?.Tricks != null && slot.RuntimeInstance != null)
            owner.Tricks.Remove(slot.RuntimeInstance);

        slot.Clear();
        NotifyChanged();
        
        return true;
    }

    public void TickCooldowns()
    {
        if (cooldownTurnsByTrickId.Count == 0)
            return;

        List<string> trickIds = new(cooldownTurnsByTrickId.Keys);
        bool changed = false;

        for (int i = 0; i < trickIds.Count; i++)
        {
            string trickId = trickIds[i];
            int remainingTurns = Mathf.Max(0, cooldownTurnsByTrickId[trickId] - 1);
            if (remainingTurns <= 0)
                cooldownTurnsByTrickId.Remove(trickId);
            else
                cooldownTurnsByTrickId[trickId] = remainingTurns;

            changed = true;
        }

        if (changed)
            NotifyChanged();
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

        AddCastedSlotsToSnapshot(snapshot, activeCastedSlots);
        AddCastedSlotsToSnapshot(snapshot, passiveCastedSlots);
        AddCooldownsToSnapshot(snapshot);

        return snapshot;
    }

    private void AddCooldownsToSnapshot(TrickInventorySnapshot snapshot)
    {
        foreach (KeyValuePair<string, int> cooldown in cooldownTurnsByTrickId)
        {
            if (string.IsNullOrWhiteSpace(cooldown.Key) || cooldown.Value <= 0)
                continue;

            snapshot.cooldowns.Add(new TrickCooldownSnapshot
            {
                trickId = cooldown.Key,
                cooldownTurnsRemaining = cooldown.Value
            });
        }
    }

    private static void AddCastedSlotsToSnapshot(TrickInventorySnapshot snapshot, List<TrickSlot> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            TrickSlot slot = slots[i];
            TrickRuntimeInstance runtimeInstance = slot?.RuntimeInstance;
            TrickSO trick = slot?.Definition;
            if (trick == null || string.IsNullOrWhiteSpace(trick.Id))
                continue;

            snapshot.castedSlots.Add(new CastedTrickSlotSnapshot
            {
                slotType = slot.SlotType,
                slotIndex = i,
                trickId = trick.Id,
                remainingTurns = runtimeInstance?.RemainingTurns ?? trick.DurationTurns,
                cooldownTurnsRemaining = runtimeInstance?.CooldownTurnsRemaining ?? 0
            });
        }

    }

    private void InitializeSlots(int identitySlotCount, int activeCastedSlotCount, int passiveCastedSlotCount)
    {
        identitySlots.Clear();
        activeCastedSlots.Clear();
        passiveCastedSlots.Clear();

        for (int i = 0; i < identitySlotCount; i++)
            identitySlots.Add(new TrickSlot(TrickSlotType.Identity, i));

        for (int i = 0; i < activeCastedSlotCount; i++)
            activeCastedSlots.Add(new TrickSlot(TrickSlotType.CastedActive, i));

        for (int i = 0; i < passiveCastedSlotCount; i++)
            passiveCastedSlots.Add(new TrickSlot(TrickSlotType.CastedPassive, i));
    }

    private void RestoreSnapshot(TrickInventorySnapshot snapshot)
    {
        learnedTricks.Clear();
        cooldownTurnsByTrickId.Clear();
        ClearSlots(identitySlots);
        ClearSlots(activeCastedSlots);
        ClearSlots(passiveCastedSlots);

        RestoreIdentitySlots(snapshot?.identityTrickIds);
        RestoreLearnedTricks(snapshot?.learnedTrickIds);
        RestoreCooldowns(snapshot?.cooldowns);
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
            List<TrickSlot> slots = GetCastedSlots(slotSnapshot.slotType);
            if (slots == null || slotSnapshot.slotIndex < 0 || slotSnapshot.slotIndex >= slots.Count)
                continue;

            TrickSO trick = FindTrick(slotSnapshot.trickId);
            if (trick == null)
                continue;

            TrickRuntimeInstance instance = new(trick, owner, slotSnapshot.remainingTurns, slotSnapshot.cooldownTurnsRemaining, slotSnapshot.slotType, slotSnapshot.slotIndex, owner);
            slots[slotSnapshot.slotIndex].BindRuntimeInstance(instance);
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
        return activeCastedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId)) || passiveCastedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId));
    }

    private bool IsTrickCoolingDown(string trickId)
    {
        return HasRegisteredCooldown(trickId)
            || activeCastedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId) && slot.RuntimeInstance != null && slot.RuntimeInstance.IsCoolingDown)
            || passiveCastedSlots.Exists(slot => IsSameTrick(slot?.Definition, trickId) && slot.RuntimeInstance != null && slot.RuntimeInstance.IsCoolingDown);
    }

    private bool HasRegisteredCooldown(string trickId)
    {
        return !string.IsNullOrWhiteSpace(trickId)
            && cooldownTurnsByTrickId.TryGetValue(trickId, out int remainingTurns)
            && remainingTurns > 0;
    }

    private void RegisterCooldown(TrickRuntimeInstance instance)
    {
        if (instance?.Definition == null || string.IsNullOrWhiteSpace(instance.Definition.Id) || instance.CooldownTurnsRemaining <= 0)
            return;

        cooldownTurnsByTrickId[instance.Definition.Id] = Mathf.Max(
            GetRegisteredCooldown(instance.Definition.Id),
            instance.CooldownTurnsRemaining);
    }

    private int GetRegisteredCooldown(string trickId)
    {
        return !string.IsNullOrWhiteSpace(trickId) && cooldownTurnsByTrickId.TryGetValue(trickId, out int remainingTurns)
            ? remainingTurns
            : 0;
    }

    private void ClearCooldown(string trickId)
    {
        if (!string.IsNullOrWhiteSpace(trickId))
            cooldownTurnsByTrickId.Remove(trickId);
    }

    private void RestoreCooldowns(List<TrickCooldownSnapshot> cooldowns)
    {
        if (cooldowns == null)
            return;

        for (int i = 0; i < cooldowns.Count; i++)
        {
            TrickCooldownSnapshot cooldown = cooldowns[i];
            if (string.IsNullOrWhiteSpace(cooldown.trickId) || cooldown.cooldownTurnsRemaining <= 0)
                continue;

            cooldownTurnsByTrickId[cooldown.trickId] = Mathf.Max(
                GetRegisteredCooldown(cooldown.trickId),
                cooldown.cooldownTurnsRemaining);
        }
    }

    private List<TrickSlot> GetCastedSlotsForTrick(TrickSO trick)
    {
        return GetCastedSlots(GetCastedSlotTypeForTrick(trick));
    }

    private static TrickSlotType GetCastedSlotTypeForTrick(TrickSO trick)
    {
        return trick != null && trick.ActivationMode == TrickActivationMode.Passive
            ? TrickSlotType.CastedPassive
            : TrickSlotType.CastedActive;
    }

    private List<TrickSlot> GetCastedSlots(TrickSlotType slotType)
    {
        return slotType switch
        {
            TrickSlotType.CastedActive => activeCastedSlots,
            TrickSlotType.CastedPassive => passiveCastedSlots,
            _ => null,
        };
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
