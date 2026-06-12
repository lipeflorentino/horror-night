using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickSlotUI : MonoBehaviour
{
    [Header("Trick Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject lockedState;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;

    private TrickInfoPanelUI trickInfoPanelUI;
    private TrickSO boundTrick;
    private TrickRuntimeInstance boundRuntimeInstance;
    private TrickInventoryItemLocation location;
    private bool isPanelOpen;

    public event Action<TrickSlotUI> TrickSelected;
    public event Action<TrickSO, TrickInventoryAction, TrickInventoryItemLocation> OnInteractWithTrick;

    private void Awake()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(HandleSelectClick);

        ShowInteractionPanel(false);
    }

    private void OnDestroy()
    {
        if (interactButton != null)
            interactButton.onClick.RemoveListener(HandleSelectClick);

        if (trickInfoPanelUI != null && isPanelOpen)
            trickInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
    }

    public void SetTrickInfoPanel(TrickInfoPanelUI panel)
    {
        trickInfoPanelUI = panel;
    }

    public void Bind(TrickSO trick, TrickInventoryItemLocation itemLocation, TrickRuntimeInstance runtimeInstance = null, bool isLocked = false)
    {
        boundTrick = trick;
        boundRuntimeInstance = runtimeInstance;
        location = itemLocation;

        if (iconImage != null)
        {
            iconImage.sprite = trick != null ? trick.Icon : null;
            iconImage.enabled = trick != null && trick.Icon != null;
        }

        if (nameText != null) nameText.text = trick != null ? trick.DisplayName : "Empty";
        if (levelText != null) levelText.text = trick != null ? $"Lvl {trick.Level}" : string.Empty;
        if (costText != null) costText.text = trick != null ? FormatCost(trick) : string.Empty;
        if (slotText != null) slotText.text = FormatSlot(itemLocation);
        if (cooldownText != null) cooldownText.text = runtimeInstance != null && runtimeInstance.IsCoolingDown ? $"CD {runtimeInstance.CooldownTurnsRemaining}" : string.Empty;
        if (emptyState != null) emptyState.SetActive(trick == null && !isLocked);
        if (lockedState != null) lockedState.SetActive(isLocked);
        if (interactButton != null) interactButton.interactable = trick != null && !isLocked;
    }

    public void ShowInteractionPanel(bool visible)
    {
        if (trickInfoPanelUI == null)
            return;

        if (visible && !isPanelOpen)
        {
            if (boundTrick == null)
                return;

            trickInfoPanelUI.SetTrickInfo(boundTrick, boundRuntimeInstance, location);
            trickInfoPanelUI.OnRaiseInteraction += OnRaiseInteraction;
            trickInfoPanelUI.ShowTooltip(transform.position);
            isPanelOpen = true;
        }
        else if (!visible && isPanelOpen)
        {
            trickInfoPanelUI.HideTooltip();
            trickInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
            isPanelOpen = false;
        }
    }

    private void HandleSelectClick()
    {
        if (boundTrick == null)
            return;

        TrickSelected?.Invoke(this);
    }

    private void OnRaiseInteraction(TrickInventoryAction action)
    {
        if (boundTrick == null)
            return;

        if (action != TrickInventoryAction.Close)
            OnInteractWithTrick?.Invoke(boundTrick, action, location);

        ShowInteractionPanel(false);
    }

    private static string FormatCost(TrickSO trick)
    {
        string cost = string.Empty;
        if (trick.MindCost > 0) cost += $"{trick.MindCost}M ";
        if (trick.BodyCost > 0) cost += $"{trick.BodyCost}B ";
        if (trick.HeartCost > 0) cost += $"{trick.HeartCost}H";
        return string.IsNullOrWhiteSpace(cost) ? "Free" : cost.Trim();
    }

    private static string FormatSlot(TrickInventoryItemLocation itemLocation)
    {
        return itemLocation.Location switch
        {
            TrickInventoryLocation.IdentitySlot => $"ID {itemLocation.SlotIndex + 1}",
            TrickInventoryLocation.CastedSlot => $"Cast {itemLocation.SlotIndex + 1}",
            _ => "Learned"
        };
    }
}
