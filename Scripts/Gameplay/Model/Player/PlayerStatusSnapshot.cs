using UnityEngine;

[System.Serializable]
public struct PlayerStatusSnapshot
{
    [SerializeField] public float heart;
    [SerializeField] public float body;
    [SerializeField] public float mind;
    [SerializeField] public float maxHeart;
    [SerializeField] public float maxBody;
    [SerializeField] public float maxMind;
    [SerializeField] public PlayerArchetype currentArchetype;
    [SerializeField] public ArchetypePoints archetypePoints;
}