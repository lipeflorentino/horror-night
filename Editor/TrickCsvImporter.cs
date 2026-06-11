#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importer que carrega o TrickTable.csv e cria TrickSO assets
/// </summary>
public static class TrickCsvImporter
{
    private static readonly string CsvPath = "Assets/Resources/Data/TrickTable.csv";
    private static readonly string OutputFolder = "Assets/Resources/Data/Tricks";

    [MenuItem("Tools/Import Tricks CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogWarning($"[TrickCsvImporter] CSV não encontrado em {CsvPath}");
            return;
        }

        EnsureFolder(OutputFolder);
        string csvText = File.ReadAllText(CsvPath);
        List<TrickSO> parsedTricks = TrickCsvParser.Parse(csvText);
        
        for (int i = 0; i < parsedTricks.Count; i++)
        {
            TrickSO parsed = parsedTricks[i];
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Id))
                continue;

            string assetPath = $"{OutputFolder}/{SanitizeFileName(parsed.Id)}.asset";
            TrickSO asset = AssetDatabase.LoadAssetAtPath<TrickSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TrickSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            Copy(parsed, asset);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TrickCsvImporter] Importados {parsedTricks.Count} tricks.");
    }

    private static void Copy(TrickSO source, TrickSO target)
    {
        target.Id = source.Id;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.Level = source.Level;
        target.MindCost = source.MindCost;
        target.BodyCost = source.BodyCost;
        target.HeartCost = source.HeartCost;
        target.Timing = source.Timing;
        target.DurationTurns = source.DurationTurns;
        target.CooldownTurns = source.CooldownTurns;
        target.PerkIds = new List<string>(source.PerkIds);
        target.Rarity = source.Rarity;
        target.Tags = new List<string>(source.Tags);
        target.FlavorText = source.FlavorText;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value;
    }
}
#endif
