using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinition", menuName = "Game/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    public int ID;
    public string LevelName;

    [Header("Tier Range")]
    public int Tier_Min;
    public int Tier_Max;

    [Header("Spawn Settings")]
    public int Spawn_Weight;
    public int Base_Difficulty;

    [Header("Tension & Presence")]
    public float Base_Tension_Modifier;
    public float Presence_Growth_Modifier;
    public float Encounter_Risk_Modifier;

    [Header("Reward Scaling")]
    public int Reward_Min_Value;
    public int Reward_Max_Value;

    [Header("Tags (comma separated)")]
    [TextArea]
    public string Tags;

    [Header("Special Rule Description")]
    [TextArea]
    public string Special_Rule;
}