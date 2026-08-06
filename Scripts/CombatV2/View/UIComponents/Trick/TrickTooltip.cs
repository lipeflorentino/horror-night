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
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image iconImage;

    /// <summary>
    /// Exibe os dados do trick no tooltip
    /// </summary>
    public void Show(TrickRuntimeInstance runtimeInstance)
    {
        if (runtimeInstance == null)
            return;
        
        if (nameText != null)
            nameText.text = runtimeInstance.Definition.DisplayName;

        if (descriptionText != null)
            descriptionText.text = runtimeInstance.Definition.Description;
        
        if (durationText != null)
            durationText.text = runtimeInstance.RemainingTurns > 0 ? runtimeInstance.RemainingTurns.ToString() : string.Empty;
            
        if (rarityText != null)
            rarityText.text = $"Raridade: {runtimeInstance.Definition.Rarity}";
            
        if (iconImage != null && runtimeInstance.Definition.Icon != null)
            iconImage.sprite = runtimeInstance.Definition.Icon;
    }
}
