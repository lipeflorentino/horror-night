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

    public void UpdateSelectionPreview(
        IReadOnlyList<DiceStatType> powerDiceTypes,
        IReadOnlyList<int> powerFaces,
        IReadOnlyList<DiceStatType> accuracyDiceTypes,
        IReadOnlyList<int> accuracyFaces,
        (int lowMax, int mediumMax, int highMin) tierBoundaries)
    {
        RebuildAllocationContainer(powerDiceContainer, powerDiceTypes, powerFaces);
        RebuildAllocationContainer(accuracyDiceContainer, accuracyDiceTypes, accuracyFaces);
        UpdateDiceTiersLabel(tierBoundaries);
        UpdateResultPanel(powerFaces, accuracyFaces);
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

    private void UpdateDiceTiersLabel((int lowMax, int mediumMax, int highMin) tierBoundaries)
    {
        if (diceTiersText == null)
            return;

        string lowRange = tierBoundaries.lowMax > 0 ? $"1-{tierBoundaries.lowMax}" : "-";
        string mediumRange = tierBoundaries.mediumMax > tierBoundaries.lowMax
            ? $"{tierBoundaries.lowMax + 1}-{tierBoundaries.mediumMax}"
            : "-";
        string highRange = tierBoundaries.highMin <= 6 ? $"{tierBoundaries.highMin}+" : "-";

        diceTiersText.text = $"Low ({lowRange}), Medium ({mediumRange}), High ({highRange})";
    }

    private void UpdateResultPanel(IReadOnlyList<int> powerFaces, IReadOnlyList<int> accuracyFaces)
    {
        if (resultPanelText == null)
            return;

        int minPower = SumMin(powerFaces);
        int maxPower = SumMax(powerFaces);
        int minAccuracy = SumMin(accuracyFaces);
        int maxAccuracy = SumMax(accuracyFaces);

        int hitThreshold = minAccuracy <= 2 ? 3 : minAccuracy;
        int criticalThreshold = minPower <= 4 ? 5 : minPower;

        StringBuilder sb = new();
        sb.AppendLine($"Damage: {minPower}-{maxPower}");
        sb.AppendLine($"Hit Threshold: {hitThreshold}+");
        sb.AppendLine($"Critical Threshold: {criticalThreshold}+");
        sb.Append("Effects: Critical Hit, Power Max (placeholder), Accuracy Max (evade/parry)");
        resultPanelText.text = sb.ToString();
    }

    private int SumMin(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0) return 0;
        return faces.Count;
    }

    private int SumMax(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0) return 0;
        int total = 0;
        for (int i = 0; i < faces.Count; i++)
            total += Mathf.Max(1, faces[i]);
        return total;
    }
}