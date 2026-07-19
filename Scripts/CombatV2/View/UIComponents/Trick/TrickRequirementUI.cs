using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickRequirementUI : MonoBehaviour
{
    private const string RequimentsIconResourcePath = "UI/Requirements/{0}.png";

    [Header("Requirement Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Tooltipable tooltipable;

    private static readonly Dictionary<string, Sprite> _iconCache = new();

    public void Setup(string statKey, int value)
    {
        if (iconImage != null) iconImage.sprite = GetRequirementIcon(statKey);
        if (countText != null) countText.text = $"{value}";
        if (tooltipable != null) tooltipable.SetTooltipText(statKey);
    }

    private static Sprite GetRequirementIcon(string statKey)
    {
        if (_iconCache.TryGetValue(statKey, out Sprite cachedSprite))
            return cachedSprite;

        string path = string.Format(RequimentsIconResourcePath, statKey);
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[RequirementUI] Icon not found at {path}");

        _iconCache[statKey] = sprite;
        return sprite;
    }
}