using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class OccurrenceCSVImporter
{
    private const string CsvPath = "Assets/Resources/Data/OccurrenceTable.csv";
    private const string OutputFolder = "Assets/Resources/Data/Occurrences/";

    [MenuItem("Tools/Import Occurrence CSV")]
    public static void ImportCSV()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError("CSV não encontrado em: " + CsvPath);
            return;
        }

        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        string[] lines = File.ReadAllLines(CsvPath);
        List<OccurrenceSO> importedEntries = new();

        bool skippedHeader = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("|"))
                continue;

            if (!skippedHeader)
            {
                skippedHeader = true;
                if (line.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            string[] values = SplitCsvLine(line);

            if (values.Length < 10)
                continue;

            int id = ParseInt(values[0]);
            if (id <= 0)
                continue;

            string assetPath = OutputFolder + id + ".asset";
            OccurrenceSO occurrenceEntry = AssetDatabase.LoadAssetAtPath<OccurrenceSO>(assetPath);

            if (occurrenceEntry == null)
            {
                occurrenceEntry = ScriptableObject.CreateInstance<OccurrenceSO>();
                AssetDatabase.CreateAsset(occurrenceEntry, assetPath);
            }

            occurrenceEntry.id = id;
            occurrenceEntry.title = values[1];
            occurrenceEntry.description = values[2];
            occurrenceEntry.successText = values[3];
            occurrenceEntry.failText = values[4];
            occurrenceEntry.successStat = NormalizeStatName(values[5]);
            occurrenceEntry.successValue = ParseInt(values[6]);
            occurrenceEntry.failStat = NormalizeStatName(values[7]);
            occurrenceEntry.failValue = ParseInt(values[8]);
            occurrenceEntry.rollRange = Mathf.Max(0, ParseInt(values[9]));

            EditorUtility.SetDirty(occurrenceEntry);
            importedEntries.Add(occurrenceEntry);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Importação de ocorrencias concluída!");
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> values = new List<string>();
        bool insideQuotes = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
                insideQuotes = !insideQuotes;

            if (line[i] == ',' && !insideQuotes)
            {
                values.Add(TrimCsvValue(line.Substring(start, i - start)));
                start = i + 1;
            }
        }

        values.Add(TrimCsvValue(line.Substring(start)));
        return values.ToArray();
    }

    private static string TrimCsvValue(string value)
    {
        string trimmed = value.Trim();

        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
            trimmed = trimmed.Substring(1, trimmed.Length - 2);

        return trimmed;
    }

    private static int ParseInt(string value)
    {
        int.TryParse(value.Trim(), out int parsed);
        return parsed;
    }

    private static string NormalizeStatName(string stat)
    {
        string normalized = stat.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "vida":
            case "life":
                return "heart";
            case "físico":
            case "fisico":
            case "força":
            case "forca":
            case "strength":
            case "physical":
                return "physical";
            case "mental":
            case "sanidade":
            case "sanity":
                return "mind";
            case "poder":
            case "power":
                return "power";
            default:
                return "heart";
        }
    }
}
