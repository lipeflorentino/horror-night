using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct PlayerStatusSnapshot
{
    [SerializeField] public string characterId;
    [SerializeField] public string characterName;
    [SerializeField] public Sprite characterIcon;
    [SerializeField] public float heart;
    [SerializeField] public float body;
    [SerializeField] public float mind;
    [SerializeField] public float attack;
    [SerializeField] public float defense;
    [SerializeField] public float initiative;
    [SerializeField] public float focus;
    [SerializeField] public float strength;
    [SerializeField] public float agility;
    [SerializeField] public int level;
    [SerializeField] public int currentXp;
    [SerializeField] public int maxXp;
    [SerializeField] public float hp;
    [FormerlySerializedAs("powerDices")]
    [SerializeField] public int currentDices;
    [SerializeField] public float maxHeart;
    [SerializeField] public float maxBody;
    [SerializeField] public float maxMind;
    [SerializeField] public float maxHp;
    [FormerlySerializedAs("maxPowerDices")]
    [SerializeField] public int maxDices;
    [SerializeField] public PlayerArchetype currentArchetype;
    [SerializeField] public ArchetypePoints archetypePoints;
    [SerializeField] public PlayerInventorySnapshot inventory;
    [SerializeField] public TrickInventorySnapshot trickInventory;
}
