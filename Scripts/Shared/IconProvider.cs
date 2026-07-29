using System.Collections.Generic;
using UnityEngine;

public static class IconProvider
{
    private const string StatsIconResourcePath = "UI/Stats/{0}Icon";
    private static readonly Dictionary<string, Sprite> _iconCache = new();
    public static Sprite GetStatIcon(DiceStatType statType) => GetStatIcon(statType.ToString());
    public static Sprite GetStatIcon(string statKey)
    {
        if (_iconCache.TryGetValue(statKey, out Sprite cachedSprite))
            return cachedSprite;

        string path = string.Format(StatsIconResourcePath, statKey);
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[CombatInfoPanelView] Icon not found at Resources/{path}.");

        _iconCache[statKey] = sprite;
        return sprite;
    }
}