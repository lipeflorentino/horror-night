using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryItemAction
{
    Use,
    Equip,
    Unequip,
    Discard
}

public enum InventoryItemLocation
{
    ItemSlots,
    WeaponSlot,
    RelicSlot
}

public class InventoryItemView : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private Image iconImage;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;

    private ItemInfoPanelUI itemInfoPanelUI;
    private ItemSO boundItem;
    private int boundCount;
    private InventoryItemLocation location;

    public event Action<InventoryItemView> ItemSelected;
    public event Action<ItemSO, InventoryItemAction, InventoryItemLocation> OnInteractWithItem;

    private void Awake()
    {
        itemInfoPanelUI = FindObjectOfType<ItemInfoPanelUI>();
        if (interactButton != null) interactButton.onClick.AddListener(HandleSelectClick);
        ShowInteractionPanel(false);
    }

    private void OnDestroy()
    {
        if (interactButton != null) interactButton.onClick.RemoveListener(HandleSelectClick);
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
        if (visible)
        {
            itemInfoPanelUI.SetItemInfo(boundItem, boundCount, boundItem.statBonus, boundItem.specialEffect, boundItem.description, boundItem.type.ToString(), location);
            itemInfoPanelUI.OnRaiseInteraction += OnRaiseInteraction;
            itemInfoPanelUI.ShowTooltip(transform.position);
        }
        else
        {
            itemInfoPanelUI.HideTooltip();
            itemInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
        }
    }

    private void HandleSelectClick()
    {
        Logger.Log($"[InventoryItemView] Item selecionado: {boundItem.itemName}");
        if (boundItem == null) return;
        ItemSelected?.Invoke(this);
    }

    private void OnRaiseInteraction(InventoryItemAction action)
    {
        if (boundItem == null) return;
        OnInteractWithItem?.Invoke(boundItem, action, location);
        Logger.Log($"[InventoryItemView] Interação: {action} para item {boundItem.itemName}");

        if (action == InventoryItemAction.Use || action == InventoryItemAction.Equip || action == InventoryItemAction.Unequip || action == InventoryItemAction.Discard)
            ShowInteractionPanel(false);
    }
}
