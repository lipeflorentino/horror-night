using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds0_5 = new(0.5f);
    private const int DefaultPowerDiceCount = 3;
    private const int DefaultAccuracyDiceCount = 3;
    [SerializeField] private string gameplaySceneName = "Gameplay";
    public CombatView View;
    public CombatInputHandler Input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public bool IsPlayerAttacker => PlayerIsAttacker;

    private DiceService DiceService;
    private ActionResolverService Resolver;
    private InitiativeResolverService InitiativeResolverService;
    private EnemyActionSelector EnemyActionSelector;
    private EnemyTurnPlanner EnemyTurnPlanner;

    private ActionDefinition AttackDef;
    private ActionDefinition DefenseDef;

    private bool PlayerIsAttacker = true;

    private ActionInstance PendingPlayerAction;
    private ActionInstance PendingEnemyAction;
    private List<DiceResult> PendingPlayerPowerRolls = new();
    private List<DiceResult> PendingPlayerAccuracyRolls = new();
    private List<DiceResult> PendingEnemyPowerRolls = new();
    private List<DiceResult> PendingEnemyAccuracyRolls = new();
    private List<DiceStatType> PendingEnemyPowerDiceTypes = new();
    private List<DiceStatType> PendingEnemyAccuracyDiceTypes = new();
    private bool CombatEnded;
    private int lastGrantedXp;
    private Dictionary<ItemSO, int> lastGrantedItens;
    private CombatSessionData SessionData;
    private RewardService RewardService;
    private InventoryInputHandler InventoryInputHandler;
    private PlayerInventory CombatPlayerInventory;

    void Start()
    {
        DiceService = new DiceService();
        Resolver = new ActionResolverService();
        InitiativeResolverService = new InitiativeResolverService();
        EnemyActionSelector = new EnemyActionSelector();
        EnemyTurnPlanner = new EnemyTurnPlanner(EnemyActionSelector);
        RewardService = new RewardService();
        AttackDef = new ActionDefinition("attack", ActionType.Attack, 0);
        DefenseDef = new ActionDefinition("defense", ActionType.Defense, 0);

        SessionData = CombatSessionStore.Consume();
        InitializeBattlers(SessionData);
        DefineStartingTurnByInitiative();

        InventoryInputHandler = FindObjectOfType<InventoryInputHandler>();
        Input = FindObjectOfType<CombatInputHandler>();
        View = FindObjectOfType<CombatView>();
        CombatPlayerInventory = ResolveCombatPlayerInventory();

        InventoryInputHandler.Init(this, CombatPlayerInventory);

        Input.Init(this);
        View.Init();
        View.BindInput(Input);
        RefreshCombatUI();

        UpdateTurnRoleUI();
    }

    public void RefreshCombatUI()
    {
        // Sincroniza stats do Player Battler com PlayerStatusManager antes de atualizar a UI
        if (Player != null && CombatPlayerInventory?.GetComponent<PlayerStatusManager>() != null)
        {
            SyncPlayerStatsFromStatusManager();
        }
        View.UpdateView(Player, Enemy);
    }
    
    public void SyncPlayerStatsFromStatusManager()
    {
        PlayerStatusManager statusManager = CombatPlayerInventory.GetComponent<PlayerStatusManager>();
        if (statusManager == null || Player == null)
            return;

        Player.Heart = statusManager.GetStatValue("heart");
        Player.Body = statusManager.GetStatValue("body");
        Player.Mind = statusManager.GetStatValue("mind");
        Player.Attack = statusManager.GetAttack();
        Player.Defense = statusManager.GetDefense();
        Player.Initiative = statusManager.GetInitiative();
        
        Debug.Log($"[CombatManager] Sincronizado stats do Player Battler: " +
                  $"Heart={Player.Heart}, Body={Player.Body}, Mind={Player.Mind}, " +
                  $"Attack={Player.Attack}, Defense={Player.Defense}, Initiative={Player.Initiative}");
    }

    private void DefineStartingTurnByInitiative()
    {
        Battler firstBattler = InitiativeResolverService.ResolveStartingBattler(Player, Enemy);
        PlayerIsAttacker = firstBattler == Player;
    }

    private void InitializeBattlers(CombatSessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogWarning("[Combat] No CombatSessionData found. Using default battlers.");
            Player = new Battler("Player", 1, 100, 10, 10, 10, 10, 5, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, true);
            Enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, false);
            return;
        }

        PlayerStatusSnapshot playerSnapshot = sessionData.PlayerSnapshot;
        EnemyInstance enemySnapshot = sessionData.EnemyInstance;

        SetEnemyVisual(enemySnapshot ?? null);

        Player = new Battler(
            "Player",
            Mathf.Max(1, playerSnapshot.level),
            Mathf.RoundToInt(playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.heart),
            Mathf.RoundToInt(playerSnapshot.mind),
            Mathf.RoundToInt(playerSnapshot.body),
            Mathf.RoundToInt(playerSnapshot.attack),
            Mathf.RoundToInt(playerSnapshot.defense),
            Mathf.RoundToInt(playerSnapshot.initiative),
            Mathf.Max(1, playerSnapshot.maxPowerDices > 0 ? playerSnapshot.maxPowerDices : DefaultPowerDiceCount),
            Mathf.Max(1, playerSnapshot.maxAccuracyDices > 0 ? playerSnapshot.maxAccuracyDices : DefaultAccuracyDiceCount),
            true,
            Mathf.RoundToInt(playerSnapshot.maxHp > 0 ? playerSnapshot.maxHp : playerSnapshot.hp)
        );

        if (enemySnapshot != null)
        {
            string enemyName = enemySnapshot.source != null ? enemySnapshot.source.enemyName : "Enemy";
            Enemy = new Battler(
                enemyName,
                enemySnapshot.runTier,
                enemySnapshot.hp,
                enemySnapshot.heart,
                enemySnapshot.mind,
                enemySnapshot.body,
                enemySnapshot.attack,
                enemySnapshot.defense,
                enemySnapshot.initiative,
                enemySnapshot.currentPowerDices > 0 ? enemySnapshot.currentPowerDices : DefaultPowerDiceCount,
                enemySnapshot.currentAccuracyDices > 0 ? enemySnapshot.currentAccuracyDices : DefaultAccuracyDiceCount,
                false
            );
        }
        else
        {
            Debug.LogWarning("[Combat] Enemy snapshot missing. Using default enemy.");
            Enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, false);
        }
    }

    public void ReceivePlayerInput(ActionType type, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        if (CombatEnded)
            return;

        ActionType expectedType = PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        if (type != expectedType)
        {
            Debug.Log($"[Input] Ignored invalid action for current role. Expected {expectedType} and received {type}");
            return;
        }

        StartCoroutine(ResolveTurnFlow(type, powerDiceTypes, accuracyDiceTypes));
    }

    public void ReceivePlayerSkipTurn()
    {
        if (CombatEnded)
            return;

        StartCoroutine(SkipTurnRoutine());
    }

    private IEnumerator SkipTurnRoutine()
    {
        yield return WaitForSeconds0_5;
        if (!PlayerIsAttacker)
            yield break;

        View.ShowSkipTurnFeedback(true);
        yield return WaitForSeconds0_5;
        EndTurn();
    }

    private IEnumerator ResolveTurnFlow(ActionType action, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        yield return WaitForSeconds0_5;

        GenerateEnemyAction();

        yield return WaitForSeconds0_5;

        RollActions(action, powerDiceTypes, accuracyDiceTypes);

        bool attackerAccuracyEffective = ResolveAttackAccuracy();
        bool defenseAccuracyEffective = ResolveDefenseAccuracy();
        bool isPlayerDefending = action == ActionType.Defense;

        yield return View.PlayDiceResolution(PendingPlayerAccuracyRolls, PendingEnemyAccuracyRolls, DiceRollType.Accuracy);

        bool shouldPlayPowerResolution = attackerAccuracyEffective && (!isPlayerDefending || defenseAccuracyEffective);

        if (shouldPlayPowerResolution)
        {
            List<DiceResult> playerRolls = isPlayerDefending && !defenseAccuracyEffective ? null : PendingPlayerPowerRolls;
            yield return View.PlayDiceResolution(playerRolls, PendingEnemyPowerRolls, DiceRollType.Power);
        }

        Resolve();

        yield return WaitForSeconds0_5;

        if (TryHandleCombatEnd())
            yield break;

        EndTurn();
    }

    private void GenerateEnemyAction()
    {
        EnemyTurnPlan plan = EnemyTurnPlanner.BuildPlan(Enemy, SessionData?.EnemyInstance, AttackDef, DefenseDef);
        PendingEnemyAction = plan.Action;
        PendingEnemyPowerDiceTypes = plan.PowerDiceTypes;
        PendingEnemyAccuracyDiceTypes = plan.AccuracyDiceTypes;
    }

    private void RollActions(ActionType action, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        ActionDefinition playerAction = BuildDefinitionFromBattler(Player, action);

        PendingPlayerPowerRolls = DiceService.RollMany(Player, powerDiceTypes, DiceRollType.Power, Player.Level, Enemy.Level);
        PendingPlayerAccuracyRolls = DiceService.RollMany(Player, accuracyDiceTypes, DiceRollType.Accuracy, Player.Level, Enemy.Level);
        PendingEnemyPowerRolls = DiceService.RollMany(Enemy, PendingEnemyPowerDiceTypes, DiceRollType.Power, Enemy.Level, Player.Level);
        PendingEnemyAccuracyRolls = DiceService.RollMany(Enemy, PendingEnemyAccuracyDiceTypes, DiceRollType.Accuracy, Enemy.Level, Player.Level);

        DiceResult playerPowerDice = DiceService.GetBestResult(PendingPlayerPowerRolls);
        DiceResult playerAccuracyDice = DiceService.GetBestResult(PendingPlayerAccuracyRolls);
        DiceResult enemyPowerDice = DiceService.GetBestResult(PendingEnemyPowerRolls);
        DiceResult enemyAccuracyDice = DiceService.GetBestResult(PendingEnemyAccuracyRolls);

        PendingPlayerAction = new ActionInstance(playerAction, playerPowerDice, playerAccuracyDice);
        PendingEnemyAction.Definition = BuildDefinitionFromBattler(Enemy, PendingEnemyAction.Definition.Type);
        PendingEnemyAction = new ActionInstance(PendingEnemyAction.Definition, enemyPowerDice, enemyAccuracyDice);

        Debug.Log($"[Flow] Player rolled POWER best:{playerPowerDice.Value} | ACCURACY best:{playerAccuracyDice.Value} using {PendingPlayerPowerRolls.Count + PendingPlayerAccuracyRolls.Count} dice.");
        Debug.Log($"[Flow] Enemy rolled POWER best:{enemyPowerDice.Value} | ACCURACY best:{enemyAccuracyDice.Value} using {PendingEnemyPowerRolls.Count + PendingEnemyAccuracyRolls.Count} dice.");
    }

    public int GetDiceMaxValueForType(Battler battler, DiceStatType diceType)
    {
        return DiceService.GetDiceMaxValueForType(battler, diceType);
    }

    public List<int> GetDiceFacesForSelection(IReadOnlyList<DiceStatType> diceTypes, bool isAggregated = false)
    {
        return isAggregated ? DiceService.ConvertToAggregatedFaces(Player, diceTypes) : DiceService.ConvertToFaces(Player, diceTypes);
    }

    public (int lowMax, int mediumMax, int highMin) GetPlayerTierBoundaries(int maxValue, DiceStatType statType, DiceRollType rollType)
    {
        return DiceService.GetTierBoundaries(maxValue, Player.Level, Enemy.Level, statType, rollType);
    }

    private void Resolve()
    {
        ActionInstance attack;
        ActionInstance defense;
        Battler attacker;
        Battler target;

        if (PlayerIsAttacker)
        {
            attack = PendingPlayerAction;
            defense = PendingEnemyAction;
            attacker = Player;
            target = Enemy;
        }
        else
        {
            attack = PendingEnemyAction;
            defense = PendingPlayerAction;
            attacker = Enemy;
            target = Player;
        }

        ActionResolutionResult result = Resolver.Resolve(attack, defense, attacker, target);
        Debug.Log($"[Resolve] Outcome: {result.Outcome} | Damage: {result.Damage} | Feedback: {result.FeedbackText}");
        View.ShowAttackEffect(PlayerIsAttacker);

        if (result.AppliesDamage)
        {
            Debug.Log($"[Resolve] Applying {result.Damage} damage to {result.FinalTarget.Name}");
            result.FinalTarget.ReceiveDamage(result.Damage);
        }

        View.ShowResolveFeedback(result, PlayerIsAttacker == false);

        Debug.Log($"[HP] Player: {Player.HP} | Enemy: {Enemy.HP}");

        View.UpdateView(Player, Enemy);
    }

    private bool ResolveAttackAccuracy()
    {
        ActionInstance attack = PlayerIsAttacker ? PendingPlayerAction : PendingEnemyAction;
        return attack != null && attack.AccuracyDice != null && attack.AccuracyDice.Tier != DiceTier.Low;
    }

    public bool ResolveDefenseAccuracy()
    {
        ActionInstance defense = PlayerIsAttacker ? PendingEnemyAction : PendingPlayerAction;
        return defense != null && defense.AccuracyDice != null && defense.AccuracyDice.Tier != DiceTier.Low;
    }

    private void EndTurn()
    {
        if (CombatEnded)
            return;

        Player.RecoverDices(1);
        Enemy.RecoverDices(1);
        View.UpdateView(Player, Enemy);
        PlayerIsAttacker = !PlayerIsAttacker;

        UpdateTurnRoleUI();
    }

    private void UpdateTurnRoleUI()
    {
        ActionType allowedAction = PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        View.UpdateTurnOwner(PlayerIsAttacker);
        Input.SetAllowedAction(allowedAction);
        View.ActionPanel.SetPlayerRoleButtons(PlayerIsAttacker);
    }

    public void SetEnemyVisual(EnemyInstance enemySnapshot = null)
    {
        Sprite enemySprite = enemySnapshot != null && enemySnapshot.source != null
            ? enemySnapshot.source.image
            : null;

        GameObject enemyBattler = GameObject.Find("EnemyBattler");
        if (enemyBattler == null)
        {
            Debug.LogWarning("[Combat] EnemyBattler GameObject not found.");
            return;
        }

        Transform visualTransform = enemyBattler.transform.Find("EnemyVisual");
        if (visualTransform == null)
        {
            Debug.LogWarning("[Combat] EnemyVisual transform not found.");
            return;
        }

        if (visualTransform.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.sprite = enemySprite;
        }
        else
        {
            Debug.LogWarning("[Combat] Could not find SpriteRenderer to set enemy image.");
        }
    }

    private ActionDefinition BuildDefinitionFromBattler(Battler battler, ActionType actionType)
    {
        int basePower = actionType == ActionType.Attack ? battler.Attack : battler.Defense;
        string id = actionType == ActionType.Attack ? "attack" : "defense";
        return new ActionDefinition(id, actionType, basePower);
    }

    private bool TryHandleCombatEnd()
    {
        if (Player.IsAlive() && Enemy.IsAlive())
            return false;

        CombatEnded = true;
        View.SetCombatInputEnabled(false);

        bool playerWon = Player.IsAlive() && !Enemy.IsAlive();
        if (playerWon)
        {
            lastGrantedXp = GrantXpRewardIfEligible();
            if (SessionData?.EnemyInstance?.source != null)
            {
                int grantedGoldCoins = GrantGoldCoinsReward();
                lastGrantedItens = RewardService.GetRandomLoot(Enemy.Level, grantedGoldCoins);
            }
            else
            {
                lastGrantedItens = new Dictionary<ItemSO, int>();
            }
            View.CombatEndView.ShowVictory(lastGrantedXp, lastGrantedItens, ProceedToGameplayScene);
        }
        else
        {
            View.CombatEndView.ShowGameOver(RestartCombat, QuitCombat);
        }

        return true;
    }

    private void RestartCombat()
    {
        if (SessionData != null)
            CombatSessionStore.SetSession(SessionData);

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    private void QuitCombat()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    private void ProceedToGameplayScene()
    {
        CombatResultStore.SetResult(new CombatResultSnapshot
        {
            PlayerSnapshot = BuildResultPlayerSnapshot(),
            EnemyInstance = SessionData?.EnemyInstance,
            PlayerWon = true,
            XpGained = lastGrantedXp,
            ItensGained = lastGrantedItens,
        });

        CombatReturnStore.Set(new CombatReturnSnapshot
        {
            SceneName = SessionData != null ? SessionData.ReturnSceneName : gameplaySceneName,
            Level = SessionData?.ReturnLevel,
            LevelIndex = SessionData != null ? SessionData.ReturnLevelIndex : 0,
            ExploredNodes = SessionData?.ReturnExploredNodes,
            PlayerPosition = SessionData != null ? SessionData.ReturnPlayerPosition : Vector3.zero
        });

        string targetScene = SessionData != null && !string.IsNullOrWhiteSpace(SessionData.ReturnSceneName)
            ? SessionData.ReturnSceneName
            : gameplaySceneName;
        SceneManager.LoadScene(targetScene);
    }

    private PlayerStatusSnapshot BuildResultPlayerSnapshot()
    {
        if (SessionData == null)
        {
            return new PlayerStatusSnapshot
            {
                hp = Player.HP,
                heart = Player.Heart,
                mind = Player.Mind,
                body = Player.Body,
                attack = Player.Attack,
                defense = Player.Defense,
                maxHp = Player.MaxHp,
                powerDices = Player.CurrentPowerDices,
                accuracyDices = Player.CurrentAccuracyDices
            };
        }

        PlayerStatusSnapshot snapshot = SessionData.PlayerSnapshot;
        snapshot.hp = Player.HP;
        snapshot.heart = Player.Heart;
        snapshot.mind = Player.Mind;
        snapshot.body = Player.Body;
        snapshot.attack = Player.Attack;
        snapshot.defense = Player.Defense;
        snapshot.maxHp = Player.MaxHp;
        snapshot.powerDices = Player.CurrentPowerDices;
        snapshot.accuracyDices = Player.CurrentAccuracyDices;
        if (CombatPlayerInventory != null)
            snapshot.inventory = CombatPlayerInventory.GetSnapshot();
        return snapshot;
    }

    private int GrantXpRewardIfEligible()
    {
        if (Enemy.Level < Player.Level)
            return 0;

        int reward = Mathf.Max(0, Enemy.Level);
        return reward;
    }

    private int GrantGoldCoinsReward()
    {
        int minGoldCoins = 1;
        int maxGoldCoins = Mathf.Max(minGoldCoins, Enemy.Level * 10);
        return UnityEngine.Random.Range(minGoldCoins, maxGoldCoins + 1);
    }

    public int GetPlayerActionPower()
    {
        int atk = Player.Attack;
        int df = Player.Defense;
        Logger.Log($"[GetPlayerActionPower] Attack: {atk}, Defense: {df}");
        return PlayerIsAttacker ? atk : df;
    }
    
    private PlayerInventory ResolveCombatPlayerInventory()
    {
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory == null)
            return null;

        if (SessionData != null)
            inventory.RestoreSnapshot(SessionData.PlayerSnapshot.inventory);

        return inventory;
    }
}
