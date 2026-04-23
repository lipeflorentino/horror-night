using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds0_5 = new(0.5f);
    private const int DefaultDiceCount = 3;
    public CombatView View;
    public CombatInputHandler Input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public bool IsPlayerAttacker => PlayerIsAttacker;

    private DiceService DiceService;
    private ActionResolverService Resolver;
    private EnemyActionSelector EnemyActionSelector;

    private ActionDefinition AttackDef;
    private ActionDefinition DefenseDef;

    private bool PlayerIsAttacker = true;

    private ActionInstance PendingPlayerAction;
    private ActionInstance PendingEnemyAction;
    private List<DiceResult> PendingPlayerRolls = new();
    private List<DiceResult> PendingEnemyRolls = new();
    private int PendingEnemyAllocatedDice = 1;

    void Start()
    {
        DiceService = new DiceService();
        Resolver = new ActionResolverService();
        EnemyActionSelector = new EnemyActionSelector();
        AttackDef = new ActionDefinition("attack", ActionType.Attack, 0);
        DefenseDef = new ActionDefinition("defense", ActionType.Defense, 0);

        CombatSessionData sessionData = CombatSessionStore.Consume();
        InitializeBattlers(sessionData);

        Input = FindObjectOfType<CombatInputHandler>();
        View = FindObjectOfType<CombatView>();

        Input.Init(this);
        View.Init();
        View.BindInput(Input);
        View.UpdateView(Player, Enemy);

        UpdateTurnRoleUI();
    }

    private void InitializeBattlers(CombatSessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogWarning("[Combat] No CombatSessionData found. Using default battlers.");
            Player = new Battler("Player", 100, 10, 10, 10, 10, 5, DefaultDiceCount);
            Enemy = new Battler("Enemy", 100, 10, 10, 10, 10, 5, DefaultDiceCount);
            return;
        }

        PlayerStatusSnapshot playerSnapshot = sessionData.PlayerSnapshot;
        EnemyInstance enemySnapshot = sessionData.EnemyInstance;

        SetEnemyVisual(enemySnapshot ?? null);

        Player = new Battler(
            "Player",
            Mathf.RoundToInt(playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.heart),
            Mathf.RoundToInt(playerSnapshot.mind),
            Mathf.RoundToInt(playerSnapshot.body),
            Mathf.RoundToInt(playerSnapshot.attack),
            Mathf.RoundToInt(playerSnapshot.defense),
            DefaultDiceCount
        );

        if (enemySnapshot != null)
        {
            string enemyName = enemySnapshot.source != null ? enemySnapshot.source.enemyName : "Enemy";
            Enemy = new Battler(
                enemyName,
                enemySnapshot.hp,
                enemySnapshot.heart,
                enemySnapshot.mind,
                enemySnapshot.body,
                enemySnapshot.attack,
                enemySnapshot.defense,
                DefaultDiceCount
            );
        }
        else
        {
            Debug.LogWarning("[Combat] Enemy snapshot missing. Using default enemy.");
            Enemy = new Battler("Enemy", 100, 10, 10, 10, 10, 5, DefaultDiceCount);
        }

        Debug.Log($"[Combat] Session loaded. Player HP: {Player.HP} | Enemy HP: {Enemy.HP}");
    }

    public void ReceivePlayerInput(ActionType type, int allocatedDice)
    {
        ActionType expectedType = PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        if (type != expectedType)
        {
            Debug.Log($"[Input] Ignored invalid action for current role. Expected {expectedType} and received {type}");
            return;
        }

        StartCoroutine(ResolveTurnRoutine(type, allocatedDice));
    }

    private IEnumerator ResolveTurnRoutine(ActionType playerType, int allocatedDice)
    {
        yield return WaitForSeconds0_5;

        GenerateEnemyAction();

        yield return WaitForSeconds0_5;

        RollActions(playerType, allocatedDice);

        yield return View.PlayDiceResolution(PendingPlayerRolls, PendingEnemyRolls);

        yield return WaitForSeconds0_5;

        Resolve();

        yield return WaitForSeconds0_5;

        EndTurn();
    }

    private void GenerateEnemyAction()
    {
        PendingEnemyAction = EnemyActionSelector.Select(AttackDef, DefenseDef);
        PendingEnemyAllocatedDice = Mathf.Clamp(Random.Range(1, Enemy.CurrentDices + 1), 1, Mathf.Max(1, Enemy.CurrentDices));
        Enemy.CurrentDices = Mathf.Max(Enemy.CurrentDices - PendingEnemyAllocatedDice, 0);
        Debug.Log($"[AI] Enemy selected {PendingEnemyAction.Definition.Type}");
    }

    private void RollActions(ActionType playerType, int allocatedDice)
    {
        ActionDefinition playerAction = BuildDefinitionFromBattler(Player, playerType);

        int safePlayerAllocatedDice = Mathf.Max(1, allocatedDice);

        PendingPlayerRolls = DiceService.RollMany(safePlayerAllocatedDice);
        PendingEnemyRolls = DiceService.RollMany(PendingEnemyAllocatedDice);

        DiceResult playerDice = DiceService.GetBestResult(PendingPlayerRolls);
        DiceResult enemyDice = DiceService.GetBestResult(PendingEnemyRolls);

        PendingPlayerAction = new ActionInstance(playerAction, playerDice);
        PendingEnemyAction.Definition = BuildDefinitionFromBattler(Enemy, PendingEnemyAction.Definition.Type);
        PendingEnemyAction.Dice = enemyDice;

        Debug.Log($"[Flow] Player rolled best of {safePlayerAllocatedDice} dice → {playerDice.Value}");
        Debug.Log($"[Flow] Enemy rolled best of {PendingEnemyAllocatedDice} dice → {enemyDice.Value}");
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
            target.ReceiveDamage(result.Damage);
        }

        View.ShowResolveFeedback(result, PlayerIsAttacker == false);

        Debug.Log($"[HP] Player: {Player.HP} | Enemy: {Enemy.HP}");

        View.UpdateView(Player, Enemy);
    }

    private void EndTurn()
    {
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
        Transform demonTransform = enemyBattler.transform.Find("EnemyVisual");
        
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
}
