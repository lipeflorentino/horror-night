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
    [SerializeField] private GameObject inputKeyField;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject lockedState;
    [SerializeField] private GameObject highlightState;
    [SerializeField] private GameObject cooldownState;
    [SerializeField] private TMP_Text cooldownText;

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
        
        if (lockedState != null) lockedState.SetActive(false);
        if (cooldownState != null) cooldownState.SetActive(false);
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

    public void Bind(TrickSO trick, TrickInventoryItemLocation itemLocation, TrickRuntimeInstance runtimeInstance = null, bool isLocked = false, string inputKeyOverride = "", int registeredCooldown = 0)
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
        UpdateInputKeyText(itemLocation, inputKeyOverride);
        
        // 1. Lemos os estados: O cooldown pode vir do RuntimeInstance OU do registro do Inventário
        bool hasRuntimeCooldown = runtimeInstance != null && runtimeInstance.IsCoolingDown;
        bool isCoolingDown = hasRuntimeCooldown || registeredCooldown > 0;
        
        bool isActive = runtimeInstance != null && runtimeInstance.RemainingTurns > 0 && !runtimeInstance.WasExpired;
        bool showCooldownVisual;

        if (location.Location == TrickInventoryLocation.LearnedTricks)
        {
            showCooldownVisual = isCoolingDown;
            
            if (cooldownText != null) 
            {
                int displayCooldown = hasRuntimeCooldown ? runtimeInstance.CooldownTurnsRemaining : registeredCooldown;
                cooldownText.text = displayCooldown > 0 ? displayCooldown.ToString("F0") : "";
            }
        }
        else
        {
            showCooldownVisual = isCoolingDown && !isActive;
            
            if (cooldownText != null) 
            {
                int displayCooldown = hasRuntimeCooldown ? runtimeInstance.CooldownTurnsRemaining : registeredCooldown;
                cooldownText.text = showCooldownVisual && displayCooldown > 0 ? displayCooldown.ToString("F0") : "";
            }
        }
        
        if (emptyState != null) emptyState.SetActive(trick == null && !isLocked);
        if (lockedState != null) lockedState.SetActive(isLocked);
        if (cooldownState != null) cooldownState.SetActive(showCooldownVisual);

        // Bloqueia se estiver castado (isLocked) OU em cooldown (isCoolingDown)
        if (interactButton != null) interactButton.interactable = trick != null && !isLocked && !isCoolingDown;
        
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
            trickInfoPanelUI.HidePanel();
            trickInfoPanelUI.OnRaiseInteraction -= OnRaiseInteraction;
            isPanelOpen = false;
        }
    }

    private void UpdateInputKeyText(TrickInventoryItemLocation itemLocation, string inputKeyOverride)
    {
        if (inputKeyText == null)
            return;

        string inputKey = itemLocation.Location == TrickInventoryLocation.CastedActiveSlot 
            ? inputKeyOverride
            : "";

        inputKeyText.text = inputKey;
        
        bool showKey = !string.IsNullOrEmpty(inputKey);
        inputKeyText.gameObject.SetActive(showKey);
        
        if (inputKeyField != null)
            inputKeyField.SetActive(showKey);
    }

    public void SetSelected(bool selected)
    {
        if (highlightState != null)
            highlightState.SetActive(selected && boundTrick != null);
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
