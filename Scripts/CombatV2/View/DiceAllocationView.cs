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
        IReadOnlyList<int> powerMinFaces,
        DiceStatType powerPrimaryStat,
        IReadOnlyList<int> aggregatedPowerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        IReadOnlyList<int> accuracyMinFaces,
        DiceStatType accuracyPrimaryStat,
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries,
        IReadOnlyDictionary<DiceStatType, int> statBaseTargets)
    {
        RebuildAllocationContainer(powerDiceContainer, powerDiceTypes, powerFaces);
        RebuildAllocationContainer(accuracyDiceContainer, accuracyDiceTypes, accuracyFaces);
        UpdateDiceTiersLabel(powerTierBoundaries, accuracyTierBoundaries);
        UpdateResultPanel(
            actionPower,
            powerDiceTypes, powerFaces, powerMinFaces, powerPrimaryStat, aggregatedPowerFaces,
            accuracyDiceTypes, accuracyFaces, accuracyMinFaces, accuracyPrimaryStat,
            powerTierBoundaries, accuracyTierBoundaries, statBaseTargets);
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

    // powerMinFaces/accuracyMinFaces/powerPrimaryStat/accuracyPrimaryStat/aggregatedPowerFaces: reservados
    // para um refinamento futuro do Min Roll (hoje calculado sobre a distribuição combinada, o que já é
    // correto; ficariam aqui se quisermos considerar agility por dado individualmente no futuro).
    private void UpdateResultPanel(
        int actionPower,
        IReadOnlyList<DiceStatType> powerDiceTypes,
        IReadOnlyList<int> powerFaces,
        IReadOnlyList<int> powerMinFaces,
        DiceStatType powerPrimaryStat,
        IReadOnlyList<int> aggregatedPowerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        IReadOnlyList<int> accuracyMinFaces,
        DiceStatType accuracyPrimaryStat,
        (int lowMax, int mediumMax, int highMin, int maxValue) powerTierBoundaries,
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyTierBoundaries,
        IReadOnlyDictionary<DiceStatType, int> statBaseTargets)
    {
        if (resultPanelText == null)
            return;

        bool hasPower = powerDiceTypes != null && powerDiceTypes.Count > 0;
        bool hasAccuracy = accuracyDiceTypes != null && accuracyDiceTypes.Count > 0;
        if (!hasPower && !hasAccuracy)
        {
            resultPanelText.text = string.Empty;
            return;
        }

        int hitThreshold = accuracyTierBoundaries.lowMax + 1;
        int criticalThreshold = accuracyTierBoundaries.highMin;
        int missThreshold = accuracyTierBoundaries.lowMax;

        Dictionary<int, float> powerDistribution = hasPower ? CalculateBestRollDistribution(powerDiceTypes, powerFaces) : null;
        Dictionary<int, float> accuracyDistribution = hasAccuracy ? CalculateBestRollDistribution(accuracyDiceTypes, accuracyFaces) : null;
        TierChances powerChances = hasPower ? CalculateTierChances(powerDistribution, powerTierBoundaries) : new TierChances();
        TierChances accuracyChances = hasAccuracy ? CalculateTierChances(accuracyDistribution, accuracyTierBoundaries) : new TierChances();

        // Rolagem Mínima: chance de o resultado final (melhor grupo) cair no piso absoluto da distribuição.
        // Não sofre o problema do dado extra (extra só adiciona valor, nunca facilita o piso).
        float powerMinRollChance = hasPower ? CalculateMinRollChance(powerDistribution) : 0f;
        float accuracyMinRollChance = hasAccuracy ? CalculateMinRollChance(accuracyDistribution) : 0f;

        // Rolagem Máxima: chance de PELO MENOS UM grupo (stat) atingir seu alvo de referência —
        // o valor BASE da stat (mesmo usado pelo DiceService/GetTierReferenceMaxValue), não a soma
        // das faces dos dados daquele grupo. Corrige o caso de dado extra de perk (ex.: Heart 12,
        // 1 dado base + 1 extra: alvo continua 12, alcançável pela SOMA dos dois, não só 12+12).
        float powerMaxRollChance = hasPower ? CalculateMaxRollChance(powerDiceTypes, powerFaces, statBaseTargets) : 0f;
        float accuracyMaxRollChance = hasAccuracy ? CalculateMaxRollChance(accuracyDiceTypes, accuracyFaces, statBaseTargets) : 0f;

        (int minPower, int maxPower) = hasPower ? GetDistributionBounds(powerDistribution) : (0, 0);
        float minDamage = hasPower ? actionPower * GetMultiplier(GetTier(minPower, powerTierBoundaries)) : 0f;
        float maxDamage = hasPower ? actionPower * GetMultiplier(GetTier(maxPower, powerTierBoundaries)) : 0f;
        string missThresholdText = missThreshold > 0 ? $"{missThreshold}-" : "--";

        StringBuilder sb = new();
        if (hasPower)
        {
            sb.AppendLine("<b>POWER</b>");
            sb.AppendLine($"Damage (Min/Max): {ColorValue(minDamage.ToString("F0"), GetTierColor(GetTier(minPower, powerTierBoundaries)))}-{ColorValue(maxDamage.ToString("F0"), GetTierColor(GetTier(maxPower, powerTierBoundaries)))}");
            sb.AppendLine($"Low / Medium / High: {ColorValue(powerChances.Low.ToString("P0"), GetBadChanceColor(powerChances.Low))} / {ColorValue(powerChances.Medium.ToString("P0"), MediumColorHex)} / {ColorValue(powerChances.High.ToString("P0"), GetGoodChanceColor(powerChances.High))}");
            sb.AppendLine($"Max / Min Roll: {ColorValue(powerMaxRollChance.ToString("P0"), GetGoodChanceColor(powerMaxRollChance))} / {ColorValue(powerMinRollChance.ToString("P0"), GetBadChanceColor(powerMinRollChance))}");
        }

        if (hasPower && hasAccuracy)
            sb.AppendLine();

        if (hasAccuracy)
        {
            sb.AppendLine("<b>ACCURACY</b>");
            sb.AppendLine($"Miss: {ColorValue(missThresholdText, GetLowerThresholdColor(missThreshold, accuracyTierBoundaries.maxValue))} / {ColorValue(accuracyChances.Low.ToString("P0"), GetBadChanceColor(accuracyChances.Low))}");
            sb.AppendLine($"Hit: {ColorValue($"{hitThreshold}+", GetLowerThresholdColor(hitThreshold, accuracyTierBoundaries.maxValue))} / {ColorValue((accuracyChances.Medium + accuracyChances.High).ToString("P0"), GetGoodChanceColor(accuracyChances.Medium + accuracyChances.High))}");
            sb.AppendLine($"Critical: {ColorValue(criticalThreshold > 0 ? $"{criticalThreshold}+" : "--", GetLowerThresholdColor(criticalThreshold, accuracyTierBoundaries.maxValue))} / {ColorValue(accuracyChances.High.ToString("P0"), GetGoodChanceColor(accuracyChances.High))}");
            sb.AppendLine($"Max / Min Roll: {ColorValue(accuracyMaxRollChance.ToString("P0"), GetGoodChanceColor(accuracyMaxRollChance))} / {ColorValue(accuracyMinRollChance.ToString("P0"), GetBadChanceColor(accuracyMinRollChance))}");
        }

        if (hasPower && hasAccuracy)
        {
            var (consistencyLabel, consistencyColorHex) = GetAllocationConsistency(powerChances, accuracyChances);
            sb.AppendLine();
            sb.AppendLine($"Result: <color={consistencyColorHex}>[{consistencyLabel}]</color>");
        }
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
            Dictionary<int, float> groupDistribution = CalculateGroupSumDistribution(group);

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

    /// <summary>
    /// Distribuição de probabilidade da SOMA dos dados de um único grupo (mesmo tipo de stat),
    /// assumindo cada dado uniforme entre 1 e sua face. Reaproveitado tanto para o "melhor resultado"
    /// (CalculateBestRollDistribution) quanto para o alvo de Rolagem Máxima (CalculateMaxRollChance).
    /// </summary>
    private Dictionary<int, float> CalculateGroupSumDistribution(List<int> faces)
    {
        Dictionary<int, float> distribution = new() { { 0, 1f } };
        foreach (int faceCount in faces)
        {
            Dictionary<int, float> nextDistribution = new();
            foreach (var current in distribution)
                for (int face = 1; face <= faceCount; face++)
                {
                    int value = current.Key + face;
                    float chance = current.Value / faceCount;
                    nextDistribution[value] = nextDistribution.TryGetValue(value, out float accumulated)
                        ? accumulated + chance
                        : chance;
                }
            distribution = nextDistribution;
        }
        return distribution;
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

    /// <summary>
    /// Chance de o resultado final (melhor grupo) cair exatamente no piso absoluto da distribuição
    /// combinada. Sem problema de "dado extra": mais dados só tornam o piso mais difícil (correto).
    /// </summary>
    private float CalculateMinRollChance(Dictionary<int, float> distribution)
    {
        (int minimum, _) = GetDistributionBounds(distribution);
        return distribution.TryGetValue(minimum, out float chance) ? chance : 0f;
    }

    /// <summary>
    /// Chance de PELO MENOS UM grupo (stat) atingir seu alvo de Rolagem Máxima — o valor BASE da stat
    /// (mesma referência de DiceService.GetTierReferenceMaxValue), não a soma das faces do grupo.
    /// Com dado extra de perk (ex.: Heart base 12 + 1 extra, ambos d12), o alvo continua 12,
    /// alcançável pela SOMA dos dois dados, não exigindo que os dois tirem 12 ao mesmo tempo.
    /// </summary>
    private float CalculateMaxRollChance(IReadOnlyList<DiceStatType> types, IReadOnlyList<int> faces, IReadOnlyDictionary<DiceStatType, int> statBaseTargets)
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

        float missChance = 1f;
        foreach (var group in groupFaces)
        {
            Dictionary<int, float> groupDistribution = CalculateGroupSumDistribution(group.Value);

            int target = statBaseTargets != null && statBaseTargets.TryGetValue(group.Key, out int statTarget)
                ? statTarget
                : SumFaces(group.Value); // fallback: sem alvo conhecido, usa a soma das faces do próprio grupo

            float hitChance = 0f;
            foreach (var outcome in groupDistribution)
                if (outcome.Key >= target)
                    hitChance += outcome.Value;

            missChance *= 1f - Mathf.Clamp01(hitChance);
        }

        return 1f - missChance;
    }

    private static int SumFaces(List<int> faces)
    {
        int sum = 0;
        for (int i = 0; i < faces.Count; i++)
            sum += faces[i];
        return sum;
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