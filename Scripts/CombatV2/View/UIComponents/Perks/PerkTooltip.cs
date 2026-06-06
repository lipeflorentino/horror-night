using UnityEngine;
using UnityEngine.UI;

public class PerkTooltip : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text nameText;
    [SerializeField] private TMPro.TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMPro.TMP_Text tags;

    public void Show(PerkSO perkDefinition)
    {
        if (nameText != null)
            nameText.text = perkDefinition.DisplayName;

        if (descriptionText != null)
            descriptionText.text = perkDefinition.Description;

        if (iconImage != null)
            iconImage.sprite = perkDefinition.Icon;
        
        if (tags != null)
            tags.text = string.Join(", ", perkDefinition.Tags);
    }
}