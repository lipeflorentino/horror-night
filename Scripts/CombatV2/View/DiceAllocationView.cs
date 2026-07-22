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

    [Header("Barra de níveis de rolagem")]
    [SerializeField] private DiceTierBarUI accuracyTierBar;
    [SerializeField] private DiceTierBarUI powerTierBar;

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
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries)
    {
        RebuildAllocationContainer(powerDiceContainer, powerDiceTypes, powerFaces);
        RebuildAllocationContainer(accuracyDiceContainer, accuracyDiceTypes, accuracyFaces);
        UpdateDiceTiersLabel(powerTierBoundaries, accuracyTierBoundaries);
        UpdateResultPanel(actionPower, powerDiceTypes, powerFaces, aggregatedPowerFaces, accuracyDiceTypes, accuracyFaces, powerTierBoundaries, accuracyTierBoundaries);
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

        int itemCount = Mathf.Min(types.Count, faces.Count);

        // Agrupa dados idênticos (mesmo tipo + mesma face) num único slot com multiplicador
        // (ex.: dado extra de perk), preservando espaço limitado do painel.
        List<(DiceStatType type, int face, int count)> groupedDice = new();
        for (int i = 0; i < itemCount; i++)
        {
            DiceStatType type = types[i];
            int face = Mathf.Max(1, faces[i]);

            int existingIndex = groupedDice.FindIndex(g => g.type == type && g.face == face);
            if (existingIndex >= 0)
            {
                var existing = groupedDice[existingIndex];
                groupedDice[existingIndex] = (existing.type, existing.face, existing.count + 1);
            }
            else
            {
                groupedDice.Add((type, face, 1));
            }
        }

        for (int i = 0; i < groupedDice.Count; i++)
        {
            DiceAllocationItemUI item = Instantiate(allocationItemPrefab, container);
            item.Bind(groupedDice[i].type, groupedDice[i].face, groupedDice[i].count);
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
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries)
    {
        powerTierBar.SetBoundaries(
            powerTierBoundaries.lowMax,
            powerTierBoundaries.mediumMax,
            powerTierBoundaries.highMin,
            powerTierBoundaries.maxValue);
            

        accuracyTierBar.SetBoundaries(
            accuracyTierBoundaries.lowMax,
            accuracyTierBoundaries.mediumMax,
            accuracyTierBoundaries.highMin,
            accuracyTierBoundaries.maxValue);
    }

    private void UpdateResultPanel(
        int actionPower,
        IReadOnlyList<DiceStatType> powerDiceTypes,
        IReadOnlyList<int> powerFaces,
        IReadOnlyList<int> aggregatedPowerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries)
    {
        if (resultPanelText == null)
            return;

        int minPower = SumMin(aggregatedPowerFaces);
        int maxPower = SumMax(aggregatedPowerFaces);

        float minDamage = actionPower * GetMultiplier(GetTier(minPower, powerTierBoundaries));
        float maxDamage = actionPower * GetMultiplier(GetTier(maxPower, powerTierBoundaries));

        int hitThreshold = accuracyTierBoundaries.lowMax + 1;
        int criticalThreshold = accuracyTierBoundaries.highMin;

        var (consistencyLabel, consistencyColorHex) = GetAllocationConsistency(powerDiceTypes, powerFaces);
        float powerMaxRollChance = CalculateMaxRollChance(powerDiceTypes, powerFaces);
        float accuracyMaxRollChance = CalculateMaxRollChance(accuracyDiceTypes, accuracyFaces);

        StringBuilder sb = new();
        sb.AppendLine($"Damage: {minDamage:F0}-{maxDamage:F0}  <color={consistencyColorHex}>[{consistencyLabel}]</color>");
        sb.AppendLine($"Chance de Rolagem Máxima/Mínima (Power): {powerMaxRollChance:P0}");
        sb.AppendLine($"Chance de Rolagem Máxima/Mínima (Accuracy): {accuracyMaxRollChance:P0}");
        sb.AppendLine($"Hit Threshold: {hitThreshold}+");
        sb.AppendLine($"Critical Threshold: {(criticalThreshold > 0 ? $"{criticalThreshold}+" : "--")}");
        // TODO: append line for effects
        resultPanelText.text = sb.ToString();
    }

    /// <summary>
    /// Classifica o padrão de alocação de dados de Poder em Consistente/Equilibrado/Arriscado,
    /// conforme a mecânica de Concentrar vs. Dispersar (Manual de Combate, seção 3).
    /// Concentrar (mesmo tipo, vários dados) = Consistente. Dispersar (tipos diferentes, 1 dado cada) = Arriscado.
    /// </summary>
    private (string label, string colorHex) GetAllocationConsistency(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces)
    {
        if (types == null || types.Count <= 1)
            return ("Consistente", "#4CAF50");

        var groupSizes = new Dictionary<DiceStatType, int>();
        foreach (var type in types)
            groupSizes[type] = groupSizes.TryGetValue(type, out int current) ? current + 1 : 1;

        int totalDice = types.Count;
        int largestGroup = 0;
        foreach (var group in groupSizes.Values)
            largestGroup = Mathf.Max(largestGroup, group);

        // 1 = todos os dados concentrados no mesmo tipo; 0 = todos dispersos em tipos distintos.
        float concentrationIndex = (largestGroup - 1f) / (totalDice - 1f);

        if (concentrationIndex >= 0.66f)
            return ("Consistente", "#4CAF50");
        if (concentrationIndex <= 0.33f)
            return ("Arriscado", "#E05C5C");
        return ("Equilibrado", "#D6B84A");
    }

    /// <summary>
    /// Calcula a chance aproximada de disparar Rolagem Máxima com a alocação atual de dados de Poder
    /// (Manual de Combate, seção 3.3). 
    /// </summary>
    private float CalculateMaxRollChance(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces)
    {
        if (types == null || faces == null || types.Count == 0 || types.Count != faces.Count)
            return 0f;

        var groupFaces = new Dictionary<DiceStatType, List<int>>();
        for (int i = 0; i < types.Count; i++)
        {
            if (!groupFaces.TryGetValue(types[i], out var list))
            {
                list = new List<int>();
                groupFaces[types[i]] = list;
            }
            list.Add(Mathf.Max(1, faces[i]));
        }

        // Cada tipo (grupo) é independente. Dados do mesmo tipo se somam (aggregate),
        // então maximizar o grupo exige que TODOS os dados dele caiam na face máxima ao mesmo tempo.
        float missChance = 1f;
        foreach (var group in groupFaces.Values)
        {
            float hitMaxChance = 1f;
            foreach (int faceCount in group)
                hitMaxChance *= 1f / faceCount;

            missChance *= 1f - hitMaxChance;
        }

        return 1f - missChance;
    }

    private DiceTier GetTier(int value, (int lowMax, int mediumMax, int highMin, int maxValue) boundaries)
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