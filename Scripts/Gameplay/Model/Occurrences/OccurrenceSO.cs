using UnityEngine;

public enum PlayerArchetype
{
    NT,
    NF,
    SJ,
    SP
}

[CreateAssetMenu(menuName = "Game/Occurrence")]
public class OccurrenceSO : ScriptableObject
{
    public int id;
    public string title;
    [TextArea] public string description;

    [Header("Choices")]
    public string profileOption1;
    public string profileOption2;
    public string neutralOption;
    public PlayerArchetype profile1Type;
    public PlayerArchetype profile2Type;

    [Header("Resolution")]
    public string primaryStat;
    public bool requiresRoll;
    [Min(0)] public int tier = 1;
    public string successText;
    public string failText;
}
