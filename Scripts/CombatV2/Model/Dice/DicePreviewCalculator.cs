using System.Collections.Generic;
using UnityEngine;

public static class DicePreviewCalculator
{
    public static (int maxValue, DiceStatType primaryStat) GetPreviewMaxValueAndPrimaryStat(
        IReadOnlyList<DiceStatType> diceTypes, 
        IReadOnlyList<int> faces,
        DiceRollType rollType
        )
    {
        if (diceTypes == null || faces == null || diceTypes.Count == 0 || faces.Count == 0)
            return (1, DiceStatType.Body);

        Dictionary<DiceStatType, int> maxValueByStat = new();
        int itemCount = Mathf.Min(diceTypes.Count, faces.Count);

        for (int i = 0; i < itemCount; i++)
        {
            DiceStatType statType = diceTypes[i];
            int faceValue = Mathf.Max(1, faces[i]);
            maxValueByStat[statType] = maxValueByStat.TryGetValue(statType, out int currentValue)
                ? currentValue + faceValue
                : faceValue;
        }

        int selectedMaxValue = 1;
        DiceStatType selectedStat = DiceStatType.Body;

        foreach (KeyValuePair<DiceStatType, int> pair in maxValueByStat)
        {
            if (pair.Value > selectedMaxValue || 
               (pair.Value == selectedMaxValue && CombatRules.GetStatPriority(pair.Key, rollType) > CombatRules.GetStatPriority(selectedStat, rollType)))
            {
                selectedMaxValue = pair.Value;
                selectedStat = pair.Key;
            }
        }

        return (Mathf.Max(1, selectedMaxValue), selectedStat);
    }
}