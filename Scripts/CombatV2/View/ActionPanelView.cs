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
    public Button EndTurnButton;
    public Button SelectItemsButton;
    public Button SelectTricksButton;
    public Button SelectInfoButton;

    [Header("Painel de Confirmação")]
    public GameObject ConfirmPanel;
    public TMP_Text ConfirmActionText;
    public Button ConfirmButton;

    [Header("Alocadores de Dado — 6 linhas (Mind/Heart/Body × Power/Accuracy)")]
    [SerializeField] private DiceStatAllocatorUI[] diceAllocators;

    [Header("Painéis Auxiliares")]
    [SerializeField] private DiceAllocationView diceAllocationView;
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private TrickInventoryView trickInventoryView;

    // -------------------------------------------------------------------------
    // Eventos públicos — ações de combate
    // -------------------------------------------------------------------------

    public event Action SelectAttackClicked;
    public event Action SelectDefendClicked;
    public event Action SkipTurnClicked;
    public event Action ConfirmClicked;
    public event Action SelectInfoButtonClicked;

    // Eventos unificados de dado: substituem os 12 eventos individuais anteriores.
    // O DiceStatType e DiceRollType já identificam qual dado foi clicado.
    public event Action<DiceStatType, DiceRollType> AddDiceClicked;
    public event Action<DiceStatType, DiceRollType> RemoveDiceClicked;

    // -------------------------------------------------------------------------
    // Estado privado
    // -------------------------------------------------------------------------

    private CombatInputHandler boundInputHandler;

    // -------------------------------------------------------------------------
    // Ciclo de vida
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Fallbacks de FindObjectOfType para referências não atribuídas no Inspector
        if (inventoryView == null)
            inventoryView = FindObjectOfType<InventoryView>();

        if (trickInventoryView == null)
            trickInventoryView = FindObjectOfType<TrickInventoryView>();

        if (diceAllocationView == null)
            diceAllocationView = FindObjectOfType<DiceAllocationView>();

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

        if (ConfirmButton != null)
            ConfirmButton.onClick.AddListener(HandleConfirmClick);

        if (SelectInfoButton != null)
            SelectInfoButton.onClick.AddListener(HandleInfoClick);

        // Alocadores de dado — um listener por componente, em vez de 12 blocos individuais
        foreach (var allocator in diceAllocators)
        {
            if (allocator == null) continue;
            allocator.OnAddPressed    += HandleAllocatorAddPressed;
            allocator.OnRemovePressed += HandleAllocatorRemovePressed;
        }

        HideConfirmPanel();
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

        if (ConfirmButton != null)
            ConfirmButton.onClick.RemoveListener(HandleConfirmClick);

        if (SelectInfoButton != null)
            SelectInfoButton.onClick.RemoveListener(HandleInfoClick);

        foreach (var allocator in diceAllocators)
        {
            if (allocator == null) continue;
            allocator.OnAddPressed    -= HandleAllocatorAddPressed;
            allocator.OnRemovePressed -= HandleAllocatorRemovePressed;
        }

        if (boundInputHandler != null)
            boundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;
    }

    // -------------------------------------------------------------------------
    // Binding com o InputHandler
    // -------------------------------------------------------------------------

    public void BindInput(CombatInputHandler inputHandler)
    {
        // Remove bindings antigos
        if (boundInputHandler != null)
        {
            SelectAttackClicked  -= boundInputHandler.OnSelectAttack;
            SelectDefendClicked  -= boundInputHandler.OnSelectDefend;
            AddDiceClicked       -= boundInputHandler.OnAddDice;
            RemoveDiceClicked    -= boundInputHandler.OnRemoveDice;
            SkipTurnClicked      -= boundInputHandler.OnSkipTurn;
            ConfirmClicked       -= boundInputHandler.OnConfirmAction;
            SelectInfoButtonClicked -= boundInputHandler.OnToggleInfoPanel;
            boundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;
        }

        boundInputHandler = inputHandler;

        // Adiciona bindings novos — apenas 7 linhas no lugar das ~20 anteriores
        SelectAttackClicked += inputHandler.OnSelectAttack;
        SelectDefendClicked += inputHandler.OnSelectDefend;
        AddDiceClicked      += inputHandler.OnAddDice;
        RemoveDiceClicked   += inputHandler.OnRemoveDice;
        SkipTurnClicked     += inputHandler.OnSkipTurn;
        ConfirmClicked      += inputHandler.OnConfirmAction;
        SelectInfoButtonClicked += inputHandler.OnToggleInfoPanel;

        inputHandler.ConfirmAvailabilityChanged += SetConfirmInteractable;

        if (SelectInfoButton != null)
            SelectInfoButton.onClick.AddListener(HandleInfoClick);

        SetConfirmInteractable(false);
        HideConfirmPanel();
    }

    // -------------------------------------------------------------------------
    // API pública — controle de interatividade
    // -------------------------------------------------------------------------

    public void SetAddDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        allocator?.SetAddInteractable(isInteractable);
    }

    public void SetRemoveDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        allocator?.SetRemoveInteractable(isInteractable);
    }

    public void SetConfirmInteractable(bool isInteractable)
    {
        if (ConfirmButton != null)
            ConfirmButton.interactable = isInteractable;
    }

    public void SetAllInteractable(bool isInteractable)
    {
        if (SelectAttackButton != null) SelectAttackButton.interactable = isInteractable;
        if (SelectDefendButton != null) SelectDefendButton.interactable = isInteractable;
        if (EndTurnButton != null)      EndTurnButton.interactable      = isInteractable;
        if (ConfirmButton != null)      ConfirmButton.interactable      = isInteractable;
        if (SelectInfoButton != null)         SelectInfoButton.interactable         = isInteractable;

        // Substitui os 12 blocos if/interactable anteriores por um loop
        foreach (var allocator in diceAllocators)
            allocator?.SetAllInteractable(isInteractable);
    }

    // -------------------------------------------------------------------------
    // API pública — exibição e atualização
    // -------------------------------------------------------------------------

    public void UpdateDiceAllocationStats(int mind, int heart, int body)
    {
        if (diceAllocationView == null)
            diceAllocationView = FindObjectOfType<DiceAllocationView>();

        diceAllocationView?.UpdateStatValueTexts(mind, heart, body);
    }

    /// <summary>
    /// Atualiza o contador visível em uma linha específica de alocador.
    /// </summary>
    public void SetAllocatorCount(DiceStatType stat, DiceRollType rollType, int count)
    {
        FindAllocator(stat, rollType)?.SetCount(count);
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

    public void ShowConfirmPanel(string actionLabel)
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(true);

        if (ConfirmActionText != null)
            ConfirmActionText.text = actionLabel;
    }

    public void HideConfirmPanel()
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(false);
    }

    public void UpdateSelectionPreview(
        int actionPower,
        IReadOnlyList<DiceStatType> powerDiceTypes,
        IReadOnlyList<int> powerFaces,
        IReadOnlyList<int> aggregatedPowerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        (int lowMax, int mediumMax, int highMin) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin) accuracyTierBoundaries)
    {
        if (diceAllocationView == null)
            return;

        diceAllocationView.UpdateSelectionPreview(
            actionPower,
            powerDiceTypes,
            powerFaces,
            aggregatedPowerFaces,
            accuracyDiceTypes,
            accuracyFaces,
            powerTierBoundaries,
            accuracyTierBoundaries);
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

    private void HandleSelectAttackClick()      => SelectAttackClicked?.Invoke();
    private void HandleSelectDefendClick()      => SelectDefendClicked?.Invoke();
    private void HandleSkipTurnClick()          => SkipTurnClicked?.Invoke();
    private void HandleConfirmClick()           => ConfirmClicked?.Invoke();
    private void HandleInfoClick() => SelectInfoButtonClicked?.Invoke();

    private void HandleItemsClick()
    {
        inventoryView?.Open();
    }

    private void HandleTricksClick()
    {
        trickInventoryView?.Open();
    }

    // -------------------------------------------------------------------------
    // Handlers privados — alocadores de dado
    // -------------------------------------------------------------------------

    // Bubbles o evento do componente filho para o evento público desta View,
    // preservando o padrão View → InputHandler já estabelecido no projeto.
    private void HandleAllocatorAddPressed(DiceStatType stat, DiceRollType roll)
        => AddDiceClicked?.Invoke(stat, roll);

    private void HandleAllocatorRemovePressed(DiceStatType stat, DiceRollType roll)
        => RemoveDiceClicked?.Invoke(stat, roll);

    // -------------------------------------------------------------------------
    // Utilitário
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retorna o alocador correspondente ao par (stat, rollType),
    /// ou null se não encontrado.
    /// </summary>
    private DiceStatAllocatorUI FindAllocator(DiceStatType stat, DiceRollType rollType)
    {
        foreach (var allocator in diceAllocators)
        {
            if (allocator != null && allocator.StatType == stat && allocator.RollType == rollType)
                return allocator;
        }

        Debug.LogWarning($"[ActionPanelView] Alocador não encontrado para {stat} + {rollType}.");
        return null;
    }
}