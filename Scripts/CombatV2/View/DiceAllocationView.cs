using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DiceAllocationView : MonoBehaviour
{
    [Header("Selection Preview")]
    [SerializeField] private RectTransform powerDiceContainer;
    [SerializeField] private RectTransform accuracyDiceContainer;
    [SerializeField] private DiceAllocationItemUI allocationItemPrefab;
    [SerializeField] private Sprite mindDiceIcon;
    [SerializeField] private Sprite heartDiceIcon;
    [SerializeField] private Sprite bodyDiceIcon;
    [SerializeField] private TMP_Text diceTiersText;
    [SerializeField] private TMP_Text resultPanelText;
    
    [Header("Stat Values")]
    [SerializeField] private TMP_Text mindPowerValueText; 
    [SerializeField] private TMP_Text mindAccuracyValueText;
    [SerializeField] private TMP_Text heartPowerValueText; 
    [SerializeField] private TMP_Text heartAccuracyValueText;
    [SerializeField] private TMP_Text bodyPowerValueText; 
    [SerializeField] private TMP_Text bodyAccuracyValueText;

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

    public void UpdateStatValueTexts(int mind, int heart, int body)
    {
        if (mindPowerValueText != null)
            mindPowerValueText.text = $"{mind}";

        if (mindAccuracyValueText != null)
            mindAccuracyValueText.text = $"{mind}";

        if (heartPowerValueText != null)
            heartPowerValueText.text = $"{heart}";

        if (heartAccuracyValueText != null)
            heartAccuracyValueText.text = $"{heart}";

        if (bodyPowerValueText != null)
            bodyPowerValueText.text = $"{body}";

        if (bodyAccuracyValueText != null)
            bodyAccuracyValueText.text = $"{body}";
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
            item.Bind(GetIcon(types[i]), faces[i]);
        }
    }

    private Sprite GetIcon(DiceStatType type)
    {
        return type switch
        {
            DiceStatType.Mind => mindDiceIcon,
            DiceStatType.Heart => heartDiceIcon,
            DiceStatType.Body => bodyDiceIcon,
            _ => null
        };
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
        return 1;
    }

    private int SumMax(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0) return 0;
        int maxValue = 0;
        for (int i = 0; i < faces.Count; i++)
            maxValue = Mathf.Max(maxValue, Mathf.Max(1, faces[i]));
        return maxValue;
    }
}