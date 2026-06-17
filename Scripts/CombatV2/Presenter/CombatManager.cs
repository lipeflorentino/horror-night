using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds0_5 = new(0.5f);
    private const int DefaultPowerDiceCount = 3;
    private const int DefaultAccuracyDiceCount = 3;
    private const int CoreStatCap = 20;
    [SerializeField] private string gameplaySceneName = "Gameplay";
    public CombatView View;
    public CombatInputHandler Input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public bool IsPlayerAttacker => PlayerIsAttacker;

    private DiceService DiceService;
    private BattlerStateService BattlerStateService;
    private PerkService PerkService;
    private TrickService TrickService;
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
    private TrickInventoryInputHandler TrickInventoryInputHandler;
    private ICombatInventory CombatPlayerInventory;
    private ITrickInventory PlayerTrickInventory;
    private ITrickInventory EnemyTrickInventory;
    [SerializeField] private EnemyVisuals EnemyVisuals;

    void Start()
    {
        BattlerStateService = new BattlerStateService();
        PerkService = new PerkService();
        TrickService = new TrickService(PerkService);
        DiceService = new DiceService(BattlerStateService, PerkService);
        Resolver = new ActionResolverService(PerkService);
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
        TrickInventoryInputHandler = FindObjectOfType<TrickInventoryInputHandler>();
        
        if (TrickInventoryInputHandler == null && FindObjectOfType<TrickInventoryView>() != null)
        {
            TrickInventoryInputHandler = gameObject.AddComponent<TrickInventoryInputHandler>();
        }
        
        Input = FindObjectOfType<CombatInputHandler>();
        View = FindObjectOfType<CombatView>();
        
        CombatPlayerInventory = BuildCombatInventory(SessionData);
        PlayerTrickInventory = BuildPlayerTrickInventory(Player);
        EnemyTrickInventory = BuildEnemyTrickInventory(Enemy);
        
        ActivatePlayerIdentityTricks();
        ActivateEnemyIdentityTricks();

        if (InventoryInputHandler != null)
            InventoryInputHandler.Init(this, CombatPlayerInventory);

        if (TrickInventoryInputHandler != null)
        {
            TrickInventoryInputHandler.Init(this, PlayerTrickInventory);
        }

        Input.Init(this);
        View.Init();
        View.BindInput(Input);
        View.BindPlayerTricks(Player, TrickService, PlayerTrickInventory);
        
        RefreshCombatUI();
        UpdateTurnRoleUI();
    }

    private void InitializeBattlers(CombatSessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogWarning("[Combat] No CombatSessionData found. Using default battlers.");
            Player = new Battler("Player", 1, 20, 10, 10, 10, 10, 5, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, true);
            Enemy = new Battler("Enemy", 1, 20, 10, 10, 10, 6, 3, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, false);
            return;
        }

        PlayerStatusSnapshot playerSnapshot = sessionData.PlayerSnapshot;
        EnemyInstance enemySnapshot = sessionData.EnemyInstance;
        EnemyVisuals = FindObjectOfType<EnemyVisuals>();
        EnemyVisuals.SetEnemyVisual(enemySnapshot ?? null);

        Player = new Battler(
            string.IsNullOrWhiteSpace(playerSnapshot.characterName) ? "Player" : playerSnapshot.characterName,
            Mathf.Max(1, playerSnapshot.level),
            Mathf.RoundToInt(playerSnapshot.hp),
            ClampCoreStat(playerSnapshot.heart),
            ClampCoreStat(playerSnapshot.mind),
            ClampCoreStat(playerSnapshot.body),
            Mathf.RoundToInt(playerSnapshot.attack),
            Mathf.RoundToInt(playerSnapshot.defense),
            Mathf.RoundToInt(playerSnapshot.initiative),
            Mathf.Max(1, playerSnapshot.maxPowerDices > 0 ? playerSnapshot.maxPowerDices : DefaultPowerDiceCount),
            Mathf.Max(1, playerSnapshot.maxAccuracyDices > 0 ? playerSnapshot.maxAccuracyDices : DefaultAccuracyDiceCount),
            true,
            Mathf.RoundToInt(playerSnapshot.maxHp > 0 ? playerSnapshot.maxHp : playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.focus),
            Mathf.RoundToInt(playerSnapshot.strength),
            Mathf.RoundToInt(playerSnapshot.agility)
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
                false,
                -1,
                enemySnapshot.focus,
                enemySnapshot.strength,
                enemySnapshot.agility
            );
        }
        else
        {
            Debug.LogWarning("[Combat] Enemy snapshot missing. Using default enemy.");
            Enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, DefaultPowerDiceCount, DefaultAccuracyDiceCount, false);
        }
    }

    public void RefreshCombatUI()
    {
        View.UpdateView(Player, Enemy);
        Input.RefreshDiceAllocationUI();
    }

    public BattlerStateService GetBattlerStateService()
    {
        return BattlerStateService;
    }

    public int GetEffectivePlayerActionPower()
    {
        ActionType actionType = PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        return BattlerStateService.GetEffectiveActionPower(Player, Enemy, actionType);
    }

    public CombatRollContext BuildPlayerRollContext(int maxValue, DiceStatType statType, DiceRollType rollType)
    {
        ActionType actionType = PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        int focus = BattlerStateService.GetEffectiveFocus(Player, Enemy, actionType);
        int strength = BattlerStateService.GetEffectiveStrength(Player, Enemy, actionType);
        return new CombatRollContext(Player, Enemy, actionType, rollType, statType, Player.Level, Enemy.Level, focus, strength, maxValue);
    }

    private void DefineStartingTurnByInitiative()
    {
        Battler firstBattler = InitiativeResolverService.ResolveStartingBattler(Player, Enemy);
        PlayerIsAttacker = firstBattler == Player;
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

    // TODO: keep string entry point for legacy UI and UnityEvents.
    public void ReceivePlayerSelectTrick(string trickId)
    {
        TryCastPlayerTrick(trickId);
    }

    public bool TryCastPlayerTrick(string trickId)
    {
        if (CombatEnded || string.IsNullOrWhiteSpace(trickId))
            return false;

        bool casted = TrickService.TryCastTrick(Player, PlayerTrickInventory, trickId, null);
        RefreshCombatUI();
        return casted;
    }

    public bool TryCastPlayerTrick(TrickSO trick)
    {
        if (CombatEnded || trick == null)
            return false;

        bool casted = TrickService.TryCastTrick(Player, PlayerTrickInventory, trick, null);
        RefreshCombatUI();
        return casted;
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
        ActionDefinition playerAction = BuildDefinitionFromBattler(Player, Enemy, action);
        ActionType enemyActionType = PendingEnemyAction.Definition.Type;

        PendingPlayerPowerRolls = DiceService.RollMany(Player, Enemy, powerDiceTypes, action, DiceRollType.Power, Player.Level, Enemy.Level);
        PendingPlayerAccuracyRolls = DiceService.RollMany(Player, Enemy, accuracyDiceTypes, action, DiceRollType.Accuracy, Player.Level, Enemy.Level);
        PendingEnemyPowerRolls = DiceService.RollMany(Enemy, Player, PendingEnemyPowerDiceTypes, enemyActionType, DiceRollType.Power, Enemy.Level, Player.Level);
        PendingEnemyAccuracyRolls = DiceService.RollMany(Enemy, Player, PendingEnemyAccuracyDiceTypes, enemyActionType, DiceRollType.Accuracy, Enemy.Level, Player.Level);

        DiceResult playerPowerDice = DiceService.GetBestResult(PendingPlayerPowerRolls);
        DiceResult playerAccuracyDice = DiceService.GetBestResult(PendingPlayerAccuracyRolls);
        DiceResult enemyPowerDice = DiceService.GetBestResult(PendingEnemyPowerRolls);
        DiceResult enemyAccuracyDice = DiceService.GetBestResult(PendingEnemyAccuracyRolls);

        PendingPlayerAction = new ActionInstance(playerAction, playerPowerDice, playerAccuracyDice);
        PendingEnemyAction.Definition = BuildDefinitionFromBattler(Enemy, Player, PendingEnemyAction.Definition.Type);
        PendingEnemyAction = new ActionInstance(PendingEnemyAction.Definition, enemyPowerDice, enemyAccuracyDice);

        Debug.Log($"[Flow] Player rolled POWER best:{playerPowerDice.Value} | ACCURACY best:{playerAccuracyDice.Value} using {PendingPlayerPowerRolls.Count + PendingPlayerAccuracyRolls.Count} dice.");
        Debug.Log($"[Flow] Enemy rolled POWER best:{enemyPowerDice.Value} | ACCURACY best:{enemyAccuracyDice.Value} using {PendingEnemyPowerRolls.Count + PendingEnemyAccuracyRolls.Count} dice.");
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
        BattlerStateService.TickTurnEnd(Player);
        BattlerStateService.TickTurnEnd(Enemy);
        PerkService.TickTurnEnd(Player);
        PerkService.TickTurnEnd(Enemy);
        TrickService.TickTrickEnd(Player, PlayerTrickInventory);
        TrickService.TickTrickEnd(Enemy, EnemyTrickInventory);
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

    private ActionDefinition BuildDefinitionFromBattler(Battler battler, Battler opponent, ActionType actionType)
    {
        int basePower = BattlerStateService.GetEffectiveActionPower(battler, opponent, actionType);
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
            lastGrantedXp = RewardService.GrantXpRewardIfEligible(Enemy.Level, Player.Level);
            if (SessionData?.EnemyInstance?.source != null)
            {
                lastGrantedItens = RewardService.GetRandomLoot(Enemy.Level);
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
        CombatSessionStore.Clear();
        CombatResultStore.Clear();
        CombatReturnStore.Clear();
        SceneManager.LoadScene(gameplaySceneName);
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
                heart = ClampCoreStat(Player.Heart),
                mind = ClampCoreStat(Player.Mind),
                body = ClampCoreStat(Player.Body),
                attack = Player.Attack,
                defense = Player.Defense,
                initiative = Player.Initiative,
                focus = Player.Focus,
                strength = Player.Strength,
                agility = Player.Agility,
                maxHeart = ClampCoreStat(Player.Heart),
                maxMind = ClampCoreStat(Player.Mind),
                maxBody = ClampCoreStat(Player.Body),
                maxHp = Player.MaxHp,
                powerDices = Player.CurrentPowerDices,
                accuracyDices = Player.CurrentAccuracyDices,
                trickInventory = PlayerTrickInventory != null
                    ? TrickInventorySnapshot.CreatePersistentSnapshot(PlayerTrickInventory.GetSnapshot())
                    : new TrickInventorySnapshot()
            };
        }

        PlayerStatusSnapshot snapshot = SessionData.PlayerSnapshot;
        snapshot.hp = Player.HP;
        snapshot.heart = ClampCoreStat(Player.Heart);
        snapshot.mind = ClampCoreStat(Player.Mind);
        snapshot.body = ClampCoreStat(Player.Body);
        snapshot.attack = Player.Attack;
        snapshot.defense = Player.Defense;
        snapshot.initiative = Player.Initiative;
        snapshot.focus = Player.Focus;
        snapshot.strength = Player.Strength;
        snapshot.agility = Player.Agility;
        snapshot.maxHeart = ClampCoreStat(Mathf.Max(snapshot.maxHeart, snapshot.heart));
        snapshot.maxMind = ClampCoreStat(Mathf.Max(snapshot.maxMind, snapshot.mind));
        snapshot.maxBody = ClampCoreStat(Mathf.Max(snapshot.maxBody, snapshot.body));
        snapshot.maxHp = Player.MaxHp;
        snapshot.powerDices = Player.CurrentPowerDices;
        snapshot.accuracyDices = Player.CurrentAccuracyDices;
        if (CombatPlayerInventory != null)
            snapshot.inventory = CombatPlayerInventory.GetSnapshot();
        if (PlayerTrickInventory != null)
            snapshot.trickInventory = TrickInventorySnapshot.CreatePersistentSnapshot(PlayerTrickInventory.GetSnapshot());
        return snapshot;
    }
    
    private static int ClampCoreStat(float value)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value), 0, CoreStatCap);
    }

    private void ActivatePlayerIdentityTricks()
    {   
        if (Player == null || PlayerTrickInventory?.IdentitySlots == null)
        {
            return;
        }

        int activatedCount = 0;
        for (int i = 0; i < PlayerTrickInventory.IdentitySlots.Count; i++)
        {
            TrickRuntimeInstance instance = PlayerTrickInventory.IdentitySlots[i]?.RuntimeInstance;
            if (instance?.Definition != null)
            {
                TrickService.ApplyTrick(Player, instance, Player);
                activatedCount++;
            }
        }
    }

    private void ActivateEnemyIdentityTricks()
    {   
        if (Enemy == null || EnemyTrickInventory?.IdentitySlots == null)
        {
            return;
        }

        // Logger.Log($"[CombatManager] ActivateEnemyIdentityTricks: Total de identity slots: {EnemyTrickInventory.IdentitySlots.Count}");
        
        int activatedCount = 0;
        for (int i = 0; i < EnemyTrickInventory.IdentitySlots.Count; i++)
        {
            TrickRuntimeInstance instance = EnemyTrickInventory.IdentitySlots[i]?.RuntimeInstance;
            if (instance?.Definition != null)
            {
                // Logger.Log($"[CombatManager] ActivateEnemyIdentityTricks[{i}]: Ativando '{instance.Definition.DisplayName}' (ID: {instance.Definition.Id})");
                TrickService.ApplyTrick(Enemy, instance, Enemy);
                activatedCount++;
            }
            else
            {
                // Logger.Log($"[CombatManager] ActivateEnemyIdentityTricks[{i}]: Slot vazio ou sem definição.");
            }
        }
        
        // Logger.Log($"[CombatManager] ActivateEnemyIdentityTricks: Conclusão. Identity tricks ativadas: {activatedCount}/{EnemyTrickInventory.IdentitySlots.Count}");
    }

    private ITrickInventory BuildPlayerTrickInventory(Battler owner)
    {   
        TrickDatabase trickDatabase = TrickDatabase.GetOrCreateRuntimeDatabase();
        TrickInventorySnapshot snapshot = SessionData != null ? SessionData.PlayerSnapshot.trickInventory : null;
        TrickInventory trickInventory = new(owner, trickDatabase, snapshot, TrickInventory.DefaultIdentitySlotCount, TrickInventory.DefaultCastedSlotCount, PerkService);

        return trickInventory;
    }

    private ITrickInventory BuildEnemyTrickInventory(Battler owner)
    {   
        TrickDatabase trickDatabase = TrickDatabase.GetOrCreateRuntimeDatabase();
        TrickInventorySnapshot snapshot = null;

        if (SessionData?.EnemyInstance?.source != null)
        {
            snapshot = SessionData.EnemyInstance.source.GetTrickInventorySnapshot();
            // Logger.Log($"[CombatManager] BuildEnemyTrickInventory: Snapshot obtido do EnemySO. Tricks aprendidas: {snapshot.learnedTrickIds.Count}.");
        }

        TrickInventory trickInventory = new(owner, trickDatabase, snapshot, TrickInventory.DefaultIdentitySlotCount, TrickInventory.DefaultCastedSlotCount, PerkService);
        
        return trickInventory;
    }

    private ICombatInventory BuildCombatInventory(CombatSessionData sessionData)
    {
        ItemDatabase itemDatabase = FindObjectOfType<ItemDatabase>();
        PlayerInventorySnapshot snapshot = null;
        if (sessionData?.PlayerSnapshot != null)
            snapshot = sessionData.PlayerSnapshot.inventory;

        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (snapshot == null && inventory != null)
            snapshot = inventory.GetSnapshot();

        if (itemDatabase != null)
            return new CombatInventory(Player, itemDatabase, snapshot ?? new PlayerInventorySnapshot());

        if (inventory != null && snapshot != null)
            inventory.RestoreSnapshot(snapshot);

        return inventory;
    }

    public DiceService GetDiceService()
    {
        return DiceService;
    }
}
