using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public string itemName;
    public Rarity rarity;
    [Min(0)]
    public int weight = 1;
    public StatModifier effects;
}