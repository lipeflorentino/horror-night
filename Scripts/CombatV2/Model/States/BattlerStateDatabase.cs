using System;
using System.Collections.Generic;
using UnityEngine;

public class BattlerStateDatabase : MonoBehaviour
{
    public const string BattlerStateResourceFolder = "Data/BattlerStates";

    private static BattlerStateDatabase runtimeInstance;

    public List<BattlerStateSO> allStates = new();

    private void Awake()
    {
        LoadAll();
    }

    public static BattlerStateDatabase GetOrCreateRuntimeDatabase()
    {
        BattlerStateDatabase existing = FindObjectOfType<BattlerStateDatabase>();
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

        GameObject databaseObject = new("BattlerStateDatabase(Runtime)")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeInstance = databaseObject.AddComponent<BattlerStateDatabase>();
        runtimeInstance.LoadAll();
        return runtimeInstance;
    }

    public void EnsureLoaded()
    {
        if (allStates == null || allStates.Count == 0)
            LoadAll();
    }

    public void LoadAll()
    {
        BattlerStateSO[] loaded = Resources.LoadAll<BattlerStateSO>(BattlerStateResourceFolder);
        allStates ??= new List<BattlerStateSO>();
        allStates.Clear();
        allStates.AddRange(loaded);
    }

    public BattlerStateSO GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureLoaded();
        return allStates.Find(state => state != null && !string.IsNullOrWhiteSpace(state.Id) && state.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetById(string id, out BattlerStateSO state)
    {
        state = GetById(id);
        return state != null;
    }
}
