using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente modular que representa uma linha de alocação de dados.
/// Encapsula: Botão de Adicionar, Botão de Remover e Texto do Contador.
///
/// Configure StatType e RollType no Inspector para identificar
/// qual dado esta linha representa (ex: Mind + Power).
///
/// Uso no ActionPanelView:
///   allocator.OnAddPressed  += handler.OnAddDice;
///   allocator.OnRemovePressed += handler.OnRemoveDice;
///   allocator.SetCount(3);
/// </summary>
public class DiceStatAllocatorUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector — Configuração da linha
    // -------------------------------------------------------------------------

    [Header("Identidade do Dado")]
    [Tooltip("Qual atributo esta linha representa (Mind, Heart, Body).")]
    [SerializeField] private DiceStatType statType;

    [Tooltip("Qual tipo de rolagem esta linha representa (Power, Accuracy).")]
    [SerializeField] private DiceRollType rollType;

    [Header("Referências de UI")]
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text statValueText;

    // -------------------------------------------------------------------------
    // Eventos públicos
    // -------------------------------------------------------------------------

    /// <summary>Disparado quando o botão "+" é clicado.</summary>
    public event Action<DiceStatType, DiceRollType> OnAddPressed;

    /// <summary>Disparado quando o botão "-" é clicado.</summary>
    public event Action<DiceStatType, DiceRollType> OnRemovePressed;

    // -------------------------------------------------------------------------
    // Propriedades somente leitura — úteis para busca por stat/roll
    // -------------------------------------------------------------------------

    public DiceStatType StatType => statType;
    public DiceRollType RollType => rollType;

    // -------------------------------------------------------------------------
    // Ciclo de vida
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (addButton != null)
            addButton.onClick.AddListener(HandleAddClick);

        if (removeButton != null)
            removeButton.onClick.AddListener(HandleRemoveClick);
    }

    private void OnDestroy()
    {
        if (addButton != null)
            addButton.onClick.RemoveListener(HandleAddClick);

        if (removeButton != null)
            removeButton.onClick.RemoveListener(HandleRemoveClick);
    }

    // -------------------------------------------------------------------------
    // Handlers internos
    // -------------------------------------------------------------------------

    private void HandleAddClick()    => OnAddPressed?.Invoke(statType, rollType);
    private void HandleRemoveClick() => OnRemovePressed?.Invoke(statType, rollType);

    // -------------------------------------------------------------------------
    // API pública
    // -------------------------------------------------------------------------

    /// <summary>Atualiza o texto do contador exibido na linha.</summary>
    public void SetCount(int value)
    {
        if (countText != null)
            countText.text = value.ToString();
    }

    /// <summary>Controla se o botão "+" pode ser clicado.</summary>
    public void SetAddInteractable(bool isInteractable)
    {
        if (addButton != null)
            addButton.interactable = isInteractable;
    }

    /// <summary>Controla se o botão "-" pode ser clicado.</summary>
    public void SetRemoveInteractable(bool isInteractable)
    {
        if (removeButton != null)
            removeButton.interactable = isInteractable;
    }

    /// <summary>Ativa ou desativa ambos os botões da linha de uma vez.</summary>
    public void SetAllInteractable(bool isInteractable)
    {
        SetAddInteractable(isInteractable);
        SetRemoveInteractable(isInteractable);
    }

    public void SetStatValue(int value)
    {
        if (statValueText != null)
            statValueText.text = value.ToString();
    }
}