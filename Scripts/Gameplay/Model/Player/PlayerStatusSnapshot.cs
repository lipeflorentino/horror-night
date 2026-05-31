using UnityEngine;

[System.Serializable]
public struct PlayerStatusSnapshot
{
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
    [SerializeField] public int powerDices;
    [SerializeField] public int accuracyDices;
    [SerializeField] public float maxHeart;
    [SerializeField] public float maxBody;
    [SerializeField] public float maxMind;
    [SerializeField] public float maxHp;
    [SerializeField] public int maxPowerDices;
    [SerializeField] public int maxAccuracyDices;
    [SerializeField] public PlayerArchetype currentArchetype;
    [SerializeField] public ArchetypePoints archetypePoints;
    [SerializeField] public PlayerInventorySnapshot inventory;
}
