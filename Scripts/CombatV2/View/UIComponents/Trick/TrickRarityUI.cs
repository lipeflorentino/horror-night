using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrickRarityUI : MonoBehaviour
{
    [Header("Rarity Info")]
    [SerializeField] private Image iconImage;
    private Tooltipable tooltipable;

    void Awake()
    {
        tooltipable = gameObject.GetOrAddComponent<Tooltipable>();
    }

    public void Setup(string rarityKey)
    {
        if (iconImage != null) iconImage.sprite = IconProvider.GetTrickRarityIcon(rarityKey);
        if (tooltipable != null) tooltipable.SetTooltipText(rarityKey);
    }
}