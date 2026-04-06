using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EnemyCSVImporter
{
    private const string CsvPath = "Assets/Resources/Data/EnemyTable.csv";
    private const string OutputFolder = "Assets/Resources/Data/Enemies/";

    [MenuItem("Tools/Import Enemy CSV")]
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
            if (values.Length < 15)
                continue;

            int id = ParseInt(values[0]);
            if (id <= 0)
                continue;

            EnemyArchetype archetype = ParseArchetype(values[3]);
            string assetPath = OutputFolder + id + ".asset";

            EnemySO enemy = LoadOrCreateEnemy(assetPath, archetype);
            
            if (enemy == null)
                continue;

            enemy.id = id;
            enemy.enemyName = values[1];
            enemy.description = values[2];
            enemy.archetype = archetype;
            enemy.weight = Mathf.Max(1, ParseInt(values[4]));
            enemy.tierMin = Mathf.Max(0, ParseInt(values[5]));
            enemy.tierMax = Mathf.Max(enemy.tierMin, ParseInt(values[6]));
            enemy.tags.behavior = values[7];
            enemy.tags.style = values[8];
            enemy.tags.type = values[9];
            enemy.heart = BuildRange(values[10], values[11]);
            enemy.body = BuildRange(values[12], values[13]);
            enemy.mind = BuildRange(values[14], values.Length > 15 ? values[15] : values[14]);
            enemy.specialRule = values.Length > 16 ? values[16] : string.Empty;
            enemy.hp = BuildRange(values[18], values[19]);

            if (values.Length > 19)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(values[17]);
                enemy.image = sprite;
            }

            EditorUtility.SetDirty(enemy);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Importação de inimigos concluída!");
    }

    private static EnemySO LoadOrCreateEnemy(string assetPath, EnemyArchetype archetype)
    {
        EnemySO loaded = AssetDatabase.LoadAssetAtPath<EnemySO>(assetPath);
        if (loaded != null)
            return loaded;

        EnemySO enemy = ScriptableObject.CreateInstance<EnemySO>();

        AssetDatabase.CreateAsset(enemy, assetPath);

        return enemy;
    }

    private static StatRange BuildRange(string minValue, string maxValue)
    {
        int min = Mathf.Max(0, ParseInt(minValue));
        int max = Mathf.Max(min, ParseInt(maxValue));
        return new StatRange { min = min, max = max };
    }

    private static EnemyArchetype ParseArchetype(string value)
    {
        if (Enum.TryParse(value, true, out EnemyArchetype parsed))
            return parsed;

        return EnemyArchetype.Normal;
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
}
