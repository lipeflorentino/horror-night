#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BattlerStateCsvImporter
{
    private static readonly string CsvPath = "Assets/Resources/Data/BattlerStatesTable.csv";
    private static readonly string OutputFolder = "Assets/Resources/Data/BattlerStates";

    [MenuItem("Tools/Import Battler States CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogWarning($"[BattlerStateCsvImporter] CSV não encontrado em {CsvPath}");
            return;
        }

        EnsureFolder(OutputFolder);
        string csvText = File.ReadAllText(CsvPath);
        List<BattlerStateSO> parsedStates = BattlerStateCsvParser.Parse(csvText);

        for (int i = 0; i < parsedStates.Count; i++)
        {
            BattlerStateSO parsed = parsedStates[i];
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Id))
                continue;

            string assetPath = $"{OutputFolder}/{SanitizeFileName(parsed.Id)}.asset";
            BattlerStateSO asset = AssetDatabase.LoadAssetAtPath<BattlerStateSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BattlerStateSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            Copy(parsed, asset);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattlerStateCsvImporter] Importados {parsedStates.Count} states.");
    }

    private static void Copy(BattlerStateSO source, BattlerStateSO target)
    {
        target.Id = source.Id;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.DefaultDurationTurns = source.DefaultDurationTurns;
        target.MaxStacks = source.MaxStacks;
        target.StackMode = source.StackMode;
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
