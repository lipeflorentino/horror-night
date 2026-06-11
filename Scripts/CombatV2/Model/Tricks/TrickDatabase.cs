using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database centralizado de Tricks (como PerkDatabase para Perks).
/// Carrega todos os TrickSO do Resources e fornece métodos de busca.
/// </summary>
public class TrickDatabase : MonoBehaviour
{
    public const string TrickResourceFolder = "Data/Tricks";
    
    private static TrickDatabase runtimeInstance;
    
    public List<TrickSO> allTricks = new();
    
    private void Awake()
    {
        LoadAll();
    }
    
    /// <summary>
    /// Retorna ou cria instância runtime singleton
    /// </summary>
    public static TrickDatabase GetOrCreateRuntimeDatabase()
    {
        TrickDatabase existing = FindObjectOfType<TrickDatabase>();
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
        
        GameObject databaseObject = new("TrickDatabase(Runtime)")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeInstance = databaseObject.AddComponent<TrickDatabase>();
        runtimeInstance.LoadAll();
        return runtimeInstance;
    }
    
    /// <summary>
    /// Carrega todos os TrickSO se não estiverem carregados
    /// </summary>
    public void EnsureLoaded()
    {
        if (allTricks == null || allTricks.Count == 0)
            LoadAll();
    }
    
    /// <summary>
    /// Carrega todos os TrickSO da pasta Resources
    /// </summary>
    public void LoadAll()
    {
        TrickSO[] loaded = Resources.LoadAll<TrickSO>(TrickResourceFolder);
        allTricks ??= new List<TrickSO>();
        
        allTricks.Clear();
        allTricks.AddRange(loaded);
        
        Debug.Log($"[TrickDatabase] Carregados {allTricks.Count} tricks");
    }
    
    /// <summary>
    /// Obtém um Trick pelo ID
    /// </summary>
    public TrickSO GetById(string trickId)
    {
        if (string.IsNullOrWhiteSpace(trickId))
            return null;
        
        EnsureLoaded();
        return allTricks.Find(trick => trick != null && 
                                      !string.IsNullOrWhiteSpace(trick.Id) && 
                                      trick.Id.Equals(trickId, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Tenta obter um Trick pelo ID
    /// </summary>
    public bool TryGetById(string trickId, out TrickSO trick)
    {
        trick = GetById(trickId);
        return trick != null;
    }
    
    /// <summary>
    /// Filtra tricks por tag
    /// </summary>
    public List<TrickSO> FilterByTag(string tag)
    {
        EnsureLoaded();
        List<TrickSO> matches = new();
        
        if (string.IsNullOrWhiteSpace(tag))
            return matches;
        
        for (int i = 0; i < allTricks.Count; i++)
        {
            TrickSO trick = allTricks[i];
            if (trick == null || trick.Tags == null)
                continue;
            
            for (int j = 0; j < trick.Tags.Count; j++)
            {
                if (trick.Tags[j].Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(trick);
                    break;
                }
            }
        }
        
        return matches;
    }
    
    /// <summary>
    /// Filtra tricks por nível mínimo
    /// </summary>
    public List<TrickSO> FilterByLevel(int minLevel)
    {
        EnsureLoaded();
        List<TrickSO> matches = new();
        
        for (int i = 0; i < allTricks.Count; i++)
        {
            if (allTricks[i] != null && allTricks[i].Level <= minLevel)
                matches.Add(allTricks[i]);
        }
        
        return matches;
    }
    
    /// <summary>
    /// Filtra tricks por rarity
    /// </summary>
    public List<TrickSO> FilterByRarity(TrickRarity rarity)
    {
        EnsureLoaded();
        List<TrickSO> matches = new();
        
        for (int i = 0; i < allTricks.Count; i++)
        {
            if (allTricks[i] != null && allTricks[i].Rarity == rarity)
                matches.Add(allTricks[i]);
        }
        
        return matches;
    }
}
