using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryItemAction
{
    Use,
    Equip,
    Unequip,
    Discard,
    Close
}

public enum InventoryItemLocation
{
    ItemSlots,
    WeaponSlot,
    RelicSlot
}

public class InventoryItemUI : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private Image iconImage;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;

    private ItemInfoPanelUI itemInfoPanelUI;
    private ItemSO boundItem;
    private int boundCount;
    private InventoryItemLocation location;
    private bool isPanelOpen = false;

    public event Action<InventoryItemUI> ItemSelected;
    public event Action<ItemSO, InventoryItemAction, InventoryItemLocation> OnInteractWithItem;

    private void Awake()
    {
        if (interactButton != null) interactButton.onClick.AddListener(HandleSelectClick);
        ShowInteractionPanel(false);
    }

    private void OnDestroy()
    {
        if (interactButton != null) interactButton.onClick.RemoveListener(HandleSelectClick);
        if (itemInfoPanelUI != null && isPanelOpen)
        {
            itemInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
        }
    }

    public void SetItemInfoPanel(ItemInfoPanelUI panel)
    {
        itemInfoPanelUI = panel;
    }

    public void Bind(ItemSO item, int count, InventoryItemLocation itemLocation)
    {
        if (item == null) return;

        boundItem = item;
        boundCount = count;
        location = itemLocation;
        if (iconImage != null) iconImage.sprite = item.icon;
    }

    public void ShowInteractionPanel(bool visible)
    {
        if (itemInfoPanelUI == null)
        {
            return;
        }

        if (visible && !isPanelOpen)
        {
            itemInfoPanelUI.SetItemInfo(boundItem, boundCount, boundItem.statBonus, boundItem.specialEffect, boundItem.description, boundItem.type.ToString(), location);
            itemInfoPanelUI.OnRaiseInteraction += OnRaiseInteraction;
            itemInfoPanelUI.ShowTooltip(transform.position);
            isPanelOpen = true;
        }
        else if (!visible && isPanelOpen)
        {
            itemInfoPanelUI.HideTooltip();
            itemInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
            isPanelOpen = false;
        }
    }

    private void HandleSelectClick()
    {
        if (boundItem == null) return;
        ItemSelected?.Invoke(this);
    }

    private void OnRaiseInteraction(InventoryItemAction action)
    {
        if (boundItem == null) return;
        if (action != InventoryItemAction.Close)
        {
            OnInteractWithItem?.Invoke(boundItem, action, location);
        }

        ShowInteractionPanel(false);
    }
}
