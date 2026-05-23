using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteExporter : MonoBehaviour
{
    [MenuItem("Tools/Export Sprites")]
    private static void ExportSprites()
    {
        Texture2D texture = Selection.activeObject as Texture2D;

        if (texture == null)
        {
            EditorUtility.DisplayDialog("Erro", "Por favor, selecione uma textura (Sprite Sheet) no Project view.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        int exportedCount = 0;

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                int x = Mathf.RoundToInt(sprite.rect.x);
                int y = Mathf.RoundToInt(sprite.rect.y);
                int width = Mathf.RoundToInt(sprite.rect.width);
                int height = Mathf.RoundToInt(sprite.rect.height);

                Texture2D newTex = new(width, height);
                
                Color[] pixels = texture.GetPixels(x, y, width, height);
                
                newTex.SetPixels(pixels);
                newTex.Apply();

                byte[] bytes = newTex.EncodeToPNG();
                
                string directory = Path.GetDirectoryName(path);
                string savePath = Path.Combine(directory, sprite.name + ".png");

                File.WriteAllBytes(savePath, bytes);
                exportedCount++;

                DestroyImmediate(newTex);
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Sucesso!", $"{exportedCount} sprites foram exportados com sucesso!", "Boa!");
    }
}