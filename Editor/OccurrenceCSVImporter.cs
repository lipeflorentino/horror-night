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
        string csvPath = ResolveCsvPath();

        if (string.IsNullOrEmpty(csvPath))
        {
            Debug.LogError($"CSV não encontrado em: {CsvPath}");
            return;
        }

        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        string[] lines = File.ReadAllLines(csvPath);
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
                if (line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
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
            occurrenceEntry.profileOption1 = values[3];
            occurrenceEntry.profileOption2 = values[4];
            occurrenceEntry.neutralOption = values[5];
            occurrenceEntry.profile1Type = ParseArchetype(values[6]);
            occurrenceEntry.profile2Type = ParseArchetype(values[7]);
            occurrenceEntry.primaryStat = NormalizeStatName(values[8]);
            occurrenceEntry.requiresRoll = ParseBool(values[9]);
            occurrenceEntry.tier = Mathf.Max(0, ParseInt(values[10]));

            EditorUtility.SetDirty(occurrenceEntry);
            importedEntries.Add(occurrenceEntry);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Importação de ocorrencias concluída! Itens: {importedEntries.Count}");
    }

    private static string ResolveCsvPath()
    {
        if (File.Exists(CsvPath))
            return CsvPath;

        return null;
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

    private static bool ParseBool(string value)
    {
        if (bool.TryParse(value.Trim(), out bool parsed))
            return parsed;

        string normalized = value.Trim().ToLowerInvariant();
        return normalized == "1" || normalized == "sim" || normalized == "yes";
    }

    private static PlayerArchetype ParseArchetype(string archetype)
    {
        string normalized = archetype.Trim().ToUpperInvariant();

        return normalized switch
        {
            "NF" => PlayerArchetype.NF,
            "SJ" => PlayerArchetype.SJ,
            "SP" => PlayerArchetype.SP,
            _ => PlayerArchetype.NT
        };
    }

    private static string NormalizeStatName(string stat)
    {
        string normalized = stat.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "vida":
            case "heart":
            case "coracao":
            case "coração":
                return "heart";
            case "físico":
            case "fisico":
            case "força":
            case "forca":
            case "strength":
            case "physical":
            case "corpo":
            case "body":
                return "body";
            case "mental":
            case "sanidade":
            case "sanity":
            case "mente":
            case "mind":
                return "mind";
            default:
                return "heart";
        }
    }
}
