using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickSlotUI : MonoBehaviour
{
    [Header("Trick Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text inputKeyText;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject lockedState;
    [SerializeField] private GameObject highlightState;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;

    private TrickInfoPanelUI trickInfoPanelUI;
    private TrickSO boundTrick;
    private TrickRuntimeInstance boundRuntimeInstance;
    private TrickInventoryItemLocation location;
    private bool isPanelOpen;
    public bool HasTrick => boundTrick != null;
    public TrickInventoryItemLocation Location => location;

    public event Action<TrickSlotUI> TrickSelected;
    public event Action<TrickSO, TrickInventoryAction, TrickInventoryItemLocation> OnInteractWithTrick;

    private void Awake()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(HandleSelectClick);

        SetSelected(false);
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

        if (nameText != null) nameText.text = trick != null ? trick.DisplayName : "";
        UpdateInputKeyText(itemLocation);
        if (emptyState != null) emptyState.SetActive(trick == null && !isLocked);
        if (lockedState != null) lockedState.SetActive(isLocked);
        if (interactButton != null) interactButton.interactable = trick != null && !isLocked;
        SetSelected(false);
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
            trickInfoPanelUI.ShowPanel();
            isPanelOpen = true;
        }
        else if (!visible && isPanelOpen)
        {
            trickInfoPanelUI.HideTooltip();
            trickInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
            isPanelOpen = false;
        }
    }

    private void UpdateInputKeyText(TrickInventoryItemLocation itemLocation)
    {
        if (inputKeyText == null)
            return;

        string inputKey = itemLocation.Location == TrickInventoryLocation.CastedSlot
            ? GetCastedSlotInputKey(itemLocation.SlotIndex)
            : "";

        inputKeyText.text = inputKey;
        inputKeyText.gameObject.SetActive(!string.IsNullOrEmpty(inputKey));
    }

    public void SetSelected(bool selected)
    {
        if (highlightState != null)
            highlightState.SetActive(selected && boundTrick != null);
    }

    private string GetCastedSlotInputKey(int slotIndex)
    {
        return slotIndex switch
        {
            0 => "Q",
            1 => "W",
            2 => "E",
            3 => "R",
            4 => "R",
            _ => ""
        };
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
}
