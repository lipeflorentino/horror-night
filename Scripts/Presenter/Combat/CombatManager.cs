using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private static CombatManager instance;

    [Header("References")]
    [SerializeField] private CombatUI combatUI;
    [SerializeField] private PlayerBattler playerBattler;
    [SerializeField] private EnemyBattler enemyBattler;
    [SerializeField] private PlayerStatusManager playerStatusManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerGridMovement playerMovement;

    private CombatStateController stateController = new CombatStateController();
    private TurnManager turnManager = new TurnManager();

    private CombatBattlerRuntime playerRuntime;
    private CombatBattlerRuntime enemyRuntime;

    private bool combatActive;

    public static CombatManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindObjectOfType<CombatManager>();

        if (instance == null)
        {
            GameObject go = new GameObject("CombatManager");
            instance = go.AddComponent<CombatManager>();
        }

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (combatUI == null)
            combatUI = FindObjectOfType<CombatUI>(true);

        if (playerBattler == null)
            playerBattler = FindObjectOfType<PlayerBattler>(true);

        if (enemyBattler == null)
            enemyBattler = FindObjectOfType<EnemyBattler>(true);

        if (playerStatusManager == null)
            playerStatusManager = FindObjectOfType<PlayerStatusManager>();

        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();

        HookEvents();
    }

    public void StartCombat(EnemyInstance enemyData, float riskModifier)
    {
        if (enemyData == null || enemyData.source == null || combatActive)
            return;

        combatActive = true;

        SetupRuntimes(enemyData, riskModifier);

        if (playerBattler != null)
            playerBattler.Setup(playerRuntime.maxHeart, playerRuntime.maxBody, playerRuntime.maxMind);

        if (enemyBattler != null)
            enemyBattler.Setup(enemyData);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (combatUI != null)
        {
            combatUI.ShowCombat(true);
            combatUI.SetPlayer(playerRuntime);
            combatUI.SetEnemy(enemyRuntime);
            combatUI.SetLog($"Encounter iniciado contra {enemyRuntime.displayName}.");
        }

        stateController.BeginCombat();
        StartFirstTurn();
    }

    private void SetupRuntimes(EnemyInstance enemyData, float riskModifier)
    {
        int playerHeart = playerStatusManager != null ? Mathf.RoundToInt(playerStatusManager.GetCurrentHeart()) : 25;
        int playerBody = playerStatusManager != null ? Mathf.RoundToInt(playerStatusManager.GetCurrentBody()) : 25;
        int playerMind = playerStatusManager != null ? Mathf.RoundToInt(playerStatusManager.GetCurrentMind()) : 25;
        int playerInitiative = playerStatusManager != null ? playerStatusManager.GetInitiative() : 10;

        playerRuntime = new CombatBattlerRuntime
        {
            displayName = "Player",
            isPlayer = true,
            heart = playerHeart,
            maxHeart = playerHeart,
            body = playerBody,
            maxBody = playerBody,
            mind = playerMind,
            maxMind = playerMind,
            hp = Mathf.Max(1, playerHeart + playerBody),
            maxHp = Mathf.Max(1, playerHeart + playerBody),
            attack = playerStatusManager != null ? playerStatusManager.GetAttack() : 10,
            defense = playerStatusManager != null ? playerStatusManager.GetDefense() : 5,
            initiative = playerInitiative
        };

        int enemyHeart = Mathf.Max(1, enemyData.heart);
        int enemyBody = Mathf.Max(1, enemyData.body);
        int enemyMind = Mathf.Max(1, enemyData.mind);

        enemyRuntime = new CombatBattlerRuntime
        {
            displayName = enemyData.source.enemyName,
            heart = enemyHeart,
            maxHeart = enemyHeart,
            body = enemyBody,
            maxBody = enemyBody,
            mind = enemyMind,
            maxMind = enemyMind,
            hp = Mathf.Max(1, enemyHeart + enemyBody),
            maxHp = Mathf.Max(1, enemyHeart + enemyBody),
            attack = Mathf.RoundToInt(10f * Mathf.Max(0.8f, riskModifier)),
            defense = Mathf.RoundToInt(4f * Mathf.Max(0.8f, riskModifier)),
            initiative = Mathf.RoundToInt(8f * Mathf.Max(0.8f, riskModifier))
        };
    }

    private void StartFirstTurn()
    {
        int playerInitiativeRoll = playerRuntime.initiative + TurnActions.RollD6();
        int enemyInitiativeRoll = enemyRuntime.initiative + TurnActions.RollD6();

        if (playerInitiativeRoll >= enemyInitiativeRoll)
            StartPlayerTurn();
        else
            StartEnemyTurn();
    }

    private void StartPlayerTurn()
    {
        if (!combatActive)
            return;

        stateController.SetPlayerTurn();
        turnManager.StartTurn();

        TurnActionResult rechargeResult = TurnActions.ResolveRecharge(false);
        playerRuntime.RecoverResources(rechargeResult.recoveredPerResource);

        RefreshHud();

        if (combatUI != null)
        {
            combatUI.SetTurnState("Turno do Player");
            combatUI.SetLog(rechargeResult.message);
        }
    }

    private void StartEnemyTurn()
    {
        if (!combatActive)
            return;

        stateController.SetEnemyTurn();

        int roll = TurnActions.RollD6();
        bool defend = roll <= 2;

        if (defend)
        {
            enemyRuntime.defense += 1;
            if (combatUI != null)
                combatUI.SetLog($"{enemyRuntime.displayName} reforçou defesa.");
        }
        else
        {
            int rawDamage = enemyRuntime.attack + Mathf.RoundToInt(roll * 1.5f);
            int dealt = playerRuntime.TakeDamage(rawDamage);
            if (combatUI != null)
                combatUI.SetLog($"{enemyRuntime.displayName} atacou e causou {dealt} de dano.");
        }

        RefreshHud();

        if (TryResolveCombatEnd())
            return;

        StartPlayerTurn();
    }

    private void HookEvents()
    {
        turnManager.OnDiceChanged += dice => combatUI?.SetDice(dice);
        stateController.OnCombatEnded += HandleCombatEnded;

        if (combatUI == null)
            return;

        combatUI.OnRechargeRequested += HandleRecharge;
        combatUI.OnInvestigateRequested += HandleInvestigate;
        combatUI.OnFleeRequested += HandleFlee;
        combatUI.OnUseItemRequested += HandleUseItem;
        combatUI.OnCombatActionRequested += HandleCombatAction;
        combatUI.OnEndTurnRequested += HandleEndTurn;
        combatUI.OnVictoryContinueRequested += HandleContinueAfterVictory;
        combatUI.OnRestartRequested += HandleRestart;
        combatUI.OnQuitRequested += HandleQuit;
    }

    private void HandleRecharge(bool boosted)
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        TurnActionResult result = TurnActions.ResolveRecharge(boosted);
        if (!turnManager.TrySpendDice(result.diceSpent))
            return;

        playerRuntime.RecoverResources(result.recoveredPerResource);
        RefreshHud();
        combatUI?.SetLog(result.message);
    }

    private void HandleInvestigate()
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        TurnActionResult result = TurnActions.ResolveInvestigate();
        if (!turnManager.TrySpendDice(result.diceSpent))
            return;

        enemyRuntime.knownInfoLevel = Mathf.Max(enemyRuntime.knownInfoLevel, result.revealedInfoLevel);
        combatUI?.SetLog(result.message);
    }

    private void HandleFlee(int diceToRoll)
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        TurnActionResult result = TurnActions.ResolveFlee(diceToRoll);
        if (!turnManager.TrySpendDice(result.diceSpent))
            return;

        combatUI?.SetLog(result.message);

        if (result.success)
            stateController.EndCombat(CombatOutcome.Fled);
    }

    private void HandleUseItem()
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        int count = playerInventory != null ? playerInventory.items.Count : 0;
        combatUI?.SetLog(count > 0
            ? $"Inventário aberto. Itens disponíveis: {count}."
            : "Inventário vazio.");
    }

    private void HandleCombatAction(PlayerActionType actionType, CombatActionIntensity intensity)
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        int heartCost = 0;
        int bodyCost = 0;
        int mindCost = 0;

        switch (actionType)
        {
            case PlayerActionType.AttackHeart:
                heartCost = 2;
                break;
            case PlayerActionType.AttackBody:
                bodyCost = 2;
                break;
            case PlayerActionType.AttackMind:
                mindCost = 2;
                break;
            case PlayerActionType.Defend:
                bodyCost = 1;
                break;
        }

        if (!playerRuntime.SpendCost(heartCost, bodyCost, mindCost))
        {
            combatUI?.SetLog("Recursos insuficientes para a ação.");
            return;
        }

        TurnActionResult result = TurnActions.ResolveActionRoll(intensity);
        if (!turnManager.TrySpendDice(result.diceSpent))
            return;

        if (actionType == PlayerActionType.Defend)
        {
            playerRuntime.defense += 1;
            combatUI?.SetLog("Defesa aumentada até o próximo turno.");
            RefreshHud();
            return;
        }

        int rawDamage = playerRuntime.attack + result.damage;
        int dealt = enemyRuntime.TakeDamage(rawDamage);

        combatUI?.SetLog($"Ação {actionType} rolou {result.roll} e causou {dealt} dano.");
        RefreshHud();
        TryResolveCombatEnd();
    }

    private void HandleEndTurn()
    {
        if (stateController.CurrentState != CombatFlowState.PlayerTurn)
            return;

        StartEnemyTurn();
    }

    private bool TryResolveCombatEnd()
    {
        if (enemyRuntime.IsDead)
        {
            stateController.EndCombat(CombatOutcome.Victory);
            return true;
        }

        if (playerRuntime.IsDead)
        {
            stateController.EndCombat(CombatOutcome.Defeat);
            return true;
        }

        return false;
    }

    private void HandleCombatEnded(CombatOutcome outcome)
    {
        combatActive = false;

        switch (outcome)
        {
            case CombatOutcome.Victory:
                combatUI?.ShowVictory();
                combatUI?.SetLog("Vitória! Continue para voltar à cena principal.");
                break;
            case CombatOutcome.Defeat:
                combatUI?.ShowGameOver();
                combatUI?.SetLog("Game Over.");
                break;
            case CombatOutcome.Fled:
                FinishCombatSession();
                break;
        }
    }

    private void HandleContinueAfterVictory()
    {
        FinishCombatSession();
    }

    private void FinishCombatSession()
    {
        stateController.SetFinished();
        combatUI?.ShowCombat(false);

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private void HandleRestart()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    private void HandleQuit()
    {
        Application.Quit();
    }

    private void RefreshHud()
    {
        combatUI?.SetPlayer(playerRuntime);
        combatUI?.SetEnemy(enemyRuntime);
    }
}
