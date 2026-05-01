using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds0_5 = new(0.5f);
    private const int DefaultDiceCount = 3;
    [SerializeField] private string gameplaySceneName = "Gameplay";
    public CombatView View;
    public CombatInputHandler Input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public bool IsPlayerAttacker => PlayerIsAttacker;

    private DiceService DiceService;
    private ActionResolverService Resolver;
    private InitiativeResolver InitiativeResolver;
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
    private CombatSessionData SessionData;

    void Start()
    {
        DiceService = new DiceService();
        Resolver = new ActionResolverService();
        InitiativeResolver = new InitiativeResolver();
        EnemyActionSelector = new EnemyActionSelector();
        EnemyTurnPlanner = new EnemyTurnPlanner(EnemyActionSelector);
        AttackDef = new ActionDefinition("attack", ActionType.Attack, 0);
        DefenseDef = new ActionDefinition("defense", ActionType.Defense, 0);

        SessionData = CombatSessionStore.Consume();
        InitializeBattlers(SessionData);
        DefineStartingTurnByInitiative();

        Input = FindObjectOfType<CombatInputHandler>();
        View = FindObjectOfType<CombatView>();

        Input.Init(this);
        View.Init();
        View.BindInput(Input);
        View.UpdateView(Player, Enemy);

        UpdateTurnRoleUI();
    }

    private void DefineStartingTurnByInitiative()
    {
        Battler firstBattler = InitiativeResolver.ResolveStartingBattler(Player, Enemy);
        PlayerIsAttacker = firstBattler == Player;
    }

    private void InitializeBattlers(CombatSessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogWarning("[Combat] No CombatSessionData found. Using default battlers.");
            Player = new Battler("Player", 1, 100, 10, 10, 10, 10, 5, 5, DefaultDiceCount);
            Enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, DefaultDiceCount);
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
            DefaultDiceCount
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
                DefaultDiceCount
            );
        }
        else
        {
            Debug.LogWarning("[Combat] Enemy snapshot missing. Using default enemy.");
            Enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, DefaultDiceCount);
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

        StartCoroutine(ResolveTurnRoutine(type, powerDiceTypes, accuracyDiceTypes));
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

    private IEnumerator ResolveTurnRoutine(ActionType playerType, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        yield return WaitForSeconds0_5;
        yield return ResolveTurnFlow(playerType, powerDiceTypes, accuracyDiceTypes);
    }

    private IEnumerator ResolveTurnFlow(ActionType playerType, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        GenerateEnemyAction();
        yield return WaitForSeconds0_5;
        RollActions(playerType, powerDiceTypes, accuracyDiceTypes);
        yield return View.PlayDiceResolution(
            PendingPlayerPowerRolls,
            PendingPlayerAccuracyRolls,
            PendingEnemyPowerRolls,
            PendingEnemyAccuracyRolls
        );
        yield return WaitForSeconds0_5;

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
        Debug.Log($"[AI] Enemy selected {PendingEnemyAction.Definition.Type}");
    }

    private void RollActions(ActionType playerType, IReadOnlyList<DiceStatType> powerDiceTypes, IReadOnlyList<DiceStatType> accuracyDiceTypes)
    {
        ActionDefinition playerAction = BuildDefinitionFromBattler(Player, playerType);

        List<int> powerDiceFaces = DiceService.ConvertToFaces(Player, powerDiceTypes);
        List<int> accuracyDiceFaces = DiceService.ConvertToFaces(Player, accuracyDiceTypes);
        List<int> enemyPowerDiceFaces = DiceService.ConvertToFaces(Enemy, PendingEnemyPowerDiceTypes);
        List<int> enemyAccuracyDiceFaces = DiceService.ConvertToFaces(Enemy, PendingEnemyAccuracyDiceTypes);

        PendingPlayerPowerRolls = DiceService.RollMany(powerDiceFaces, Player.Level, Enemy.Level);
        PendingPlayerAccuracyRolls = DiceService.RollMany(accuracyDiceFaces, Player.Level, Enemy.Level);
        PendingEnemyPowerRolls = DiceService.RollMany(enemyPowerDiceFaces, Enemy.Level, Player.Level);
        PendingEnemyAccuracyRolls = DiceService.RollMany(enemyAccuracyDiceFaces, Enemy.Level, Player.Level);

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
        View.ShowAttackEffect(PlayerIsAttacker);

        if (result.AppliesDamage)
        {
            result.FinalTarget.ReceiveDamage(result.Damage);
        }

        View.ShowResolveFeedback(result, PlayerIsAttacker == false);

        Debug.Log($"[HP] Player: {Player.HP} | Enemy: {Enemy.HP}");

        View.UpdateView(Player, Enemy);
    }

    private void EndTurn()
    {
        if (CombatEnded)
            return;

        Player.RecoverDice(1);
        Enemy.RecoverDice(1);
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

        Transform demonTransform = enemyBattler.transform.Find("EnemyVisual");
        if (demonTransform == null)
        {
            Debug.LogWarning("[Combat] EnemyVisual transform not found.");
            return;
        }

        if (demonTransform.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
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
            View.CombatEndView.ShowVictory(ProceedToGameplayScene);
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
            PlayerWon = true
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
                defense = Player.Defense
            };
        }

        PlayerStatusSnapshot snapshot = SessionData.PlayerSnapshot;
        snapshot.hp = Player.HP;
        snapshot.heart = Player.Heart;
        snapshot.mind = Player.Mind;
        snapshot.body = Player.Body;
        snapshot.attack = Player.Attack;
        snapshot.defense = Player.Defense;
        return snapshot;
    }
}
