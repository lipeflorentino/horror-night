using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Painel principal de ações do jogador em combate.
///
/// REFATORAÇÃO — DiceStatAllocatorUI:
///   Os 12 campos de botões avulsos (AddMind/Heart/Body Power/Accuracy e seus
///   correspondentes Remove) foram substituídos por 6 referências de
///   DiceStatAllocatorUI. Cada componente encapsula internamente os botões
///   "+ / -" e o contador de uma linha de dado.
///
///   Configure no Inspector:
///     diceAllocators[0] → Mind   + Power    (DiceStatAllocatorUI no Prefab)
///     diceAllocators[1] → Heart  + Power
///     diceAllocators[2] → Body   + Power
///     diceAllocators[3] → Mind   + Accuracy
///     diceAllocators[4] → Heart  + Accuracy
///     diceAllocators[5] → Body   + Accuracy
/// </summary>
public class ActionPanelView : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector — Botões de ação global
    // -------------------------------------------------------------------------

    public Button SelectAttackButton;
    public Button SelectDefendButton;
    public Button SelectItemsButton;
    public Button SelectTricksButton;
    public Button SelectInfoButton;
    public Button EndTurnButton;


    [Header("Painéis Auxiliares")]
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private TrickInventoryView trickInventoryView;

    // -------------------------------------------------------------------------
    // Eventos públicos — ações de combate
    // -------------------------------------------------------------------------

    public event Action SelectAttackClicked;
    public event Action SelectDefendClicked;
    public event Action SkipTurnClicked;
    public event Action SelectInfoButtonClicked;

    // -------------------------------------------------------------------------
    // Estado privado
    // -------------------------------------------------------------------------

    private CombatInputHandler boundInputHandler;

    // -------------------------------------------------------------------------
    // Ciclo de vida
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (inventoryView == null)
            inventoryView = FindObjectOfType<InventoryView>();

        if (trickInventoryView == null)
            trickInventoryView = FindObjectOfType<TrickInventoryView>();

        // Botões de ação global
        if (SelectAttackButton != null)
            SelectAttackButton.onClick.AddListener(HandleSelectAttackClick);

        if (SelectDefendButton != null)
            SelectDefendButton.onClick.AddListener(HandleSelectDefendClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.AddListener(HandleSkipTurnClick);

        if (SelectItemsButton != null)
            SelectItemsButton.onClick.AddListener(HandleItemsClick);

        if (SelectTricksButton != null)
            SelectTricksButton.onClick.AddListener(HandleTricksClick);

        if (SelectInfoButton != null)
            SelectInfoButton.onClick.AddListener(HandleInfoClick);
    }

    private void OnDestroy()
    {
        if (SelectAttackButton != null)
            SelectAttackButton.onClick.RemoveListener(HandleSelectAttackClick);

        if (SelectDefendButton != null)
            SelectDefendButton.onClick.RemoveListener(HandleSelectDefendClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.RemoveListener(HandleSkipTurnClick);

        if (SelectItemsButton != null)
            SelectItemsButton.onClick.RemoveListener(HandleItemsClick);

        if (SelectTricksButton != null)
            SelectTricksButton.onClick.RemoveListener(HandleTricksClick);

        if (SelectInfoButton != null)
            SelectInfoButton.onClick.RemoveListener(HandleInfoClick);
    }

    // -------------------------------------------------------------------------
    // Binding com o InputHandler
    // -------------------------------------------------------------------------

    public void BindInput(CombatInputHandler inputHandler)
    {
        if (boundInputHandler != null)
        {
            SelectAttackClicked     -= boundInputHandler.OnSelectAttack;
            SelectDefendClicked     -= boundInputHandler.OnSelectDefend;
            SkipTurnClicked         -= boundInputHandler.OnSkipTurn;
            SelectInfoButtonClicked -= boundInputHandler.OnSelectInfoPanel;
        }

        boundInputHandler = inputHandler;

        SelectAttackClicked     += inputHandler.OnSelectAttack;
        SelectDefendClicked     += inputHandler.OnSelectDefend;
        SkipTurnClicked         += inputHandler.OnSkipTurn;
        SelectInfoButtonClicked += inputHandler.OnSelectInfoPanel;
    }

    public void SetAllInteractable(bool isInteractable)
    {
        if (SelectAttackButton != null) SelectAttackButton.interactable = isInteractable;
        if (SelectDefendButton != null) SelectDefendButton.interactable = isInteractable;
        if (EndTurnButton != null)      EndTurnButton.interactable      = isInteractable;
        if (SelectInfoButton != null)   SelectInfoButton.interactable   = isInteractable;
    }

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        if (SelectAttackButton != null)
            SelectAttackButton.gameObject.SetActive(isPlayerAttacker);

        if (SelectDefendButton != null)
            SelectDefendButton.gameObject.SetActive(!isPlayerAttacker);

        SetSkipButtonVisible(isPlayerAttacker);
    }

    public void SetSkipButtonVisible(bool isVisible)
    {
        if (EndTurnButton != null)
            EndTurnButton.gameObject.SetActive(isVisible);
    }

    public void HighlightSelectedAction(ActionInstance action)
    {
        if (action == null)
        {
            Debug.LogWarning("HighlightSelectedAction called with null action.");
            return;
        }

        bool isAttack = action.Definition != null && action.Definition.Type == ActionType.Attack;

        if (SelectAttackButton != null)
            SelectAttackButton.image.color = isAttack
                ? new Color(0.85f, 0.35f, 0.35f, 1f)
                : Color.white;

        if (SelectDefendButton != null)
            SelectDefendButton.image.color = isAttack
                ? Color.white
                : new Color(0.35f, 0.65f, 0.95f, 1f);
    }

    // -------------------------------------------------------------------------
    // Handlers privados — botões de ação
    // -------------------------------------------------------------------------

    private void HandleSelectAttackClick() => SelectAttackClicked?.Invoke();
    private void HandleSelectDefendClick() => SelectDefendClicked?.Invoke();
    private void HandleSkipTurnClick()     => SkipTurnClicked?.Invoke();
    private void HandleInfoClick()         => SelectInfoButtonClicked?.Invoke();

    private void HandleItemsClick()
    {
        inventoryView.Open();
    }

    private void HandleTricksClick()
    {
        trickInventoryView.Open();
    }
}