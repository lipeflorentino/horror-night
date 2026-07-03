using System.Collections.Generic;
using UnityEngine;

public static class PerkRuntimeHelper
{
    public static int ResolveDuration(int requestedDuration, int defaultDuration, int currentDuration)
    {
        int newDuration = requestedDuration >= 0 ? requestedDuration : defaultDuration;
        if (currentDuration < 0 || newDuration < 0)
            return -1;

        return Mathf.Max(currentDuration, newDuration);
    }

    public static List<PerkRuntimeInstance> GetEffectivePerks(Battler battler)
    {
        List<PerkRuntimeInstance> perks = new();
        HashSet<string> addedKeys = new();

        if (battler == null)
            return perks;

        List<PerkRuntimeInstance> battlerPerks = battler.GetEffectivePerks();
        for (int i = 0; i < battlerPerks.Count; i++)
            AddEffectivePerk(perks, addedKeys, battlerPerks[i]);

        return perks;
    }

    private static void AddEffectivePerk(List<PerkRuntimeInstance> perks, HashSet<string> addedKeys, PerkRuntimeInstance perk)
    {
        if (perk?.Definition == null)
            return;

        string key = GetEffectivePerkKey(perk);
        if (addedKeys.Add(key))
            perks.Add(perk);
    }

    private static string GetEffectivePerkKey(PerkRuntimeInstance perk)
    {
        string perkId = perk.Definition?.Id ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(perk.SourceTrickInstanceId))
            return $"trick:{perk.SourceTrickInstanceId}:{perkId}";

        return $"direct:{perkId}";
    }
}
