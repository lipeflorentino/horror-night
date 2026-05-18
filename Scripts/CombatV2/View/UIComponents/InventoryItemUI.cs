using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image iconImage;

    public void Bind(ItemSO item, int count)
    {
        if (item == null)
            return;

        if (nameText != null)
            nameText.text = item.itemName;

        if (countText != null)
            countText.text = count.ToString();

        if (iconImage != null)
            iconImage.sprite = item.icon;
    }
}
