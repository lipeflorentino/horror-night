using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CombatManager : MonoBehaviour
{
    [Header("Settings")]
    [FormerlySerializedAs("DefaultPowerDiceCount")]
    [SerializeField] private int DefaultDiceCount = 3;
    [Tooltip("Maximum value for Heart, Mind, and Body stats")]
    [SerializeField] private int CoreStatCap = 20;
    [SerializeField] private string GameplaySceneName = "Gameplay";
    private static readonly WaitForSeconds WaitForSeconds0_5 = new(0.5f);
    private static readonly WaitForSeconds WaitForSeconds2 = new(2f);

    [Header("References and UI")]
    public CombatView View;
    public CombatInputHandler Input;

    // =========================
    // Battlers and combat state
    // =========================
    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public Sprite PlayerIcon { get; private set; }
    public Sprite EnemyIcon { get; private set; }
    
    // Facades/Properties delegation to state
    public CombatTurnContext TurnState { get; private set; } = new();
    public bool IsPlayerAttacker => TurnState.PlayerIsAttacker;
    public bool CombatEnded => TurnState.CombatEnded;
    private CombatSessionData SessionData;

    // =========================
    // Services
    // =========================
    private DiceService DiceService;
    private PerkService PerkService;
    private TrickService TrickService;
    private ActionResolverService Resolver;
    private InitiativeResolverService InitiativeResolverService;
    private EnemyActionSelector EnemyActionSelector;
    private EnemyTurnPlanner EnemyTurnPlanner;
    private RewardService RewardService;

    // =========================
    // Action definitions
    // =========================
    private ActionDefinition AttackDef;
    private ActionDefinition DefenseDef;

    // =========================
    // Inventory and trick state
    // =========================
    private InventoryInputHandler InventoryInputHandler;
    private TrickInventoryInputHandler TrickInventoryInputHandler;
    private ICombatInventory CombatPlayerInventory;
    public ITrickInventory PlayerTrickInventory { get; private set; }
    private ITrickInventory EnemyTrickInventory;

    // =========================
    // Lifecycle and initialization
    // =========================
    void Start()
    {
        PerkService = new PerkService();
        TrickService = new TrickService(PerkService);
        DiceService = new DiceService(PerkService);
        Resolver = new ActionResolverService(PerkService);
        InitiativeResolverService = new InitiativeResolverService();
        EnemyActionSelector = new EnemyActionSelector();
        EnemyTurnPlanner = new EnemyTurnPlanner(EnemyActionSelector);
        RewardService = new RewardService();
        AttackDef = new ActionDefinition("attack", ActionType.Attack, 0);
        DefenseDef = new ActionDefinition("defense", ActionType.Defense, 0);
        SessionData = CombatSessionStore.Consume();

        var (player, enemy, playerIcon, enemyIcon) = CombatInitializer.InitializeBattlers(SessionData, DefaultDiceCount, CoreStatCap);
        Player = player;
        Enemy = enemy;
        PlayerIcon = playerIcon;
        EnemyIcon = enemyIcon;

        // TODO: Remove this line after testing, it's just to ensure the player has a trick for testing purposes.
        player.AddMomentum(6);

        TurnManager.DefineStartingTurnByInitiative(Player, Enemy, InitiativeResolverService, TurnState);

        InventoryInputHandler = FindObjectOfType<InventoryInputHandler>();
        TrickInventoryInputHandler = FindObjectOfType<TrickInventoryInputHandler>();
        
        if (TrickInventoryInputHandler == null && FindObjectOfType<TrickInventoryView>() != null)
        {
            TrickInventoryInputHandler = gameObject.AddComponent<TrickInventoryInputHandler>();
        }
        
        Input = FindObjectOfType<CombatInputHandler>();
        View = FindObjectOfType<CombatView>();
        
        CombatPlayerInventory = CombatInventoryInitializer.BuildCombatInventory(SessionData, Player);
        PlayerTrickInventory = CombatInventoryInitializer.BuildPlayerTrickInventory(Player, SessionData, PerkService);
        EnemyTrickInventory = CombatInventoryInitializer.BuildEnemyTrickInventory(Enemy, SessionData, PerkService);
        
        CombatInventoryInitializer.ActivatePlayerIdentityTricks(Player, PlayerTrickInventory, TrickService);
        CombatInventoryInitializer.ActivateEnemyIdentityTricks(Enemy, EnemyTrickInventory, TrickService);

        if (InventoryInputHandler != null)
            InventoryInputHandler.Init(this, CombatPlayerInventory);

        if (TrickInventoryInputHandler != null)
        {
            TrickInventoryInputHandler.Init(this, PlayerTrickInventory);
        }

        Input.Init(this);
        View.Init(this);
        View.BindInput(Input);
        
        RefreshCombatUI();
        TurnManager.UpdateTurnRoleUI(TurnState, View, Input);
        CombatRules.SetPlayerStrategy(CombatRules.ThresholdStrategy.Balanced);
    }

    // =========================
    // UI and view refresh
    // =========================
    public void RefreshCombatUI()
    {
        View.UpdateView(Player, Enemy);
        Input.RefreshDiceAllocationUI();
    }

    // =========================
    // Input and event handling
    // =========================
    public void ReceivePlayerInput(ActionType type, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        if (!TurnManager.CanReceivePlayerInput(type, TurnState, out string rejectionReason))
        {
            if (rejectionReason != null)
                Logger.Log(rejectionReason);
            return;
        }
        
        Dictionary<DiceStatType, int> diceCosts = CombatDiceRollManager.ApplyStatDiceCost(Player, Enemy, powerDiceTypes, accuracyDiceTypes, TurnState);
        View.ShowDiceCostFeedback(diceCosts);
        RefreshCombatUI();

        StartCoroutine(ResolveTurnFlow(type, powerDiceTypes, accuracyDiceTypes));
    }

    public void ReceivePlayerSkipTurn()
    {
        if (!TurnManager.CanReceivePlayerSkipTurn(TurnState))
            return;

        StartCoroutine(SkipTurnRoutine());
    }

    public bool TryCastPlayerTrick(TrickSO trick)
    {
        if (TurnState.CombatEnded || trick == null)
            return false;

        bool casted = TrickService.TryCastTrick(Player, PlayerTrickInventory, trick, null);
        if (casted) View.ShowCombatLog($"[Trick] <color=yellow>{trick.name}</color> cast by <color=blue>{Player.Name}</color>");
        RefreshCombatUI();
        return casted;
    }

    public void ExecuteManualTrickActivation(TrickRuntimeInstance instance)
    {
        Logger.Log($"[CombatManager] Attempting manual activation of trick: {instance?.Definition?.name ?? "null"}");
        
        if (TurnState.CombatEnded || instance == null || instance.Owner == null || PerkService == null)
            return;

        // Verificação de segurança baseada na nova propriedade
        if (!instance.IsReadyToTrigger) 
            return;

        PerkService.ExecuteManualActivation(instance.Owner, instance);
        
        // Dispara log visualmente rico utilizando o ícone da habilidade
        string displayName = string.IsNullOrWhiteSpace(instance.Definition.DisplayName) ? instance.Definition.Id : instance.Definition.DisplayName;
        View.ShowCombatLog($"<color=white>{displayName}</color> ativado!", instance.Definition.Icon);
        
        RefreshCombatUI();
        
        // Força a atualização da barra de Tricks para renderizar cooldowns/consumo imediatamente
        View.RefreshActiveTricks(); 
    }

    // =========================
    // Turn flow and combat resolution
    // =========================
    private IEnumerator SkipTurnRoutine()
    {
        yield return WaitForSeconds0_5;
        if (!TurnState.PlayerIsAttacker)
            yield break;

        View.ShowSkipTurnFeedback(true);
        yield return WaitForSeconds0_5;
        EndTurn();
    }

    private IEnumerator ResolveTurnFlow(ActionType action, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        yield return WaitForSeconds0_5;

        TurnManager.GenerateEnemyAction(Enemy, SessionData, EnemyTurnPlanner, AttackDef, DefenseDef, TurnState);

        yield return WaitForSeconds2;

        CombatDiceRollManager.RollActions(Player, Enemy, action, powerDiceTypes, accuracyDiceTypes, DiceService, PerkService, TurnState);

        bool attackerAccuracyEffective = CombatResolutionManager.ResolveAttackAccuracy(TurnState);
        bool defenseAccuracyEffective = CombatResolutionManager.ResolveDefenseAccuracy(TurnState);
        bool isPlayerDefending = action == ActionType.Defense;

        DiceResult bestAccuracy = DiceService.GetBestResult(TurnState.PendingPlayerAccuracyRolls);
        var accuracyBoundaries = bestAccuracy != null 
            ? CombatDiceRollManager.GetPlayerTierBoundaries(Player, Enemy, bestAccuracy.MaxValue, bestAccuracy.StatType, DiceRollType.Accuracy, DiceService, PerkService, TurnState)
            : (1, 1, 1, 1);

        yield return View.PlayDiceResolution(TurnState.PendingPlayerAccuracyRolls, TurnState.PendingEnemyAccuracyRolls, DiceRollType.Accuracy, accuracyBoundaries);

        bool shouldPlayPowerResolution = attackerAccuracyEffective && (!isPlayerDefending || defenseAccuracyEffective);

        if (shouldPlayPowerResolution)
        {
            List<DiceResult> playerRolls = isPlayerDefending && !defenseAccuracyEffective ? null : TurnState.PendingPlayerPowerRolls;
            DiceResult bestPower = DiceService.GetBestResult(TurnState.PendingPlayerPowerRolls);
            var powerBoundaries = bestPower != null
                ? CombatDiceRollManager.GetPlayerTierBoundaries(Player, Enemy, bestPower.MaxValue, bestPower.StatType, DiceRollType.Power, DiceService, PerkService, TurnState)
                : (1, 1, 1, 1);

            yield return View.PlayDiceResolution(playerRolls, TurnState.PendingEnemyPowerRolls, DiceRollType.Power, powerBoundaries);
        }

        CombatResolutionManager.Resolve(Resolver, TurnState, View, Player, Enemy);

        yield return WaitForSeconds2;

        if (TryHandleCombatEnd())
            yield break;

        EndTurn();
    }

    private void EndTurn()
    {
        TurnManager.EndTurn(Player, Enemy, PerkService, TrickService, PlayerTrickInventory, EnemyTrickInventory, TurnState);
        RefreshCombatUI();
        if (TryHandleCombatEnd())
            return;
        TurnManager.UpdateTurnRoleUI(TurnState, View, Input);
    }

    // =========================
    // Combat end and scene flow
    // =========================
    private bool TryHandleCombatEnd()
    {
        return CombatEndService.TryHandleCombatEnd(
            Player, Enemy, TurnState, View, RewardService, SessionData, 
            ProceedToGameplayScene, RestartCombat, QuitCombat);
    }

    private void RestartCombat()
    {
        CombatEndService.RestartCombat(GameplaySceneName);
    }

    private void QuitCombat()
    {
        CombatEndService.QuitCombat();
    }

    private void ProceedToGameplayScene()
    {
        CombatEndService.ProceedToGameplayScene(
            Player, PlayerTrickInventory, CombatPlayerInventory, SessionData, TurnState, CoreStatCap, GameplaySceneName);
    }

    // =========================
    // Service accessors and helpers
    // =========================
    public int GetEffectivePlayerActionPower()
    {
        ActionType actionType = TurnState.PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        return PerkService.GetEffectiveActionPower(Player, Enemy, actionType);
    }

    public (int lowMax, int mediumMax, int highMin, int maxValue) GetPlayerTierBoundaries(int maxValue, DiceStatType statType, DiceRollType rollType, int allocatedDiceCount = 1)
    {
        return CombatDiceRollManager.GetPlayerTierBoundaries(Player, Enemy, maxValue, statType, rollType, DiceService, PerkService, TurnState, allocatedDiceCount);
    }

    public DiceService GetDiceService() => DiceService;
    public PerkService GetPerkService() => PerkService;
    public TrickService GetTrickService() => TrickService;
}
