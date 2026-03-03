using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EventCSVImporter
{
    private const string CsvPath = "Assets/Resources/Data/EventsTable.csv";
    private const string OutputFolder = "Assets/Resources/Data/Events/";
    private const string DatabasePath = "Assets/Resources/Data/EventDatabase.asset";

    [MenuItem("Tools/Import Events CSV")]
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
        List<EventEntrySO> importedEvents = new List<EventEntrySO>();

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
            EventEntrySO eventEntry = AssetDatabase.LoadAssetAtPath<EventEntrySO>(assetPath);

            if (eventEntry == null)
            {
                eventEntry = ScriptableObject.CreateInstance<EventEntrySO>();
                AssetDatabase.CreateAsset(eventEntry, assetPath);
            }

            eventEntry.id = id;
            eventEntry.title = values[1];
            eventEntry.description = values[2];
            eventEntry.successText = values[3];
            eventEntry.failText = values[4];
            eventEntry.successStat = NormalizeStatName(values[5]);
            eventEntry.successValue = ParseInt(values[6]);
            eventEntry.failStat = NormalizeStatName(values[7]);
            eventEntry.failValue = ParseInt(values[8]);
            eventEntry.rollRange = Mathf.Max(0, ParseInt(values[9]));

            EditorUtility.SetDirty(eventEntry);
            importedEvents.Add(eventEntry);
        }

        EventDatabase database = AssetDatabase.LoadAssetAtPath<EventDatabase>(DatabasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<EventDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.allEvents = importedEvents;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Importação de eventos concluída! Total: {importedEvents.Count}");
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
                return "life";
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
                return "sanity";
            case "poder":
            case "power":
                return "power";
            default:
                return "life";
        }
    }
}
