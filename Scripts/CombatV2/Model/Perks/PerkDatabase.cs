using System;
using System.Collections.Generic;
using UnityEngine;

public class PerkDatabase : MonoBehaviour
{
    public const string PerkResourceFolder = "Data/Perks";
    public const string PerkTableResourcePath = "Data/PerkTable";

    private static PerkDatabase runtimeInstance;

    public List<PerkSO> allPerks = new();

    private void Awake()
    {
        LoadAll();
    }

    public static PerkDatabase GetOrCreateRuntimeDatabase()
    {
        PerkDatabase existing = FindObjectOfType<PerkDatabase>();
        if (existing != null)
        {
            existing.EnsureLoaded();
            return existing;
        }

        if (runtimeInstance != null)
        {
            runtimeInstance.EnsureLoaded();
            return runtimeInstance;
        }

        GameObject databaseObject = new("PerkDatabase(Runtime)")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeInstance = databaseObject.AddComponent<PerkDatabase>();
        runtimeInstance.LoadAll();
        return runtimeInstance;
    }

    public void EnsureLoaded()
    {
        if (allPerks == null || allPerks.Count == 0)
            LoadAll();
    }

    public void LoadAll()
    {
        PerkSO[] loaded = Resources.LoadAll<PerkSO>(PerkResourceFolder);
        allPerks ??= new List<PerkSO>();

        allPerks.Clear();
        allPerks.AddRange(loaded);
    }

    public PerkSO GetById(string perkId)
    {
        if (string.IsNullOrWhiteSpace(perkId))
            return null;

        EnsureLoaded();
        return allPerks.Find(perk => perk != null && !string.IsNullOrWhiteSpace(perk.Id) && perk.Id.Equals(perkId, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetById(string perkId, out PerkSO perk)
    {
        perk = GetById(perkId);
        return perk != null;
    }

    public List<PerkSO> GetIdentityPerks()
    {
        EnsureLoaded();
        return allPerks.FindAll(perk => perk != null && perk.IsPermanentIdentity);
    }

    public List<PerkSO> FilterByTag(string tag)
    {
        EnsureLoaded();
        List<PerkSO> matches = new();
        if (string.IsNullOrWhiteSpace(tag))
            return matches;

        for (int i = 0; i < allPerks.Count; i++)
        {
            PerkSO perk = allPerks[i];
            if (perk == null || string.IsNullOrWhiteSpace(perk.Tags))
                continue;

            string[] tags = perk.Tags.Split(';');
            for (int j = 0; j < tags.Length; j++)
            {
                if (!tags[j].Trim().Equals(tag, StringComparison.OrdinalIgnoreCase))
                    continue;

                matches.Add(perk);
                break;
            }
        }

        return matches;
    }
}
