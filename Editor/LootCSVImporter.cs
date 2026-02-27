using UnityEngine;
using UnityEditor;
using System.IO;

public class LootCSVImporter
{
    private static string csvPath = "Assets/Resources/Data/LootTable.csv";
    private static string outputFolder = "Assets/Resources/Data/Items/";

    [MenuItem("Tools/Import Loot CSV")]
    public static void ImportCSV()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError("CSV não encontrado em: " + csvPath);
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string[] lines = File.ReadAllLines(csvPath);

        bool skipHeader = true;

        foreach (string line in lines)
        {
            if (skipHeader)
            {
                skipHeader = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values = line.Split(',');

            int id = int.Parse(values[0]);
            string name = values[1];
            string description = values[2];
            Rarity rarity = (Rarity)System.Enum.Parse(typeof(Rarity), values[3]);
            int weight = int.Parse(values[4]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/" + id + ".png");
            
            string assetPath = outputFolder + id + ".asset";

            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(assetPath);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemSO>();
                AssetDatabase.CreateAsset(item, assetPath);
            }

            item.id = id;
            item.itemName = name;
            item.description = description;
            item.rarity = rarity;
            item.weight = weight;
            item.icon = sprite;

            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Importação concluída!");
    }
}