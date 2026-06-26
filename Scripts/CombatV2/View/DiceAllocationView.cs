using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationView : MonoBehaviour
{
    [Header("Selection Preview")]
    [SerializeField] private RectTransform powerDiceContainer;
    [SerializeField] private RectTransform accuracyDiceContainer;
    [SerializeField] private DiceAllocationItemUI allocationItemPrefab;
    [SerializeField] private TMP_Text diceTiersText;
    [SerializeField] private TMP_Text resultPanelText;
    
    [Header("Painel de Alocação")]
    [SerializeField] private GameObject allocationPanel;
    [SerializeField] private TMP_Text allocationActionText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Alocadores de Dado — gerados em runtime (Mind/Heart/Body × Power/Accuracy)")]
    [SerializeField] private RectTransform accuracyAllocatorsContainer; 
    [SerializeField] private RectTransform powerAllocatorsContainer;
    [SerializeField] private DiceStatAllocatorUI allocatorPrefab;

    private DiceStatAllocatorUI[] diceAllocators;

    public event Action<DiceStatType, DiceRollType> AddDiceClicked;
    public event Action<DiceStatType, DiceRollType> RemoveDiceClicked;
     private CombatInputHandler boundInputHandler;

    public event Action ConfirmClicked;

    private void Awake()
    {
        InstantiateAllocators();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirmClick);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => HideAllocationPanel());

        HideAllocationPanel();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirmClick);

        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        foreach (var allocator in diceAllocators)
        {
            if (allocator == null) continue;
            allocator.OnAddPressed    -= HandleAllocatorAddPressed;
            allocator.OnRemovePressed -= HandleAllocatorRemovePressed;
        }
    }

    private void InstantiateAllocators()
    {
        if (allocatorPrefab == null || accuracyAllocatorsContainer == null || powerAllocatorsContainer == null)
        {
            Debug.LogError("[DiceAllocationView] allocatorPrefab ou allocatorsContainer não atribuídos.");
            diceAllocators = Array.Empty<DiceStatAllocatorUI>();
            return;
        }

        var combinations = new (DiceStatType stat, DiceRollType roll)[]
        {
            (DiceStatType.Mind,  DiceRollType.Power),
            (DiceStatType.Mind,  DiceRollType.Accuracy),
            (DiceStatType.Heart, DiceRollType.Power),
            (DiceStatType.Heart, DiceRollType.Accuracy),
            (DiceStatType.Body,  DiceRollType.Power),
            (DiceStatType.Body,  DiceRollType.Accuracy),
        };

        diceAllocators = new DiceStatAllocatorUI[combinations.Length];

        for (int i = 0; i < combinations.Length; i++)
        {
            if (combinations[i].roll == DiceRollType.Power)
            {
                DiceStatAllocatorUI powerAllocator = Instantiate(allocatorPrefab, powerAllocatorsContainer);
                powerAllocator.Initialize(combinations[i].stat, combinations[i].roll);
                powerAllocator.OnAddPressed    += HandleAllocatorAddPressed;
                powerAllocator.OnRemovePressed += HandleAllocatorRemovePressed;

                diceAllocators[i] = powerAllocator;
            }  
            else
            {
                DiceStatAllocatorUI accuracyAllocator = Instantiate(allocatorPrefab, accuracyAllocatorsContainer);
                accuracyAllocator.Initialize(combinations[i].stat, combinations[i].roll);
                accuracyAllocator.OnAddPressed    += HandleAllocatorAddPressed;
                accuracyAllocator.OnRemovePressed += HandleAllocatorRemovePressed;

                diceAllocators[i] = accuracyAllocator;
            }
        }
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        inputHandler.BindDiceAllocationView(this);

        if (boundInputHandler != null)
        {
            AddDiceClicked          -= boundInputHandler.OnAddDice;
            RemoveDiceClicked       -= boundInputHandler.OnRemoveDice;
        }

        boundInputHandler = inputHandler;
        
        AddDiceClicked          += inputHandler.OnAddDice;
        RemoveDiceClicked       += inputHandler.OnRemoveDice;
    }

    // -------------------------------------------------------------------------
    // API pública — Painel de Alocação
    // -------------------------------------------------------------------------

    public void ShowAllocationPanel(string actionLabel)
    {
        if (allocationPanel != null)
            allocationPanel.SetActive(true);

        if (allocationActionText != null)
            allocationActionText.text = actionLabel;
    }

    public void HideAllocationPanel()
    {
        if (allocationPanel != null)
            allocationPanel.SetActive(false);
    }

    public void SetConfirmInteractable(bool isInteractable)
    {
        if (confirmButton != null)
            confirmButton.interactable = isInteractable;
    }

    // -------------------------------------------------------------------------
    // Handler privado
    // -------------------------------------------------------------------------

    private void HandleConfirmClick() => ConfirmClicked?.Invoke();

    // -------------------------------------------------------------------------
    // API pública — Preview
    // -------------------------------------------------------------------------

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
        RebuildAllocationContainer(powerDiceContainer, powerDiceTypes, powerFaces);
        RebuildAllocationContainer(accuracyDiceContainer, accuracyDiceTypes, accuracyFaces);
        UpdateDiceTiersLabel(powerTierBoundaries, accuracyTierBoundaries);
        UpdateResultPanel(actionPower, aggregatedPowerFaces, powerTierBoundaries, accuracyTierBoundaries);
    }

    

    // -------------------------------------------------------------------------
    // API pública — exibição e atualização
    // -------------------------------------------------------------------------

    public void UpdateDiceAllocationStats(int mind, int heart, int body)
    {
        foreach (var allocator in diceAllocators)
        {
            if (allocator == null) continue;
            if (allocator.StatType == DiceStatType.Mind)
            {
                allocator.SetStatValue(mind);
            }
            else if (allocator.StatType == DiceStatType.Heart)
            {
                allocator.SetStatValue(heart);
            }
            else if (allocator.StatType == DiceStatType.Body)
            {
                allocator.SetStatValue(body);
            }
        }
    }

    private void RebuildAllocationContainer(RectTransform container, IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces)
    {
        if (container == null || allocationItemPrefab == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        if (types == null || faces == null)
            return;

        int count = Mathf.Min(types.Count, faces.Count);
        for (int i = 0; i < count; i++)
        {
            DiceAllocationItemUI item = Instantiate(allocationItemPrefab, container);
            item.Bind(types[i], faces[i]);
        }
    }

    // -------------------------------------------------------------------------
    // API pública — controle de interatividade
    // -------------------------------------------------------------------------

    public void SetAddDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        allocator.SetAddInteractable(isInteractable);
    }

    public void SetRemoveDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        allocator.SetRemoveInteractable(isInteractable);
    }

    public void SetAllAllocatorButtonsInteractable(bool isInteractable)
    {
        foreach (var allocator in diceAllocators)
            allocator.SetAllInteractable(isInteractable);
    }

    /// <summary>
    /// Atualiza o contador visível em uma linha específica de alocador.
    /// </summary>
    public void SetAllocatorCount(DiceStatType stat, DiceRollType rollType, int count)
    {
        FindAllocator(stat, rollType).SetCount(count);
    }

    private void UpdateDiceTiersLabel(
        (int lowMax, int mediumMax, int highMin) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin) accuracyTierBoundaries)
    {
        if (diceTiersText == null)
            return;

        string powerLow = powerTierBoundaries.lowMax > 0 ? $"1-{powerTierBoundaries.lowMax}" : "-";
        string powerMedium = powerTierBoundaries.mediumMax > powerTierBoundaries.lowMax
            ? $"{powerTierBoundaries.lowMax + 1}-{powerTierBoundaries.mediumMax}"
            : "-";
        string powerHigh = powerTierBoundaries.highMin > 0 ? $"{powerTierBoundaries.highMin}+" : "-";

        string accuracyLow = accuracyTierBoundaries.lowMax > 0 ? $"1-{accuracyTierBoundaries.lowMax}" : "-";
        string accuracyMedium = accuracyTierBoundaries.mediumMax > accuracyTierBoundaries.lowMax
            ? $"{accuracyTierBoundaries.lowMax + 1}-{accuracyTierBoundaries.mediumMax}"
            : "-";
        string accuracyHigh = accuracyTierBoundaries.highMin > 0 ? $"{accuracyTierBoundaries.highMin}+" : "-";

        diceTiersText.text = $"Power L({powerLow}) M({powerMedium}) H({powerHigh})\nAccuracy L({accuracyLow}) M({accuracyMedium}) H({accuracyHigh})";
    }

    private void UpdateResultPanel(
        int actionPower,
        IReadOnlyList<int> aggregatedPowerFaces,
        (int lowMax, int mediumMax, int highMin) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin) accuracyTierBoundaries)
    {
        if (resultPanelText == null)
            return;

        int minPower = SumMin(aggregatedPowerFaces);
        int maxPower = SumMax(aggregatedPowerFaces);

        float minDamage = actionPower * GetMultiplier(GetTier(minPower, powerTierBoundaries));
        float maxDamage = actionPower * GetMultiplier(GetTier(maxPower, powerTierBoundaries));

        int hitThreshold = accuracyTierBoundaries.lowMax + 1;
        int criticalThreshold = accuracyTierBoundaries.highMin;

        StringBuilder sb = new();
        sb.AppendLine($"Damage: {minDamage:F0}-{maxDamage:F0}");
        sb.AppendLine($"Hit Threshold: {hitThreshold}+");
        sb.AppendLine($"Critical Threshold: {(criticalThreshold > 0 ? $"{criticalThreshold}+" : "--")}");
        sb.Append("Effects: Critical Hit, Power Max (placeholder), Accuracy Max (evade/parry)");
        resultPanelText.text = sb.ToString();
    }

    private DiceTier GetTier(int value, (int lowMax, int mediumMax, int highMin) boundaries)
    {
        if (value <= boundaries.lowMax)
            return DiceTier.Low;
        else if (value <= boundaries.mediumMax)
            return DiceTier.Medium;
        else
            return DiceTier.High;
    }

    private float GetMultiplier(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => 0.5f,
            DiceTier.Medium => 1f,
            DiceTier.High => 1.5f,
            _ => 1f,
        };
    }

    private int SumMin(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0) return 0;
        int minValue = int.MaxValue;
        for (int i = 0; i < faces.Count; i++)
            minValue = Mathf.Min(minValue, Mathf.Max(1, faces[i]));
        return minValue;
    }

    private int SumMax(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0) return 0;
        int maxValue = 0;
        for (int i = 0; i < faces.Count; i++)
            maxValue = Mathf.Max(maxValue, Mathf.Max(1, faces[i]));
        return maxValue;
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