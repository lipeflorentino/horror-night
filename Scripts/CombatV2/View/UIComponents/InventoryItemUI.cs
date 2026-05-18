using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image iconImage;

    [SerializeField] private Button interactButton;

    private ItemSO boundItem;

    public event Action<ItemSO> InteractRequested;

    private void Awake()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(HandleInteractClick);
    }

    private void OnDestroy()
    {
        if (interactButton != null)
            interactButton.onClick.RemoveListener(HandleInteractClick);
    }

    public void Bind(ItemSO item, int count)
    {
        if (item == null)
            return;

        boundItem = item;

        if (nameText != null)
            nameText.text = item.itemName;

        if (countText != null)
            countText.text = count.ToString();

        if (iconImage != null)
            iconImage.sprite = item.icon;
    }

    private void HandleInteractClick()
    {
        if (boundItem == null)
            return;

        InteractRequested?.Invoke(boundItem);
    }
}
