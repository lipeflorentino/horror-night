using UnityEngine;

public class EncounterSystem : MonoBehaviour
{
    public static EncounterSystem Instance;

    [SerializeField] private EnemyDatabase enemyDatabase;
    [SerializeField] private LevelController levelController;

    private float riskModifier = 1f;

    private void Awake()
    {
        Instance = this;

        if (enemyDatabase == null)
            enemyDatabase = FindObjectOfType<EnemyDatabase>();

        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();
    }

    public void SetRiskModifier(float value)
    {
        riskModifier = value;
    }

    public void TriggerEncounter()
    {
        TriggerEncounterInternal(false);
    }

    public void TriggerForcedEncounter()
    {
        TriggerEncounterInternal(true);
    }

    private void TriggerEncounterInternal(bool isForced)
    {
        EnemyRunContext context = BuildContext(isForced);
        EnemyInstance selectedEnemy = enemyDatabase != null ? enemyDatabase.RollRandomEnemy(context) : null;

        float modifier = isForced ? riskModifier * 1.5f : riskModifier;
        CombatManager.EnsureInstance().StartCombat(selectedEnemy, modifier);
    }

    private EnemyRunContext BuildContext(bool isForced)
    {
        LevelSO level = levelController != null ? levelController.currentLevel : null;

        return new EnemyRunContext
        {
            tier = level != null ? Mathf.Max(0, level.Tier_Min) : 0,
            levelTags = level != null ? level.Tags : string.Empty,
            riskModifier = riskModifier,
            forcedEncounter = isForced
        };
    }
}
