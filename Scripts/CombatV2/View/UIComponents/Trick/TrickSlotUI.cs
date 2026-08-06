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
    // highlightState removido
    [SerializeField] private GameObject cooldownState;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;

    private TrickInfoPanelUI trickInfoPanelUI;
    private TrickSO boundTrick;
    private TrickRuntimeInstance boundRuntimeInstance;
    private TrickInventoryItemLocation location;
    private bool isPanelOpen;

    // Variáveis pré-configuradas no Bind para uso do Tooltip
    private bool shouldShowTooltip;
    private string tooltipMessage;
    private TooltipUI.TooltipColor tooltipColor;

    public bool HasTrick => boundTrick != null;
    public TrickInventoryItemLocation Location => location;

    public event Action<TrickSlotUI> TrickSelected;
    public event Action<TrickSO, TrickInventoryAction, TrickInventoryItemLocation> OnInteractWithTrick;
    public Transform TargetHighlightTransform => transform;
    
    [SerializeField] private Tooltipable tooltipable;

    private void Awake()
    {
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(HandleSelectClick);   
            tooltipable = interactButton.gameObject.GetOrAddComponent<Tooltipable>();
        }

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
        
        bool isEmpty = trick == null;
        if (iconImage != null)
        {
            iconImage.sprite = !isEmpty ? trick.Icon : null;
            iconImage.enabled = !isEmpty && trick.Icon != null;
        }

        if (nameText != null) 
            nameText.text = !isEmpty ? trick.DisplayName : "";

        UpdateInputKeyText(itemLocation, inputKeyOverride);
        
        bool hasRuntimeCooldown = runtimeInstance != null && runtimeInstance.IsCoolingDown;
        bool isCoolingDown = hasRuntimeCooldown || registeredCooldown > 0;
        bool isActive = runtimeInstance != null && runtimeInstance.RemainingTurns > 0 && !runtimeInstance.WasExpired;
        
        int currentCooldownTurns = hasRuntimeCooldown ? runtimeInstance.CooldownTurnsRemaining : registeredCooldown;
        bool showCooldownVisual = false;
        
        shouldShowTooltip = true;
        tooltipMessage = isEmpty ? "Empty Slot" : "";
        tooltipColor = TooltipUI.TooltipColor.Default;

        switch (location.Location)
        {
            case TrickInventoryLocation.LearnedTricks:
                showCooldownVisual = isCoolingDown;

                if (isLocked && isActive) 
                {
                    tooltipMessage = "Blocked";
                    tooltipColor = TooltipUI.TooltipColor.Yellow;
                }
                else if (isCoolingDown)
                {
                    tooltipMessage = $"Cooling down: <color=orange>{currentCooldownTurns} turns</color> remaining";
                    tooltipColor = TooltipUI.TooltipColor.Red;
                }
                else if (!isEmpty)
                {
                    tooltipMessage = "Ready to cast";
                    tooltipColor = TooltipUI.TooltipColor.Blue;
                }
                break;

            case TrickInventoryLocation.CastedActiveSlot:
            case TrickInventoryLocation.CastedPassiveSlot:
                showCooldownVisual = isCoolingDown && !isActive;

                if (isActive)
                {
                    tooltipMessage = "Ready to trigger";
                    tooltipColor = TooltipUI.TooltipColor.Blue; 
                }
                else if (isCoolingDown)
                {
                    tooltipMessage = $"Cooling down: <color=orange>{currentCooldownTurns} turns</color> remaining";
                    tooltipColor = TooltipUI.TooltipColor.Red;
                }
                else if (!isEmpty)
                {
                    // Caso tenha uma Trick equipada, mas não está ativa nem em cooldown
                    shouldShowTooltip = false; 
                }
                break;

            default: // IdentitySlot
                if (!isEmpty)
                {
                    tooltipMessage = "Ready to trigger";
                    tooltipColor = TooltipUI.TooltipColor.Blue;
                }
                else
                {
                    tooltipMessage = "Empty Slot";
                }
                break;
        }
        
        if (cooldownText != null) 
            cooldownText.text = showCooldownVisual && currentCooldownTurns > 0 ? currentCooldownTurns.ToString("F0") : "";

        if (emptyState != null) emptyState.SetActive(isEmpty && !isLocked);
        if (lockedState != null) lockedState.SetActive(isLocked);
        if (cooldownState != null) cooldownState.SetActive(showCooldownVisual);
        if (interactButton != null) 
            interactButton.interactable = !isEmpty && !isLocked && !isCoolingDown;

        if (tooltipable != null)
        {
            tooltipable.SetTooltipColor(tooltipColor, gameObject);
            tooltipable.SetTooltipText(tooltipMessage);
            tooltipable.DisableTooltip(!shouldShowTooltip);
        }
    }

    public void ShowInteractionPanel(bool visible)
    {
        if (trickInfoPanelUI == null)
            return;

        if (visible && !isPanelOpen)
        {
            if (boundTrick == null) return;

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
        if (inputKeyText == null) return;

        string inputKey = itemLocation.Location == TrickInventoryLocation.CastedActiveSlot ? inputKeyOverride : "";
        inputKeyText.text = inputKey;
        
        bool showKey = !string.IsNullOrEmpty(inputKey);
        inputKeyText.gameObject.SetActive(showKey);
        
        if (inputKeyField != null)
            inputKeyField.SetActive(showKey);
    }

    private void HandleSelectClick()
    {
        if (boundTrick == null) return;
        TrickSelected?.Invoke(this);
    }

    private void OnRaiseInteraction(TrickInventoryAction action)
    {
        if (boundTrick == null) return;

        if (action != TrickInventoryAction.Close)
            OnInteractWithTrick?.Invoke(boundTrick, action, location);

        ShowInteractionPanel(false);
    }
}