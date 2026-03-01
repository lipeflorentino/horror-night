using UnityEngine;

[CreateAssetMenu(fileName = "LevelSO", menuName = "Game/Level Definition")]
public class LevelSO : ScriptableObject
{
    [Header("Identity")]
    public string levelId;
    [Header("Structure")]
    [Min(3)]
    public int size = 5; 
    public GameObject backgroundPrefab;
    [Header("Node Definitions")]
    public LevelNodeSO defaultNode, leftPortalNode, rightPortalNode;
    [Header("Layout")]
    public float tileSpacing = 4f;

    /* 
    Regras:
        Index 0 → leftPortalNode
        Index size - 1 → rightPortalNode
        Demais → defaultNode 
    */

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

    [Header("Node Activity - Base Weights")]
    [Min(0f)] public float Loot_Weight = 1f;
    [Min(0f)] public float Event_Weight = 1f;
    [Min(0f)] public float Encounter_Weight = 1f;
    [Min(0f)] public float Treasure_Weight = 0.5f;
    [Min(0f)] public float None_Weight = 0.5f;

    [Header("Node Activity - Runtime Modifiers")]
    public float Loot_Weight_Modifier;
    public float Event_Weight_Modifier;
    public float Encounter_Weight_Modifier;
    public float Treasure_Weight_Modifier;
    public float None_Weight_Modifier;

    public float GetEffectiveActivityWeight(NodeActivityType activityType)
    {
        switch (activityType)
        {
            case NodeActivityType.Loot:
                return Mathf.Max(0f, Loot_Weight + Loot_Weight_Modifier);
            case NodeActivityType.Event:
                return Mathf.Max(0f, Event_Weight + Event_Weight_Modifier);
            case NodeActivityType.Encounter:
                return Mathf.Max(0f, Encounter_Weight + Encounter_Weight_Modifier);
            case NodeActivityType.Treasure:
                return Mathf.Max(0f, Treasure_Weight + Treasure_Weight_Modifier);
            default:
                return Mathf.Max(0f, None_Weight + None_Weight_Modifier);
        }
    }
}
