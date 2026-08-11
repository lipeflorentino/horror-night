using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationView : MonoBehaviour
{

    [Header("Selection Preview")]
    [SerializeField] private RectTransform powerDiceContainer;
    [SerializeField] private RectTransform accuracyDiceContainer;
    [SerializeField] private DiceAllocationItemUI allocationItemPrefab;
    [SerializeField] private TMP_Text diceTiersText;
    [SerializeField] private TMP_Text accuracyResultPanelText;
    [SerializeField] private TMP_Text powerResultPanelText;
    [SerializeField] private TMP_Text overallResultPanelText;
    
    [Header("Painel de Alocação")]
    [SerializeField] private GameObject allocationPanel;
    [SerializeField] private TMP_Text allocationActionText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Alocadores de Dado — gerados em runtime")]
    [SerializeField] private RectTransform accuracyAllocatorsContainer; 
    [SerializeField] private RectTransform powerAllocatorsContainer;
    [SerializeField] private DiceStatAllocatorUI allocatorPrefab;

    [Header("Barra de níveis de rolagem")]
    [SerializeField] private DiceTierBarUI accuracyTierBar;
    [SerializeField] private DiceTierBarUI powerTierBar;

    [Header("Estratégia de Threshold")]
    [SerializeField] private TMP_Dropdown thresholdStrategyDropdown;

    private DiceStatAllocatorUI[] diceAllocators;
    public event Action<DiceStatType, DiceRollType> AddDiceClicked;
    public event Action<DiceStatType, DiceRollType> RemoveDiceClicked;
    public event Action<CombatRules.ThresholdStrategy> ThresholdStrategyChanged;
    private CombatInputHandler boundInputHandler;

    [Header("Feedback de Custo de Alocação")]
    [SerializeField] private GameObject allocationCostPanel;      // pai que liga/desliga
    [SerializeField] private RectTransform allocationCostContainer; // tem VerticalLayoutGroup no prefab/cena
    [SerializeField] private DiceAllocationCostItemUI allocationCostItemPrefab;

    private static readonly DiceStatType[] StatOrder = { DiceStatType.Mind, DiceStatType.Heart, DiceStatType.Body };
    private readonly List<DiceAllocationCostItemUI> allocationCostPool = new();
    private const string DiceIconKey = "Dices"; // espera Resources/UI/Stats/DiceIcon

    [Header("Pool de Dados")]
    [SerializeField] private TMP_Text dicePoolText;
    [SerializeField] private TMP_Text powerDicePoolText;
    [SerializeField] private TMP_Text accuracyDicePoolText;

    public event Action ConfirmClicked;

    private const string ConfirmTooltipDefault = "Confirmar";
    private const string ConfirmTooltipPowerPending = "Power dice not allocated";
    private const string ConfirmTooltipAccuracyPending = "Accuracy dice not allocated";
    private Tooltipable tooltipable;

    private void Awake()
    {
        InstantiateAllocators();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HandleConfirmClick);
        }
            

        if (closeButton != null)
            closeButton.onClick.AddListener(HideAllocationPanel);

        if (thresholdStrategyDropdown != null)
        {
            thresholdStrategyDropdown.ClearOptions();
            thresholdStrategyDropdown.AddOptions(new List<string> { "Seguro", "Equilibrado", "Arriscado" });
            thresholdStrategyDropdown.SetValueWithoutNotify((int)CombatRules.ThresholdStrategy.Balanced);
            thresholdStrategyDropdown.onValueChanged.AddListener(HandleThresholdStrategyChanged);
        }

        HideAllocationPanel();
        tooltipable = confirmButton.GetOrAddComponent<Tooltipable>();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirmClick);

        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (thresholdStrategyDropdown != null)
            thresholdStrategyDropdown.onValueChanged.RemoveListener(HandleThresholdStrategyChanged);

        if (diceAllocators != null)
        {
            foreach (var allocator in diceAllocators)
            {
                if (allocator == null) continue;
                allocator.OnAddPressed -= HandleAllocatorAddPressed;
                allocator.OnRemovePressed -= HandleAllocatorRemovePressed;
            }
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
            var container = combinations[i].roll == DiceRollType.Power ? powerAllocatorsContainer : accuracyAllocatorsContainer;
            DiceStatAllocatorUI allocator = Instantiate(allocatorPrefab, container);
            allocator.Initialize(combinations[i].stat, combinations[i].roll);
            allocator.OnAddPressed += HandleAllocatorAddPressed;
            allocator.OnRemovePressed += HandleAllocatorRemovePressed;

            diceAllocators[i] = allocator;
        }
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        if (boundInputHandler != null)
        {
            AddDiceClicked -= boundInputHandler.OnAddDice;
            RemoveDiceClicked -= boundInputHandler.OnRemoveDice;
        }

        boundInputHandler = inputHandler;
        
        if (boundInputHandler != null)
        {
            AddDiceClicked += boundInputHandler.OnAddDice;
            RemoveDiceClicked += boundInputHandler.OnRemoveDice;
        }

        inputHandler.BindDiceAllocationView(this);
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

    private void HandleConfirmClick() {
        confirmButton.GetComponent<Tooltipable>().HideTooltip();
        ConfirmClicked?.Invoke();
    }

    // -------------------------------------------------------------------------
    // API pública — Preview e Exibição
    // -------------------------------------------------------------------------

    public void UpdateSelectionPreview(DiceAllocationContext previewData)
    {
        RebuildAllocationContainer(powerDiceContainer, previewData.PowerDiceTypes, previewData.PowerFaces);
        RebuildAllocationContainer(accuracyDiceContainer, previewData.AccuracyDiceTypes, previewData.AccuracyFaces);
        
        UpdateDiceTiersLabel(previewData.PowerTierBoundaries, previewData.AccuracyTierBoundaries);
        UpdateResultPanel(previewData);
    }

    public void UpdateDiceAllocationStats(int mind, int heart, int body)
    {
        foreach (var allocator in diceAllocators)
        {
            if (allocator == null) continue;
            
            if (allocator.StatType == DiceStatType.Mind)
                allocator.SetStatValue(mind);
            else if (allocator.StatType == DiceStatType.Heart)
                allocator.SetStatValue(heart);
            else if (allocator.StatType == DiceStatType.Body)
                allocator.SetStatValue(body);
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

    private void UpdateDiceTiersLabel(
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries)
    {
        if (powerTierBar != null)
        {
            powerTierBar.SetBoundaries(
                powerTierBoundaries.lowMax,
                powerTierBoundaries.mediumMax,
                powerTierBoundaries.highMin,
                powerTierBoundaries.maxValue);
        }

        if (accuracyTierBar != null)
        {
            accuracyTierBar.SetBoundaries(
                accuracyTierBoundaries.lowMax,
                accuracyTierBoundaries.mediumMax,
                accuracyTierBoundaries.highMin,
                accuracyTierBoundaries.maxValue);
        }
    }

    private void UpdateResultPanel(DiceAllocationContext data)
    {
        if (accuracyResultPanelText == null || powerResultPanelText == null)
            return;

        if (!data.HasPower && !data.HasAccuracy)
        {
            accuracyResultPanelText.text = string.Empty;
            powerResultPanelText.text = string.Empty;
            if (overallResultPanelText != null)
                overallResultPanelText.text = string.Empty;
            return;
        }

        StringBuilder accSb = new();
        StringBuilder powSb = new();

        if (data.HasAccuracy)
        {
            string missThresholdText = data.MissThreshold > 0 ? $"1-{data.MissThreshold}" : "--";
            
            accSb.AppendLine($"<color={Colorization.AccuracyColorHex}><b><size=120%>ACCURACY</size></b></color>");
            accSb.AppendLine();
            accSb.AppendLine($"Miss Threshold: {ColorValue(missThresholdText, GetLowerThresholdColor(data.MissThreshold, data.AccuracyTierBoundaries.maxValue))}");
            accSb.AppendLine($"Miss Chance:{ColorValue(data.AccuracyChances.Low.ToString("P0"), GetBadChanceColor(data.AccuracyChances.Low))}");
            accSb.AppendLine($"Hit Threshold: {ColorValue($"{data.HitThreshold}+", GetLowerThresholdColor(data.HitThreshold, data.AccuracyTierBoundaries.maxValue))}");
            accSb.AppendLine($"Hit Chance: {ColorValue((data.AccuracyChances.Medium + data.AccuracyChances.High).ToString("P0"), GetGoodChanceColor(data.AccuracyChances.Medium + data.AccuracyChances.High))}");
            accSb.AppendLine($"Critical Threshold: {ColorValue(data.CriticalThreshold > 0 ? $"{data.CriticalThreshold}+" : "--", GetLowerThresholdColor(data.CriticalThreshold, data.AccuracyTierBoundaries.maxValue))}");
            accSb.AppendLine($"Critical Chance: {ColorValue(data.AccuracyChances.High.ToString("P0"), GetGoodChanceColor(data.AccuracyChances.High))}");
            accSb.AppendLine($"Max Accuracy Threshold: {ColorValue(data.AccuracyTierBoundaries.maxValue.ToString(), GetLowerThresholdColor(data.AccuracyTierBoundaries.maxValue, data.AccuracyTierBoundaries.maxValue))}");
            accSb.AppendLine($"Max Accuracy Chance: {ColorValue(data.AccuracyMaxRollChance.ToString("P0"), GetGoodChanceColor(data.AccuracyMaxRollChance))}");
        }
        
        if (data.HasPower)
        {
            powSb.AppendLine($"<color={Colorization.PowerColorHex}><b><size=120%>POWER</size></b></color>");
            powSb.AppendLine();
            powSb.AppendLine($"Damage (Min/Max): {ColorValue(data.MinDamage.ToString("F0"), GetTierColor(data.MinPowerTier))}-{ColorValue(data.MaxDamage.ToString("F0"), GetTierColor(data.MaxPowerTier))}");
            powSb.AppendLine($"Damage Multiplier: {ColorValue(data.DamageMultiplier.ToString(), GetGoodChanceColor(data.DamageMultiplier))}x");
            powSb.AppendLine($"Max Power Threshold: {ColorValue(data.PowerTierBoundaries.maxValue.ToString(), GetLowerThresholdColor(data.PowerTierBoundaries.maxValue, data.PowerTierBoundaries.maxValue))}");
            powSb.AppendLine($"Max Power Chance: {ColorValue(data.PowerMaxRollChance.ToString("P0"), GetGoodChanceColor(data.PowerMaxRollChance))}");
        }

        if (data.HasPower && data.HasAccuracy)
        {
            string consistencyColorHex = GetConsistencyColor(data.Consistency);
            if (overallResultPanelText != null)
                overallResultPanelText.text = $"[ Overall <color={consistencyColorHex}>{data.Consistency}</color> ]";
        }
        
        accuracyResultPanelText.text = accSb.ToString();
        powerResultPanelText.text = powSb.ToString();
    }

    public void UpdateAllocationCostFeedback(IReadOnlyDictionary<DiceStatType, int> costsByStat)
    {
        int usedCount = 0;
        int totalDiceCost = 0;

        foreach (DiceStatType stat in StatOrder)
        {
            if (!costsByStat.TryGetValue(stat, out int amount) || amount <= 0)
                continue;

            DiceAllocationCostItemUI item = GetOrCreatePooledCostItem(usedCount);
            item.Bind(stat, amount);
            item.gameObject.SetActive(true);
            usedCount++;
            totalDiceCost += amount;
        }

        if (totalDiceCost > 0)
        {
            DiceAllocationCostItemUI diceItem = GetOrCreatePooledCostItem(usedCount);
            diceItem.Bind(DiceIconKey, totalDiceCost);
            diceItem.gameObject.SetActive(true);
            usedCount++;
        }

        for (int i = usedCount; i < allocationCostPool.Count; i++)
            allocationCostPool[i].gameObject.SetActive(false);

        if (allocationCostPanel != null)
            allocationCostPanel.SetActive(usedCount > 0);
    }

    public void UpdateDicePoolDisplay(int current, int max, int allocatedPowerDice, int allocatedAccuracyDice)
    {
        if (dicePoolText != null)
            dicePoolText.text = $"{current}/{max}";

        if (powerDicePoolText != null)
            powerDicePoolText.text = $"<color={(allocatedPowerDice > 0 ? Colorization.WhiteColorHex : Colorization.BadColorHex)}>{allocatedPowerDice}/{current}</color>";

        if (accuracyDicePoolText != null)
            accuracyDicePoolText.text = $"<color={(allocatedAccuracyDice > 0 ? Colorization.WhiteColorHex : Colorization.BadColorHex)}>{allocatedAccuracyDice}/{current}</color>";

        UpdateConfirmTooltip(allocatedPowerDice, allocatedAccuracyDice);
    }

    private void UpdateConfirmTooltip(int allocatedPowerDice, int allocatedAccuracyDice)
    {
        if (confirmButton == null)
            return;

        string tooltip = allocatedAccuracyDice <= 0
            ? ConfirmTooltipAccuracyPending
            : allocatedPowerDice <= 0
                ? ConfirmTooltipPowerPending
                : ConfirmTooltipDefault;

        if (allocatedPowerDice <= 0 || allocatedAccuracyDice <= 0)
        {
            tooltipable.SetTooltipColor(TooltipUI.TooltipColor.Red);
        }
        else
        {
            tooltipable.SetTooltipColor(TooltipUI.TooltipColor.Default);
        }

        tooltipable.SetTooltipText(tooltip);
    }

    private DiceAllocationCostItemUI GetOrCreatePooledCostItem(int index)
    {
        if (index < allocationCostPool.Count)
            return allocationCostPool[index];

        DiceAllocationCostItemUI item = Instantiate(allocationCostItemPrefab, allocationCostContainer);
        allocationCostPool.Add(item);
        return item;
    }

    // -------------------------------------------------------------------------
    // Funções de formatação e cor (Responsabilidade exclusiva da View)
    // -------------------------------------------------------------------------

    private string ColorValue(string value, string colorHex) => $"<color={colorHex}>{value}</color>";

    private string GetGoodChanceColor(float chance)
    {
        return chance >= 0.60f ? Colorization.GoodColorHex : chance >= 0.35f ? Colorization.MediumColorHex : Colorization.BadColorHex;
    }

    private string GetBadChanceColor(float chance)
    {
        return chance <= 0.20f ? Colorization.GoodColorHex : chance <= 0.45f ? Colorization.MediumColorHex : Colorization.BadColorHex;
    }

    private string GetLowerThresholdColor(int threshold, int maximum)
    {
        if (threshold <= 0 || maximum <= 0)
            return Colorization.GoodColorHex;

        float relativeThreshold = threshold / (float)maximum;
        return relativeThreshold <= 0.33f ? Colorization.GoodColorHex : relativeThreshold <= 0.66f ? Colorization.MediumColorHex : Colorization.BadColorHex;
    }

    private string GetTierColor(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => Colorization.BadColorHex,
            DiceTier.Medium => Colorization.MediumColorHex,
            DiceTier.High => Colorization.GoodColorHex,
            _ => Colorization.MediumColorHex,
        };
    }

    private string GetConsistencyColor(AllocationConsistency consistency)
    {
        return consistency switch
        {
            AllocationConsistency.Balanced => Colorization.MediumColorHex,
            AllocationConsistency.Favorable => Colorization.GoodColorHex,
            AllocationConsistency.Unfavorable => Colorization.BadColorHex,
            _ => Colorization.MediumColorHex
        };
    }

    // -------------------------------------------------------------------------
    // API pública — Controle de interatividade
    // -------------------------------------------------------------------------

    public void SetAddDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        if (allocator != null) allocator.SetAddInteractable(isInteractable);
    }

    public void SetRemoveDiceButtonInteractable(DiceStatType stat, DiceRollType rollType, bool isInteractable)
    {
        var allocator = FindAllocator(stat, rollType);
        if (allocator != null) allocator.SetRemoveInteractable(isInteractable);
    }

    public void SetAllAllocatorButtonsInteractable(bool isInteractable)
    {
        foreach (var allocator in diceAllocators)
        {
            if (allocator != null)
                allocator.SetAllInteractable(isInteractable);
        }
    }

    public void SetAllocatorCount(DiceStatType stat, DiceRollType rollType, int count)
    {
        var allocator = FindAllocator(stat, rollType);
        if (allocator != null) allocator.SetCount(count);
    }

    // -------------------------------------------------------------------------
    // Handlers e utilitários
    // -------------------------------------------------------------------------

    private void HandleAllocatorAddPressed(DiceStatType stat, DiceRollType roll)
        => AddDiceClicked?.Invoke(stat, roll);

    private void HandleAllocatorRemovePressed(DiceStatType stat, DiceRollType roll)
        => RemoveDiceClicked?.Invoke(stat, roll);

    private void HandleThresholdStrategyChanged(int index) 
        => ThresholdStrategyChanged?.Invoke((CombatRules.ThresholdStrategy)index);

    private DiceStatAllocatorUI FindAllocator(DiceStatType stat, DiceRollType rollType)
    {
        foreach (var allocator in diceAllocators)
        {
            if (allocator != null && allocator.StatType == stat && allocator.RollType == rollType)
                return allocator;
        }

        Debug.LogWarning($"[DiceAllocationView] Alocador não encontrado para {stat} + {rollType}.");
        return null;
    }
}