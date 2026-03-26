using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Scene")]
    [SerializeField] private string combatSceneName = "Combat";

    private CombatStateController stateController;
    private TurnManager turnManager;
    private PlayerBattler playerBattler;
    private EnemyBattler enemyBattler;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static CombatManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        CombatManager existing = FindObjectOfType<CombatManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return Instance;
        }

        GameObject container = new GameObject("CombatManager");
        Instance = container.AddComponent<CombatManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        stateController = GetComponent<CombatStateController>();

        if (stateController == null)
            stateController = gameObject.AddComponent<CombatStateController>();

        if (turnManager == null)
            turnManager = new TurnManager();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void StartCombat(float difficultyModifier)
    {
        StartCombat(null, difficultyModifier);
    }

    public void StartCombat(EnemyInstance enemy, float difficultyModifier)
    {
        if (stateController.CombatActive)
        {
            Debug.LogWarning("Combat already active.");
            return;
        }

        if (enemy == null)
        {
            Debug.Log("Combat started with no enemy selected. Modifier: " + difficultyModifier);
            return;
        }

        if (!stateController.TryBeginCombat(enemy))
            return;

        Debug.Log($"Combat started vs {enemy.source.enemyName} ({enemy.source.archetype}) | Life: {enemy.life} Physical: {enemy.physical} Mental: {enemy.mental} | Modifier: {difficultyModifier}");
        SceneManager.LoadScene(combatSceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!stateController.CombatActive)
            return;

        if (scene.name == combatSceneName)
            SetupCombatScene();
        else if (stateController.RunSnapshot != null && scene.name == stateController.RunSnapshot.sceneName)
        {
            if (stateController.ShouldRestoreSnapshotOnReturn)
                StartCoroutine(stateController.RestoreRunStateAfterCombatNextFrame());
            else
                stateController.Clear();
        }
    }

    private void SetupCombatScene()
    {
        CombatSceneBindings bindings = FindObjectOfType<CombatSceneBindings>();
        if (bindings == null)
        {
            Debug.LogError("CombatSceneBindings missing in combat scene.");
            return;
        }

        turnManager.Initialize(stateController.RunSnapshot, stateController.CurrentEnemy);
        SpawnBattlers(bindings);
        StartCoroutine(RunCombatFlow(bindings));
    }

    private IEnumerator RunCombatFlow(CombatSceneBindings bindings)
    {
        yield return StartCoroutine(turnManager.RunTurnCombat(bindings));

        if (turnManager.Outcome == CombatOutcome.Victory)
        {
            stateController.ApplyCombatResults(turnManager.PlayerLife, turnManager.PlayerPhysical, turnManager.PlayerMental, turnManager.PlayerCombatStats);
            yield return new WaitForSeconds(0.8f);
            EndCombatAndReturnToRun();
            yield break;
        }

        bindings.ShowGameOverUI(() =>
        {
            stateController.MarkSkipRestoreOnReturn();
            SceneManager.LoadScene(stateController.RunSnapshot.sceneName);
        });
    }

    private void SpawnBattlers(CombatSceneBindings bindings)
    {
        playerBattler = FindObjectOfType<PlayerBattler>();
        enemyBattler = FindObjectOfType<EnemyBattler>();

        if (playerBattler != null && bindings.playerSpawnPoint != null)
        {
            if (playerBattler != null)
                playerBattler.Setup(turnManager.PlayerLife, turnManager.PlayerPhysical, turnManager.PlayerMental, turnManager.PlayerCombatStats);
        }

        if (enemyBattler != null && bindings.enemySpawnPoint != null)
            enemyBattler.Setup(stateController.CurrentEnemy);

        bindings.RefreshCombatVisualReferences();
    }

    private void EndCombatAndReturnToRun()
    {
        SceneManager.LoadScene(stateController.RunSnapshot.sceneName);
    }
}
