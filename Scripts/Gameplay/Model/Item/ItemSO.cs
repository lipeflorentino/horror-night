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
    public int levelRequirement;
    public string statBonus = "none";
    public string specialEffect = "none";
    public ItemType type = ItemType.Consumable;
    public int durability = -1;
}
