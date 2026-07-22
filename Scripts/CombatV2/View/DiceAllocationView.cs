using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationView : MonoBehaviour
{
    private const string BadColorHex = "#E05C5C";
    private const string MediumColorHex = "#F59E0B";
    private const string GoodColorHex = "#4CAF50";

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

        if (powerDiceTypes == null || powerDiceTypes.Count == 0 ||
            accuracyDiceTypes == null || accuracyDiceTypes.Count == 0)
        {
            resultPanelText.text = string.Empty;
            return;
        }

        int hitThreshold = accuracyTierBoundaries.lowMax + 1;
        int criticalThreshold = accuracyTierBoundaries.highMin;
        int missThreshold = accuracyTierBoundaries.lowMax;

        Dictionary<int, float> powerDistribution = CalculateBestRollDistribution(powerDiceTypes, powerFaces);
        Dictionary<int, float> accuracyDistribution = CalculateBestRollDistribution(accuracyDiceTypes, accuracyFaces);
        TierChances powerChances = CalculateTierChances(powerDistribution, powerTierBoundaries);
        TierChances accuracyChances = CalculateTierChances(accuracyDistribution, accuracyTierBoundaries);
        RollExtremes powerExtremes = CalculateRollExtremes(powerDistribution);
        RollExtremes accuracyExtremes = CalculateRollExtremes(accuracyDistribution);
        (int minPower, int maxPower) = GetDistributionBounds(powerDistribution);
        float minDamage = actionPower * GetMultiplier(GetTier(minPower, powerTierBoundaries));
        float maxDamage = actionPower * GetMultiplier(GetTier(maxPower, powerTierBoundaries));

        var (consistencyLabel, consistencyColorHex) = GetAllocationConsistency(powerChances, accuracyChances);
        float hitChance = accuracyChances.Medium + accuracyChances.High;
        string missThresholdText = missThreshold > 0 ? $"{missThreshold}-" : "--";

        StringBuilder sb = new();
        sb.AppendLine($"Result Instance: <color={consistencyColorHex}>[{consistencyLabel}]</color>");
        sb.AppendLine($"Damage: {ColorValue(minDamage.ToString("F0"), GetTierColor(GetTier(minPower, powerTierBoundaries)))}-{ColorValue(maxDamage.ToString("F0"), GetTierColor(GetTier(maxPower, powerTierBoundaries)))}");
        sb.AppendLine($"Rolagem Máxima/Mínima (Power): {ColorValue(powerExtremes.Maximum.ToString("P0"), GetGoodChanceColor(powerExtremes.Maximum))} / {ColorValue(powerExtremes.Minimum.ToString("P0"), GetBadChanceColor(powerExtremes.Minimum))}");
        sb.AppendLine($"Rolagem Máxima/Mínima (Accuracy): {ColorValue(accuracyExtremes.Maximum.ToString("P0"), GetGoodChanceColor(accuracyExtremes.Maximum))} / {ColorValue(accuracyExtremes.Minimum.ToString("P0"), GetBadChanceColor(accuracyExtremes.Minimum))}");
        sb.AppendLine($"Hit Threshold: {ColorValue($"{hitThreshold}+", GetLowerThresholdColor(hitThreshold, accuracyTierBoundaries.maxValue))}");
        sb.AppendLine($"Hit Chance: {ColorValue(hitChance.ToString("P0"), GetGoodChanceColor(hitChance))}");
        sb.AppendLine($"Miss Threshold: {ColorValue(missThresholdText, GetLowerThresholdColor(missThreshold, accuracyTierBoundaries.maxValue))}");
        sb.AppendLine($"Miss Chance: {ColorValue(accuracyChances.Low.ToString("P0"), GetBadChanceColor(accuracyChances.Low))}");
        sb.AppendLine($"Critical Threshold: {ColorValue(criticalThreshold > 0 ? $"{criticalThreshold}+" : "--", GetLowerThresholdColor(criticalThreshold, accuracyTierBoundaries.maxValue))}");
        sb.AppendLine($"Critical Chance: {ColorValue(accuracyChances.High.ToString("P0"), GetGoodChanceColor(accuracyChances.High))}");
        // TODO: append line for effects
        resultPanelText.text = sb.ToString();
    }

    private string ColorValue(string value, string colorHex)
    {
        return $"<color={colorHex}>{value}</color>";
    }

    private string GetGoodChanceColor(float chance)
    {
        return chance >= 0.60f ? GoodColorHex : chance >= 0.35f ? MediumColorHex : BadColorHex;
    }

    private string GetBadChanceColor(float chance)
    {
        return chance <= 0.20f ? GoodColorHex : chance <= 0.45f ? MediumColorHex : BadColorHex;
    }

    private string GetLowerThresholdColor(int threshold, int maximum)
    {
        if (threshold <= 0 || maximum <= 0)
            return GoodColorHex;

        float relativeThreshold = threshold / (float)maximum;
        return relativeThreshold <= 0.33f ? GoodColorHex : relativeThreshold <= 0.66f ? MediumColorHex : BadColorHex;
    }

    private string GetTierColor(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => BadColorHex,
            DiceTier.Medium => MediumColorHex,
            DiceTier.High => GoodColorHex,
            _ => MediumColorHex,
        };
    }

    /// <summary>
    /// Classifica a alocação pelo resultado completo. Favorável = crítico ou hit forte;
    /// desfavorável = miss ou hit fraco; médio = hit de poder médio.
    /// </summary>
    private (string label, string colorHex) GetAllocationConsistency(TierChances power, TierChances accuracy)
    {
        float favorable = accuracy.High + accuracy.Medium * power.High;
        float unfavorable = accuracy.Low + accuracy.Medium * power.Low;
        float medium = accuracy.Medium * power.Medium;

        if (medium >= favorable && medium >= unfavorable)
            return ("Equilibrado", MediumColorHex);

        if (favorable > unfavorable)
            return ("Consistente", GoodColorHex);

        return ("Arriscado", BadColorHex);
    }

    /// <summary>
    /// Calcula a distribuição exata do melhor resultado: soma dados do mesmo atributo e,
    /// em seguida, aplica a mesma regra de DiceService.GetBestResult (maior valor vence).
    /// </summary>
    private Dictionary<int, float> CalculateBestRollDistribution(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces)
    {
        if (types == null || faces == null || types.Count == 0 || types.Count != faces.Count)
            return new Dictionary<int, float> { { 1, 1f } };

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

        Dictionary<int, float> bestDistribution = new() { { 0, 1f } };
        foreach (var group in groupFaces.Values)
        {
            Dictionary<int, float> groupDistribution = new() { { 0, 1f } };
            foreach (int faceCount in group)
            {
                Dictionary<int, float> nextDistribution = new();
                foreach (var current in groupDistribution)
                    for (int face = 1; face <= faceCount; face++)
                    {
                        int value = current.Key + face;
                        float chance = current.Value / faceCount;
                        nextDistribution[value] = nextDistribution.TryGetValue(value, out float accumulated)
                            ? accumulated + chance
                            : chance;
                    }
                groupDistribution = nextDistribution;
            }

            Dictionary<int, float> nextBestDistribution = new();
            foreach (var best in bestDistribution)
                foreach (var groupValue in groupDistribution)
                {
                    int value = Mathf.Max(best.Key, groupValue.Key);
                    float chance = best.Value * groupValue.Value;
                    nextBestDistribution[value] = nextBestDistribution.TryGetValue(value, out float accumulated)
                        ? accumulated + chance
                        : chance;
                }
            bestDistribution = nextBestDistribution;
        }

        return bestDistribution;
    }

    private TierChances CalculateTierChances(Dictionary<int, float> distribution, (int lowMax, int mediumMax, int highMin, int maxValue) boundaries)
    {
        TierChances chances = new();
        foreach (var result in distribution)
        {
            if (result.Key <= boundaries.lowMax)
                chances.Low += result.Value;
            else if (result.Key <= boundaries.mediumMax)
                chances.Medium += result.Value;
            else
                chances.High += result.Value;
        }
        return chances;
    }

    private RollExtremes CalculateRollExtremes(Dictionary<int, float> distribution)
    {
        (int minimum, int maximum) = GetDistributionBounds(distribution);

        return new RollExtremes
        {
            Minimum = distribution.TryGetValue(minimum, out float minimumChance) ? minimumChance : 0f,
            Maximum = distribution.TryGetValue(maximum, out float maximumChance) ? maximumChance : 0f
        };
    }

    private (int minimum, int maximum) GetDistributionBounds(Dictionary<int, float> distribution)
    {
        int minimum = int.MaxValue;
        int maximum = int.MinValue;
        foreach (int value in distribution.Keys)
        {
            minimum = Mathf.Min(minimum, value);
            maximum = Mathf.Max(maximum, value);
        }
        return (minimum, maximum);
    }

    private struct TierChances
    {
        public float Low;
        public float Medium;
        public float High;
    }

    private struct RollExtremes
    {
        public float Minimum;
        public float Maximum;
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
