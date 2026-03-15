using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
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

    private int basePlayerLife;
    private int basePlayerPhysical;
    private int basePlayerMental;

    private int baseEnemyLife;
    private int baseEnemyPhysical;
    private int baseEnemyMental;

    private PlayerActionType? pendingPlayerAction;

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

        basePlayerLife = playerLife;
        basePlayerPhysical = playerPhysical;
        basePlayerMental = playerMental;

        baseEnemyLife = enemyLife;
        baseEnemyPhysical = enemyPhysical;
        baseEnemyMental = enemyMental;

        SpawnBattlers(bindings);
        StartCoroutine(RunTurnCombat(bindings));
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

    private IEnumerator RunTurnCombat(CombatSceneBindings bindings)
    {
        bool playerTurn = (playerLife + playerPhysical + playerMental) >= (enemyLife + enemyPhysical + enemyMental);
        bindings.SetCombatLog("O combate começou.");

        while (playerLife > 0 && enemyLife > 0)
        {
            UpdateCombatHud(bindings);

            if (playerTurn)
                yield return ExecutePlayerTurn(bindings);
            else
                yield return ExecuteEnemyTurn(bindings);

            if (enemyLife <= 0 || playerLife <= 0)
                break;

            playerTurn = !playerTurn;
            yield return new WaitForSeconds(0.5f);
        }

        UpdateCombatHud(bindings);

        if (enemyLife <= 0)
        {
            bindings.SetTurnText("Vitória");
            bindings.SetCombatLog("Inimigo derrotado. Placeholder de recompensa gerado.");
            ApplyCombatResultsToSnapshot();
            yield return new WaitForSeconds(0.8f);
            EndCombatAndReturnToRun();
            yield break;
        }

        bindings.SetTurnText("Derrota");
        bindings.SetCombatLog("Você foi derrotado.");
        bindings.ShowGameOverUI(() =>
        {
            shouldRestoreSnapshotOnReturn = false;
            SceneManager.LoadScene(runSnapshot.sceneName);
        });
    }

    private IEnumerator ExecutePlayerTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno do Jogador");
        bindings.SetCombatLog("Escolha uma ação.");
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CachePlayerAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CachePlayerAction;
        PlayerActionType action = pendingPlayerAction.Value;
        int playerRoll = RollDice(bindings);
        int enemyRoll = RollDice(bindings);

        ResolveActions(action, ChooseEnemyAction(), playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator ExecuteEnemyTurn(CombatSceneBindings bindings)
    {
        bindings.SetTurnText("Turno do Inimigo");
        bindings.SetCombatLog("Inimigo está escolhendo uma ação...");

        EnemyActionType enemyAction = ChooseEnemyAction();
        PlayerActionType passivePlayerAction = ChooseAutoDefenseAction();

        int enemyRoll = RollDice(bindings);
        int playerRoll = RollDice(bindings);

        ResolveActions(passivePlayerAction, enemyAction, playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private void CachePlayerAction(PlayerActionType action)
    {
        pendingPlayerAction = action;
    }

    private int RollDice(CombatSceneBindings bindings)
    {
        int roll = Random.Range(1, 21);
        bindings.SetDiceValue(roll);
        return roll;
    }

    private void ResolveActions(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        string resultLog;

        if (IsEnemyAttack(enemyAction) && (playerAction == PlayerActionType.Defend || playerAction == PlayerActionType.Parry))
        {
            resultLog = ResolveDefensiveResponse(playerAction, enemyAction, playerRoll, enemyRoll);
            bindings.SetCombatLog(resultLog);
            return;
        }

        if (IsPlayerAttack(playerAction))
        {
            int damage = Mathf.Max(1, playerRoll);
            if (enemyAction == EnemyActionType.Defend)
            {
                damage = Mathf.Max(0, damage - enemyRoll);
                resultLog = $"Você atacou ({playerAction}), mas o inimigo defendeu parte do dano. Dano final: {damage}.";
            }
            else
            {
                resultLog = $"Você atacou ({playerAction}) e causou {damage} de dano.";
            }

            ApplyDamageToEnemy(playerAction, damage);
        }
        else
        {
            resultLog = ResolvePlayerSpecialAction(playerAction, playerRoll);
        }

        if (enemyLife > 0 && playerLife > 0 && IsEnemyAttack(enemyAction))
        {
            ApplyDamageToPlayer(enemyAction, enemyRoll);
            resultLog += $" Inimigo atacou ({enemyAction}) e causou {enemyRoll} de dano.";
        }

        bindings.SetCombatLog(resultLog);
    }

    private string ResolveDefensiveResponse(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll)
    {
        if (playerAction == PlayerActionType.Defend)
        {
            int finalDamage = Mathf.Max(0, enemyRoll - playerRoll);
            ApplyDamageToPlayer(enemyAction, finalDamage);
            return $"Você defendeu. Defesa: {playerRoll}. Dano recebido: {finalDamage}.";
        }

        bool parrySuccess = playerRoll >= enemyRoll;
        if (parrySuccess)
        {
            ApplyDamageToEnemy(PlayerActionType.AttackLife, playerRoll);
            return $"Parry perfeito! Você reverteu {playerRoll} de dano ao inimigo.";
        }

        ApplyDamageToPlayer(enemyAction, enemyRoll);
        return $"Parry falhou. Você recebeu {enemyRoll} de dano.";
    }

    private string ResolvePlayerSpecialAction(PlayerActionType action, int roll)
    {
        switch (action)
        {
            case PlayerActionType.Flee:
                if (roll >= 18)
                {
                    enemyLife = 0;
                    return "Fuga bem sucedida! Combate encerrado.";
                }

                return "Tentativa de fuga falhou.";
            case PlayerActionType.InstantKill:
                if (roll >= 20)
                {
                    enemyLife = 0;
                    return "Instant Kill ativado! Inimigo eliminado.";
                }

                return "Instant Kill falhou.";
            case PlayerActionType.Learn:
                if (roll >= 12)
                    return "Learn bem sucedido: informações do inimigo coletadas (placeholder).";

                return "Learn falhou: nenhuma informação nova.";
            case PlayerActionType.Item:
                return "Uso de item ainda é placeholder.";
            default:
                return $"Ação {action} é placeholder.";
        }
    }

    private void ApplyDamageToEnemy(PlayerActionType attackType, int amount)
    {
        if (amount == 0)
            return;

        switch (attackType)
        {
            case PlayerActionType.AttackLife:
                enemyLife = Mathf.Clamp(enemyLife - amount, 0, baseEnemyLife);
                break;
            case PlayerActionType.AttackPhysical:
                enemyPhysical = Mathf.Clamp(enemyPhysical - amount, 0, baseEnemyPhysical);
                break;
            case PlayerActionType.AttackMental:
                enemyMental = Mathf.Clamp(enemyMental - amount, 0, baseEnemyMental);
                break;
            default:
                enemyLife = Mathf.Clamp(enemyLife - amount, 0, baseEnemyLife);
                break;
        }
    }

    private void ApplyDamageToPlayer(EnemyActionType attackType, int amount)
    {
        if (amount <= 0)
            return;

        switch (attackType)
        {
            case EnemyActionType.AttackLife:
                playerLife = Mathf.Max(0, playerLife - amount);
                break;
            case EnemyActionType.AttackPhysical:
                playerPhysical = Mathf.Max(0, playerPhysical - amount);
                break;
            case EnemyActionType.AttackMental:
                playerMental = Mathf.Max(0, playerMental - amount);
                break;
        }
    }

    private EnemyActionType ChooseEnemyAction()
    {
        int roll = Random.Range(0, 100);
        if (roll < 30)
            return EnemyActionType.AttackLife;
        if (roll < 55)
            return EnemyActionType.AttackPhysical;
        if (roll < 80)
            return EnemyActionType.AttackMental;
        return EnemyActionType.Defend;
    }

    private PlayerActionType ChooseAutoDefenseAction()
    {
        return Random.value < 0.75f ? PlayerActionType.Defend : PlayerActionType.Parry;
    }

    private bool IsPlayerAttack(PlayerActionType action)
    {
        return action == PlayerActionType.AttackLife || action == PlayerActionType.AttackPhysical || action == PlayerActionType.AttackMental;
    }

    private bool IsEnemyAttack(EnemyActionType action)
    {
        return action == EnemyActionType.AttackLife || action == EnemyActionType.AttackPhysical || action == EnemyActionType.AttackMental;
    }

    private void UpdateCombatHud(CombatSceneBindings bindings)
    {
        bindings.UpdateHud(
            playerLife,
            basePlayerLife,
            playerPhysical,
            basePlayerPhysical,
            playerMental,
            basePlayerMental,
            enemyLife,
            baseEnemyLife,
            enemyPhysical,
            baseEnemyPhysical,
            enemyMental,
            baseEnemyMental);
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
