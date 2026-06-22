using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Parser para carregar Drawbacks do CSV e gerar DrawbackSO
/// </summary>
public static class DrawbackCsvParser
{
    public static List<DrawbackSO> Parse(string csvText)
    {
        List<DrawbackSO> drawbacks = new();
        List<List<string>> rows = ParseRows(csvText);
        if (rows.Count <= 1)
            return drawbacks;

        Dictionary<string, int> columns = BuildColumnMap(rows[0]);
        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            string id = Get(row, columns, "Id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            DrawbackSO drawback = ScriptableObject.CreateInstance<DrawbackSO>();
            
            string iconName = Get(row, columns, "IconName");
            Sprite sprite = null;
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Drawbacks/" + iconName + ".png");
            }
#endif
            
            drawback.Id = id;
            drawback.DisplayName = Get(row, columns, "DisplayName");
            drawback.Description = Get(row, columns, "Description");
            drawback.Icon = sprite;
            drawback.DurationTurns = ParseInt(Get(row, columns, "DurationTurns"), -1);
            drawback.PerkIds = ParseStringList(Get(row, columns, "PerkIds"), ";");
            drawback.FlavorText = Get(row, columns, "FlavorText");
            
            drawbacks.Add(drawback);
        }

        return drawbacks;
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
