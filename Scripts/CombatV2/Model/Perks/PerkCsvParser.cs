using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PerkCsvParser
{
    public static List<PerkSO> Parse(string csvText)
    {
        List<PerkSO> perks = new();
        List<List<string>> rows = ParseRows(csvText);
        if (rows.Count <= 1)
            return perks;

        Dictionary<string, int> columns = BuildColumnMap(rows[0]);
        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            string id = Get(row, columns, "Id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            PerkSO perk = ScriptableObject.CreateInstance<PerkSO>();
            
            perk.Id = id;
            perk.IsPermanentIdentity = ParseBool(Get(row, columns, "IsPermanentIdentity"));
            perk.DefaultDurationTurns = ParseInt(Get(row, columns, "DurationTurns"), -1);
            perk.MaxStacks = Math.Max(1, ParseInt(Get(row, columns, "MaxStacks"), 1));
            perk.StackMode = ParseEnum(Get(row, columns, "StackMode"), BattlerStateStackMode.RefreshDuration);
            perk.Tags = Get(row, columns, "Tags");
            perk.Rules.Add(ParseRule(row, columns));
            perks.Add(perk);
        }

        return perks;
    }

    private static PerkRule ParseRule(List<string> row, Dictionary<string, int> columns)
    {
        string actionFilter = Get(row, columns, "ActionFilter");
        string rollFilter = Get(row, columns, "RollFilter");
        string statFilter = Get(row, columns, "StatFilter");
        string tierFilter = Get(row, columns, "TierFilter");
        PerkModifierTarget modifierTarget = ParseEnum(Get(row, columns, "ModifierTarget"), PerkModifierTarget.ExtraDice);
        bool filterByTier = HasFilter(tierFilter);

        return new PerkRule
        {
            Trigger = InferTrigger(modifierTarget, filterByTier),
            ModifierTarget = modifierTarget,
            Operation = ParseEnum(Get(row, columns, "Operation"), PerkOperation.Add),
            OwnerRole = ParseEnum(Get(row, columns, "OwnerRole"), BattlerStateRole.OwnerAsActor),
            ActionType = ParseEnum(actionFilter, ActionType.Attack),
            FilterByActionType = HasFilter(actionFilter),
            RollType = ParseEnum(rollFilter, DiceRollType.Power),
            FilterByRollType = HasFilter(rollFilter),
            StatType = ParseEnum(statFilter, DiceStatType.Body),
            FilterByStatType = HasFilter(statFilter),
            Tier = ParseEnum(tierFilter, DiceTier.Low),
            FilterByTier = filterByTier,
            ConditionKey = ParseEnum(Get(row, columns, "ConditionKey"), PerkConditionKey.Always),
            ConditionValue = Get(row, columns, "ConditionValue"),
            Value = ParseFloat(Get(row, columns, "Value"), 0f)
        };
    }

    private static PerkTrigger InferTrigger(PerkModifierTarget target, bool filterByTier = false)
    {
        if (target == PerkModifierTarget.PowerMultiplier)
            return PerkTrigger.PowerMultiplier;

        if (target == PerkModifierTarget.DamagePercent || target == PerkModifierTarget.MomentumPoints)
            return PerkTrigger.AfterResolve;

        if (target == PerkModifierTarget.Strength || target == PerkModifierTarget.Focus)
            return PerkTrigger.OnActionResolved;

        if (target == PerkModifierTarget.ExtraDice && filterByTier)
            return PerkTrigger.AfterAccuracyRoll;

        return PerkTrigger.BeforeRoll;
    }

    private static bool HasFilter(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Equals("Any", StringComparison.OrdinalIgnoreCase) && !value.Equals("None", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildColumnMap(List<string> headers)
    {
        Dictionary<string, int> columns = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(header) && !columns.ContainsKey(header))
                columns.Add(header, i);
        }

        return columns;
    }

    private static string Get(List<string> row, Dictionary<string, int> columns, string key)
    {
        if (!columns.TryGetValue(key, out int index) || index < 0 || index >= row.Count)
            return string.Empty;

        return row[index]?.Trim() ?? string.Empty;
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Any", StringComparison.OrdinalIgnoreCase) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            return fallback;

        return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
    }

    private static bool ParseBool(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static List<List<string>> ParseRows(string csvText)
    {
        List<List<string>> rows = new();
        if (string.IsNullOrWhiteSpace(csvText))
            return rows;

        List<string> currentRow = new();
        StringBuilder currentValue = new();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    i++;

                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                AddRowIfNotEmpty(rows, currentRow);
                currentRow = new List<string>();
                continue;
            }

            currentValue.Append(c);
        }

        currentRow.Add(currentValue.ToString());
        AddRowIfNotEmpty(rows, currentRow);
        return rows;
    }

    private static void AddRowIfNotEmpty(List<List<string>> rows, List<string> row)
    {
        for (int i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
            {
                rows.Add(row);
                return;
            }
        }
    }
}
