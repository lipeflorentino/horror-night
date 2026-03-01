using UnityEngine;

public class NodeActivityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelController levelController;
    [SerializeField] private PlayerGridMovement playerMovement;
    [SerializeField] private LootSystem lootSystem;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private EncounterSystem encounterSystem;
    [SerializeField] private TreasureSystem treasureSystem;

    private void Awake()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();
        if (lootSystem == null)
            lootSystem = FindObjectOfType<LootSystem>();
        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();
        if (encounterSystem == null)
            encounterSystem = FindObjectOfType<EncounterSystem>();
        if (treasureSystem == null)
            treasureSystem = FindObjectOfType<TreasureSystem>();
    }

    private void OnEnable()
    {
        if (playerMovement != null)
            playerMovement.OnMoveCompleted += HandleMoveCompleted;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
            playerMovement.OnMoveCompleted -= HandleMoveCompleted;
    }

    private void HandleMoveCompleted(int index)
    {
        if (levelController == null || levelController.currentLevel == null || levelController.nodes == null)
            return;

        if (index < 0 || index >= levelController.nodes.Length)
            return;

        LevelNode node = levelController.nodes[index];

        if (node == null)
            return;

        if (node.definition.flags.HasFlag(NodeFlags.OneTimeOnly) && node.activityResolved)
            return;

        NodeActivityType result = RollActivity(node, levelController.currentLevel);
        DispatchActivity(result, node, levelController.currentLevel);

        if (node.definition.flags.HasFlag(NodeFlags.OneTimeOnly))
            node.activityResolved = true;
    }

    private NodeActivityType RollActivity(LevelNode node, LevelSO level)
    {
        float lootWeight = IsLootAllowed(node) ? level.GetEffectiveActivityWeight(NodeActivityType.Loot) : 0f;
        float eventWeight = IsEventAllowed(node) ? level.GetEffectiveActivityWeight(NodeActivityType.Event) : 0f;
        float encounterWeight = IsEncounterAllowed(node) ? level.GetEffectiveActivityWeight(NodeActivityType.Encounter) * Mathf.Max(0f, level.Encounter_Risk_Modifier) : 0f;
        float treasureWeight = IsTreasureAllowed(node) ? level.GetEffectiveActivityWeight(NodeActivityType.Treasure) : 0f;
        float noneWeight = level.GetEffectiveActivityWeight(NodeActivityType.None);

        float total = lootWeight + eventWeight + encounterWeight + treasureWeight + noneWeight;

        if (total <= 0f)
            return NodeActivityType.None;

        float roll = Random.value * total;

        if (roll < lootWeight) return NodeActivityType.Loot;
        roll -= lootWeight;

        if (roll < eventWeight) return NodeActivityType.Event;
        roll -= eventWeight;

        if (roll < encounterWeight) return NodeActivityType.Encounter;
        roll -= encounterWeight;

        if (roll < treasureWeight) return NodeActivityType.Treasure;

        return NodeActivityType.None;
    }

    private void DispatchActivity(NodeActivityType activity, LevelNode node, LevelSO level)
    {
        switch (activity)
        {
            case NodeActivityType.Loot:
                lootSystem?.TriggerLoot(node);
                break;
            case NodeActivityType.Event:
                eventSystem?.TriggerEvent(node, level);
                break;
            case NodeActivityType.Encounter:
                encounterSystem?.TriggerEncounter();
                break;
            case NodeActivityType.Treasure:
                treasureSystem?.TriggerTreasure(node);
                break;
            default:
                break;
        }
    }

    private static bool IsLootAllowed(LevelNode node)
    {
        return node.definition.flags.HasFlag(NodeFlags.CanSpawnLoot) && !node.looted;
    }

    private static bool IsEventAllowed(LevelNode node)
    {
        return node.definition.flags.HasFlag(NodeFlags.CanSpawnEvent);
    }

    private static bool IsEncounterAllowed(LevelNode node)
    {
        return node.definition.flags.HasFlag(NodeFlags.CanSpawnEnemy);
    }

    private static bool IsTreasureAllowed(LevelNode node)
    {
        return !node.looted && node.definition.nodeType != NodeType.Portal;
    }
}
