using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    public int id;
    public string itemName;
    [TextArea] public string description;
    public int weight;
    public Rarity rarity;
    public Sprite icon;
}