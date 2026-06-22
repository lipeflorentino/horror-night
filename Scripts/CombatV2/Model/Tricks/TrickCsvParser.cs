using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Parser para carregar Tricks do CSV e gerar TrickSO
/// </summary>
public static class TrickCsvParser
{
    public static List<TrickSO> Parse(string csvText)
    {
        List<TrickSO> tricks = new();
        List<List<string>> rows = ParseRows(csvText);
        if (rows.Count <= 1)
            return tricks;

        Dictionary<string, int> columns = BuildColumnMap(rows[0]);
        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            string id = Get(row, columns, "Id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            TrickSO trick = ScriptableObject.CreateInstance<TrickSO>();
            
            string iconName = Get(row, columns, "IconName");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Tricks/" + iconName + ".png");
            
            trick.Id = id;
            trick.DisplayName = Get(row, columns, "DisplayName");
            trick.Description = Get(row, columns, "Description");
            trick.Icon = sprite;
            trick.Level = ParseInt(Get(row, columns, "Level"), 1);
            trick.MindCost = ParseInt(Get(row, columns, "MindCost"), 0);
            trick.BodyCost = ParseInt(Get(row, columns, "BodyCost"), 0);
            trick.HeartCost = ParseInt(Get(row, columns, "HeartCost"), 0);
            trick.TimingTurns = ParseInt(GetFirst(row, columns, "TimingTurns", "Timing"), 0);
            trick.DurationTurns = ParseInt(Get(row, columns, "DurationTurns"), -1);
            trick.CooldownTurns = ParseInt(Get(row, columns, "CooldownTurns"), 0);
            trick.PerkIds = ParseStringList(Get(row, columns, "PerkIds"), ";");
            trick.DrawbackPerkId = Get(row, columns, "DrawbackPerkId");
            trick.ActivationMode = ParseEnum(Get(row, columns, "ActivationMode"), TrickActivationMode.Active);
            trick.Rarity = ParseEnum(Get(row, columns, "Rarity"), TrickRarity.Common);
            trick.Tags = ParseStringList(Get(row, columns, "Tags"), ";");
            trick.FlavorText = Get(row, columns, "FlavorText");
            
            tricks.Add(trick);
        }

        return tricks;
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

    private static string GetFirst(List<string> row, Dictionary<string, int> columns, params string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            string value = Get(row, columns, keys[i]);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
    }

    private static List<string> ParseStringList(string value, string separator = ";")
    {
        List<string> result = new();
        
        if (string.IsNullOrWhiteSpace(value))
            return result;
        
        string[] parts = value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string trimmed = parts[i].Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                result.Add(trimmed);
        }
        
        return result;
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
