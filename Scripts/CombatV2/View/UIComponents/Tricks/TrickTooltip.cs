using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exibe tooltip com informações detalhadas do Trick ao passar o mouse
/// </summary>
public class TrickTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI flavorText;
    [SerializeField] private Image iconImage;

    /// <summary>
    /// Exibe os dados do trick no tooltip
    /// </summary>
    public void Show(TrickSO trickDefinition)
    {
        if (trickDefinition == null)
            return;
        
        if (nameText != null)
            nameText.text = trickDefinition.DisplayName;

        if (descriptionText != null)
            descriptionText.text = trickDefinition.Description;
        
        if (costText != null)
            costText.text = $"Custo: Mind {trickDefinition.MindCost} | Body {trickDefinition.BodyCost} | Heart {trickDefinition.HeartCost}";
        
        if (levelText != null)
            levelText.text = $"Level: {trickDefinition.Level}";
        
        if (rarityText != null)
            rarityText.text = $"Raridade: {trickDefinition.Rarity}";
        
        if (flavorText != null)
            flavorText.text = trickDefinition.FlavorText;

        if (iconImage != null && trickDefinition.Icon != null)
            iconImage.sprite = trickDefinition.Icon;
    }
}
