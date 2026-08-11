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
        EnsureTooltipable();
    }

    private void EnsureTooltipable()
    {
        if (tooltipable == null)
        {
            tooltipable = gameObject.GetOrAddComponent<Tooltipable>();
        }
    }

    public void Setup(string rarityKey)
    {
        EnsureTooltipable(); // Garante que a referência existe antes de usá-la
        
        if (iconImage != null) iconImage.sprite = IconProvider.GetTrickRarityIcon(rarityKey);
        if (tooltipable != null) tooltipable.SetTooltipText(rarityKey);
    }
}