using System;
using System.Collections.Generic;
using UnityEngine;

public class DrawbackDatabase : MonoBehaviour
{
    public const string DrawbackResourceFolder = "Data/Drawbacks";

    private static DrawbackDatabase runtimeInstance;

    public List<DrawbackSO> allDrawbacks = new();

    private void Awake()
    {
        LoadAll();
    }

    public static DrawbackDatabase GetOrCreateRuntimeDatabase()
    {
        DrawbackDatabase existing = FindObjectOfType<DrawbackDatabase>();
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

        GameObject databaseObject = new("DrawbackDatabase(Runtime)")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeInstance = databaseObject.AddComponent<DrawbackDatabase>();
        runtimeInstance.LoadAll();
        return runtimeInstance;
    }

    public void EnsureLoaded()
    {
        if (allDrawbacks == null || allDrawbacks.Count == 0)
            LoadAll();
    }

    public void LoadAll()
    {
        DrawbackSO[] loaded = Resources.LoadAll<DrawbackSO>(DrawbackResourceFolder);
        allDrawbacks ??= new List<DrawbackSO>();

        allDrawbacks.Clear();
        allDrawbacks.AddRange(loaded);
    }

    public DrawbackSO GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureLoaded();
        return allDrawbacks.Find(drawback => drawback != null && !string.IsNullOrWhiteSpace(drawback.Id) && drawback.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetById(string id, out DrawbackSO drawback)
    {
        drawback = GetById(id);
        return drawback != null;
    }
}
