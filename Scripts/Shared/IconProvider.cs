using System.Collections.Generic;
using UnityEngine;

public static class IconProvider
{
    private const string StatsIconResourcePath = "UI/Stats/{0}Icon";
    private const string RarityIconResourcePath = "UI/RarityIcons/{0}";
    private const string RequirementsIconResourcePath = "UI/Requirements/{0}";
    private static readonly Dictionary<string, Sprite> _iconCache = new();
    
    public static Sprite GetIcon(string statKey, string resourcePath)
    {
        if (_iconCache.TryGetValue(statKey, out Sprite cachedSprite))
            return cachedSprite;

        string path = string.Format(resourcePath, statKey);
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[CombatInfoPanelView] Icon not found at Resources/{path}.");

        _iconCache[statKey] = sprite;
        return sprite;
    }

    public static Sprite GetStatIcon(DiceStatType statType) => GetIcon(statType.ToString(), StatsIconResourcePath);
    public static Sprite GetStatIcon(string statKey) => GetIcon(statKey, StatsIconResourcePath);
    public static Sprite GetRequirementIcon(string requirementKey) => GetIcon(requirementKey, RequirementsIconResourcePath);
    public static Sprite GetTrickRarityIcon(string trickKey) => GetIcon(trickKey, RarityIconResourcePath);
}