using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text nameText;

    public void Bind(Sprite itemIcon, string itemName, int count)
    {
        if (icon != null)
            icon.sprite = itemIcon;

        if (countText != null)
            countText.text = $"x{count}";

        if (nameText != null)
            nameText.text = itemName;
    }
}