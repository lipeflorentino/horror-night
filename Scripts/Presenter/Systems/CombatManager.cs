using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [System.Serializable]
    private class RunStateSnapshot
    {
        public string sceneName;
        public LevelSO level;
        public int levelIndex;
        public bool[] exploredNodes;
        public PlayerStatusManager.PlayerStatusSnapshot playerStatus;
        public List<ItemSO> inventoryItems;
    }

    public static CombatManager Instance;

    [Header("Scene")]
    [SerializeField] private string combatSceneName = "Combat";

    [Header("Combat Prefabs")]
    [SerializeField] private GameObject enemyCombatPrefab;
    [SerializeField] private GameObject playerBattlerPrefab;

    private EnemyInstance currentEnemy;
    private RunStateSnapshot runSnapshot;
    private bool combatActive;
    private bool shouldRestoreSnapshotOnReturn = true;

    private int playerLife;
    private int playerPhysical;
    private int playerMental;

    private int enemyLife;
    private int enemyPhysical;
    private int enemyMental;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        if (combatActive)
        {
            Debug.LogWarning("Combat already active.");
            return;
        }

        if (enemy == null)
        {
            Debug.Log("Combat started with no enemy selected. Modifier: " + difficultyModifier);
            return;
        }

        if (!CaptureRunSnapshot())
            return;

        currentEnemy = enemy;
        combatActive = true;
        shouldRestoreSnapshotOnReturn = true;

        Debug.Log($"Combat started vs {enemy.source.enemyName} ({enemy.source.archetype}) | Life: {enemy.life} Physical: {enemy.physical} Mental: {enemy.mental} | Modifier: {difficultyModifier}");

        SceneManager.LoadScene(combatSceneName);
    }

    private bool CaptureRunSnapshot()
    {
        LevelController levelController = FindObjectOfType<LevelController>();
        PlayerStatusManager statusManager = FindObjectOfType<PlayerStatusManager>();
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();

        if (levelController == null || statusManager == null || inventory == null)
        {
            Debug.LogError("Could not capture run state before combat.");
            return false;
        }

        runSnapshot = new RunStateSnapshot
        {
            sceneName = SceneManager.GetActiveScene().name,
            level = levelController.currentLevel,
            levelIndex = levelController.CurrentIndex,
            exploredNodes = levelController.CaptureExploredSnapshot(),
            playerStatus = statusManager.GetSnapshot(),
            inventoryItems = inventory.CreateSnapshot()
        };

        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!combatActive)
            return;

        if (scene.name == combatSceneName)
            SetupCombatScene();
        else if (runSnapshot != null && scene.name == runSnapshot.sceneName)
        {
            if (shouldRestoreSnapshotOnReturn)
                StartCoroutine(RestoreRunStateAfterCombatNextFrame());
            else
                ClearCombatState();
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

        playerLife = Mathf.RoundToInt(runSnapshot.playerStatus.life);
        playerPhysical = Mathf.Max(1, Mathf.RoundToInt(runSnapshot.playerStatus.strength));
        playerMental = Mathf.Max(1, Mathf.RoundToInt(runSnapshot.playerStatus.sanity));

        enemyLife = Mathf.Max(1, currentEnemy.life);
        enemyPhysical = Mathf.Max(1, currentEnemy.physical);
        enemyMental = Mathf.Max(1, currentEnemy.mental);

        SpawnBattlers(bindings);
        StartCoroutine(RunBasicTurnCombat(bindings));
    }

    private void SpawnBattlers(CombatSceneBindings bindings)
    {
        if (playerBattlerPrefab != null && bindings.playerSpawnPoint != null)
        {
            GameObject playerObj = Instantiate(playerBattlerPrefab, bindings.playerSpawnPoint.position, Quaternion.identity);
            PlayerBattler battler = playerObj.GetComponent<PlayerBattler>();
            if (battler != null)
                battler.Setup(playerLife, playerPhysical, playerMental);
        }

        if (enemyCombatPrefab != null && bindings.enemySpawnPoint != null)
        {
            GameObject enemyObj = Instantiate(enemyCombatPrefab, bindings.enemySpawnPoint.position, Quaternion.identity);
            EnemyBattler battler = enemyObj.GetComponent<EnemyBattler>();
            if (battler != null)
                battler.Setup(currentEnemy);
        }
    }

    private IEnumerator RunBasicTurnCombat(CombatSceneBindings bindings)
    {
        bool playerTurn = (playerLife + playerPhysical + playerMental) >= (enemyLife + enemyPhysical + enemyMental);

        while (playerLife > 0 && enemyLife > 0)
        {
            if (playerTurn)
            {
                int damage = Mathf.Max(1, Mathf.RoundToInt(playerPhysical * 0.35f));
                enemyLife = Mathf.Max(0, enemyLife - damage);
                Debug.Log($"[Combat] Player attacks for {damage}. Enemy life: {enemyLife}");
            }
            else
            {
                int lifeDamage = Mathf.Max(1, Mathf.RoundToInt(enemyPhysical * 0.3f));
                int sanityDamage = Mathf.Max(1, Mathf.RoundToInt(enemyMental * 0.15f));

                playerLife = Mathf.Max(0, playerLife - lifeDamage);
                playerMental = Mathf.Max(0, playerMental - sanityDamage);

                Debug.Log($"[Combat] Enemy attacks for {lifeDamage} life and {sanityDamage} sanity. Player life: {playerLife}");
            }

            if (enemyLife <= 0 || playerLife <= 0)
                break;

            playerTurn = !playerTurn;
            yield return new WaitForSeconds(0.8f);
        }

        if (enemyLife <= 0)
        {
            Debug.Log("[Combat] Enemy defeated. Placeholder reward generated.");
            ApplyCombatResultsToSnapshot();
            EndCombatAndReturnToRun();
            yield break;
        }

        bindings.ShowGameOverUI(() =>
        {
            shouldRestoreSnapshotOnReturn = false;
            SceneManager.LoadScene(runSnapshot.sceneName);
        });
    }

    private void ApplyCombatResultsToSnapshot()
    {
        if (runSnapshot == null)
            return;

        runSnapshot.playerStatus.life = playerLife;
        runSnapshot.playerStatus.sanity = playerMental;
        runSnapshot.playerStatus.strength = playerPhysical;
    }

    private void EndCombatAndReturnToRun()
    {
        SceneManager.LoadScene(runSnapshot.sceneName);
    }

    private IEnumerator RestoreRunStateAfterCombatNextFrame()
    {
        yield return null;


        LevelController levelController = FindObjectOfType<LevelController>();
        PlayerStatusManager statusManager = FindObjectOfType<PlayerStatusManager>();
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();

        if (levelController != null)
            levelController.RestoreProgress(runSnapshot.level, runSnapshot.levelIndex, runSnapshot.exploredNodes);

        if (statusManager != null)
            statusManager.RestoreSnapshot(runSnapshot.playerStatus);

        if (inventory != null)
            inventory.RestoreSnapshot(runSnapshot.inventoryItems);

        PlayerGridMovement movement = FindObjectOfType<PlayerGridMovement>();
        if (movement != null && levelController != null)
        {
            Vector3 position = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);
            movement.transform.position = position;
        }

        ClearCombatState();
    }

    private void ClearCombatState()
    {
        combatActive = false;
        currentEnemy = null;
        runSnapshot = null;
        shouldRestoreSnapshotOnReturn = true;
    }
}
