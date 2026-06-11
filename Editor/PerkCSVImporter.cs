#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PerkCSVImporter
{
    private static readonly string CsvPath = $"Assets/Resources/{PerkDatabase.PerkTableResourcePath}.csv";
    private static readonly string OutputFolder = $"Assets/Resources/{PerkDatabase.PerkResourceFolder}";

    [MenuItem("Tools/Import Perks CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogWarning($"[PerkCSVImporter] CSV not found at {CsvPath}");
            return;
        }

        EnsureFolder(OutputFolder);
        string csvText = File.ReadAllText(CsvPath);
        List<PerkSO> parsedPerks = PerkCsvParser.Parse(csvText);
        for (int i = 0; i < parsedPerks.Count; i++)
        {
            PerkSO parsed = parsedPerks[i];
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Id))
                continue;

            string assetPath = $"{OutputFolder}/{SanitizeFileName(parsed.Id)}.asset";
            PerkSO asset = AssetDatabase.LoadAssetAtPath<PerkSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PerkSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            Copy(parsed, asset);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PerkCSVImporter] Imported {parsedPerks.Count} perks.");
    }

    private static void Copy(PerkSO source, PerkSO target)
    {
        target.Id = source.Id;
        target.IsPermanentIdentity = source.IsPermanentIdentity;
        target.DefaultDurationTurns = source.DefaultDurationTurns;
        target.MaxStacks = source.MaxStacks;
        target.StackMode = source.StackMode;
        target.Tags = source.Tags;
        target.Rules = new List<PerkRule>(source.Rules);
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
