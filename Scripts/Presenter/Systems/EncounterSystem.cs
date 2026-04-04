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
        TriggerEncounterInternal(false, true);
    }

    public void TriggerEncounterFromTension()
    {
        TriggerEncounterInternal(false, false);
    }

    public void TriggerForcedEncounter()
    {
        TriggerEncounterInternal(true, true);
    }

    private void TriggerEncounterInternal(bool isForced, bool increaseTension)
    {
        if (increaseTension && TensionSystem.Instance != null)
            TensionSystem.Instance.AddTension(isForced ? 2 : 1);

        EnemyRunContext context = BuildContext(isForced);
        EnemyInstance selectedEnemy = enemyDatabase != null ? enemyDatabase.RollRandomEnemy(context) : null;

        float modifier = isForced ? riskModifier * 1.5f : riskModifier;
        // TODO: CombatManager.EnsureInstance().StartCombat(selectedEnemy, modifier);
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
