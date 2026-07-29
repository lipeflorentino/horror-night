using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationCostItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;

    public void Bind(DiceStatType statType, int amount)
    {
        Bind(statType.ToString(), amount);
    }

    public void Bind(string iconKey, int amount)
    {
        iconImage.sprite = IconProvider.GetStatIcon(iconKey);
        amountText.text = $"-{amount}";
    }
}