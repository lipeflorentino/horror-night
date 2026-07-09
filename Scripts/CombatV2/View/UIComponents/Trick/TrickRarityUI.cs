using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TrickRarityUI : MonoBehaviour
{
    private const string RarityIconResourcePath = "Assets/Art/Sprites/Tricks/Icons/{0}.png";

    [Header("Rarity Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Tooltipable tooltipable;

    private static readonly Dictionary<string, Sprite> _iconCache = new();

    public void Setup(string rarity)
    {
        if (iconImage != null) iconImage.sprite = GetRarityIcon(rarity);
        if (tooltipable != null) tooltipable.SetTooltipText(rarity);
    }

    private static Sprite GetRarityIcon(string rarity)
    {
        if (_iconCache.TryGetValue(rarity, out Sprite cachedSprite))
            return cachedSprite;

        string path = string.Format(RarityIconResourcePath, rarity);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[TrickRarityUI] Icon not found at {path}");

        _iconCache[rarity] = sprite;
        return sprite;
    }
}