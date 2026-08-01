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

    public void Bind(TrickSO trick, TrickInventoryItemLocation itemLocation, TrickRuntimeInstance runtimeInstance = null, bool isLocked = false, string inputKeyOverride = "")
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
        
        // 1. Lemos os estados matemáticos puros do Runtime Instance
        bool isCoolingDown = runtimeInstance != null && runtimeInstance.IsCoolingDown;
        bool isActive = runtimeInstance != null && runtimeInstance.RemainingTurns > 0 && !runtimeInstance.WasExpired;
        bool showCooldownVisual;

        if (location.Location == TrickInventoryLocation.LearnedTricks)
        {
            // No painel de inventário, mostramos o visual de cooldown imediatamente, 
            // pois o jogador precisa saber que a habilidade não está pronta.
            showCooldownVisual = isCoolingDown;
        }
        else
        {
            // Nas barras de combate (CastedActive / CastedPassive), o slot continua 
            // "aceso" enquanto o efeito estiver durando (!isActive = false).
            // O visual de cooldown só aparece depois que o RemainingTurns zera.
            showCooldownVisual = isCoolingDown && !isActive;
            if (cooldownText != null) cooldownText.text = boundRuntimeInstance?.CooldownTurnsRemaining.ToString("F0") ?? "";
        }
        
        if (emptyState != null) emptyState.SetActive(trick == null && !isLocked);
        if (lockedState != null) lockedState.SetActive(isLocked);
        if (cooldownState != null) cooldownState.SetActive(showCooldownVisual);

        // Mantém o botão bloqueado caso esteja em cooldown (independentemente de onde esteja)
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
