using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrickRarityUI : MonoBehaviour
{
    [Header("Rarity Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Tooltipable tooltipable;

    public void Setup(string rarityKey)
    {
        if (iconImage != null) iconImage.sprite = IconProvider.GetTrickRarityIcon(rarityKey);
        if (tooltipable != null) tooltipable.SetTooltipText(rarityKey);
    }
}