using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CharacterCSVImporter
{
    private const string CsvPath = "Assets/Resources/Data/CharacterTable.csv";
    private const string OutputFolder = "Assets/Resources/Data/Characters";
    private const string TrickFolder = "Assets/Resources/Data/Tricks";

    [MenuItem("Tools/Import Character CSV")]
    public static void ImportCSV()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError("CSV não encontrado em: " + CsvPath);
            return;
        }

        EnsureFolder(OutputFolder);

        string[] lines = File.ReadAllLines(CsvPath);
        bool skippedHeader = false;
        int importedCount = 0;

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
            if (values.Length < 17)
            {
                Debug.LogWarning($"[CharacterCSVImporter] Linha ignorada. Esperados 17 campos, encontrados {values.Length}: {line}");
                continue;
            }

            string id = values[0];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string assetPath = $"{OutputFolder}/{SanitizeFileName(id)}.asset";
            CharacterSO character = LoadOrCreateCharacter(assetPath);
            if (character == null)
                continue;

            character.Id = id;
            character.DisplayName = values[1];
            character.Description = values[2];
            character.Xp = Mathf.Max(0, ParseInt(values[3]));
            character.Heart = Mathf.Max(1f, ParseFloat(values[4]));
            character.Body = Mathf.Max(1f, ParseFloat(values[5]));
            character.Mind = Mathf.Max(1f, ParseFloat(values[6]));
            character.Hp = Mathf.Max(1f, ParseFloat(values[7]));
            character.Attack = Mathf.Max(0, ParseInt(values[8]));
            character.Defense = Mathf.Max(0, ParseInt(values[9]));
            character.Initiative = Mathf.Max(0, ParseInt(values[10]));
            character.Focus = Mathf.Max(0, ParseInt(values[11]));
            character.Strength = Mathf.Max(0, ParseInt(values[12]));
            character.Agility = Mathf.Max(0, ParseInt(values[13]));
            character.PowerDices = Mathf.Max(1, ParseInt(values[14]));
            character.AccuracyDices = Mathf.Max(1, ParseInt(values[15]));
            character.IdentityTricks = LoadTrickList(values[16]);

            EditorUtility.SetDirty(character);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CharacterCSVImporter] Importados {importedCount} personagens.");
    }

    private static CharacterSO LoadOrCreateCharacter(string assetPath)
    {
        CharacterSO loaded = AssetDatabase.LoadAssetAtPath<CharacterSO>(assetPath);
        if (loaded != null)
            return loaded;

        CharacterSO character = ScriptableObject.CreateInstance<CharacterSO>();
        AssetDatabase.CreateAsset(character, assetPath);
        return character;
    }

    private static List<TrickSO> LoadTrickList(string rawIds)
    {
        List<TrickSO> tricks = new();
        if (string.IsNullOrWhiteSpace(rawIds))
            return tricks;

        string[] ids = rawIds.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i].Trim();
            TrickSO trick = LoadTrickById(id);
            if (trick != null && !tricks.Contains(trick))
                tricks.Add(trick);
        }

        return tricks;
    }

    private static TrickSO LoadTrickById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string[] trickGuids = AssetDatabase.FindAssets("t:TrickSO", new[] { TrickFolder });
        for (int i = 0; i < trickGuids.Length; i++)
        {
            TrickSO trick = AssetDatabase.LoadAssetAtPath<TrickSO>(AssetDatabase.GUIDToAssetPath(trickGuids[i]));
            if (trick != null && string.Equals(trick.Id, id, StringComparison.OrdinalIgnoreCase))
                return trick;
        }

        Debug.LogWarning($"[CharacterCSVImporter] Trick não encontrado: {id}");
        return null;
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> values = new();
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

        return trimmed.Replace("\"\"", "\"");
    }

    private static int ParseInt(string value)
    {
        int.TryParse(value.Trim(), out int parsed);
        return parsed;
    }

    private static float ParseFloat(string value)
    {
        float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed);
        return parsed;
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
