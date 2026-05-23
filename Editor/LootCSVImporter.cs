using UnityEngine;
using UnityEditor;
using System.IO;

public class LootCSVImporter
{
    private static readonly string csvPath = "Assets/Resources/Data/LootTable.csv";
    private static readonly string outputFolder = "Assets/Resources/Data/Items/";

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
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Objects/Itens/" + values[3] + ".png");
            Rarity rarity = (Rarity)System.Enum.Parse(typeof(Rarity), values[4]);
            int weight = int.Parse(values[5]);
            int levelRequirement = int.Parse(values[6]);
            string statBonus = values[7];
            string specialEffect = values[8];
            ItemType itemType = (ItemType)System.Enum.Parse(typeof(ItemType), values[9]);
            
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
            item.levelRequirement = levelRequirement;
            item.statBonus = statBonus;
            item.specialEffect = specialEffect;
            item.type = itemType;
            item.icon = sprite;

            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Importação concluída!");
    }
}