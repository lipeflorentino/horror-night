using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text facesText;
    [SerializeField] private Sprite mindDiceIcon;
    [SerializeField] private Sprite heartDiceIcon;
    [SerializeField] private Sprite bodyDiceIcon;

    public void Bind(DiceStatType statType, int faces)
    {
        if (iconImage != null)
        {
            iconImage.sprite = GetIcon(statType);
            iconImage.enabled = iconImage.sprite != null;
        }

        if (facesText != null)
            facesText.text = $"d{Mathf.Max(1, faces)}";
    }

    private Sprite GetIcon(DiceStatType type)
    {
        return type switch
        {
            DiceStatType.Mind => mindDiceIcon,
            DiceStatType.Heart => heartDiceIcon,
            DiceStatType.Body => bodyDiceIcon,
            _ => null
        };
    }
}
