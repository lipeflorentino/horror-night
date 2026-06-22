#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importer que carrega o DrawbackTable.csv e cria DrawbackSO assets
/// </summary>
public static class DrawbackCsvImporter
{
    private static readonly string CsvPath = "Assets/Resources/Data/DrawbackTable.csv";
    private static readonly string OutputFolder = "Assets/Resources/Data/Drawbacks";

    [MenuItem("Tools/Import Drawbacks CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogWarning($"[DrawbackCsvImporter] CSV não encontrado em {CsvPath}");
            return;
        }

        EnsureFolder(OutputFolder);
        string csvText = File.ReadAllText(CsvPath);
        List<DrawbackSO> parsedDrawbacks = DrawbackCsvParser.Parse(csvText);
        
        for (int i = 0; i < parsedDrawbacks.Count; i++)
        {
            DrawbackSO parsed = parsedDrawbacks[i];
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Id))
                continue;

            string assetPath = $"{OutputFolder}/{SanitizeFileName(parsed.Id)}.asset";
            DrawbackSO asset = AssetDatabase.LoadAssetAtPath<DrawbackSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DrawbackSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            Copy(parsed, asset);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DrawbackCsvImporter] Importados {parsedDrawbacks.Count} drawbacks.");
    }

    private static void Copy(DrawbackSO source, DrawbackSO target)
    {
        target.Id = source.Id;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.DurationTurns = source.DurationTurns;
        target.PerkIds = new List<string>(source.PerkIds);
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
