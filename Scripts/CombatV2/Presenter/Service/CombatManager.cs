using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    private const int DefaultDiceCount = 3;
    public CombatView view;
    public CombatInputHandler input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }
    public bool IsPlayerAttacker => _playerIsAttacker;

    private DiceService _diceService;
    private ActionResolverService _resolver;
    private EnemyActionSelector _enemyActionSelector;

    private ActionDefinition _attackDef;
    private ActionDefinition _defenseDef;

    private bool _playerIsAttacker = true;

    private ActionInstance _pendingPlayerAction;
    private ActionInstance _pendingEnemyAction;

    void Start()
    {
        _diceService = new DiceService();
        _resolver = new ActionResolverService();
        _enemyActionSelector = new EnemyActionSelector();

        CombatSessionData sessionData = CombatSessionStore.Consume();
        InitializeBattlers(sessionData);

        _attackDef = new ActionDefinition("attack", ActionType.Attack, 10);
        _defenseDef = new ActionDefinition("defense", ActionType.Defense, 8);

        input.Init(this);

        view.UpdateView(Player, Enemy);
        UpdateTurnRoleUI();
    }

    private void InitializeBattlers(CombatSessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogWarning("[Combat] No CombatSessionData found. Using default battlers.");
            Player = new Battler("Player", 100, 10, 10, 10, DefaultDiceCount);
            Enemy = new Battler("Enemy", 100, 10, 10, 10, DefaultDiceCount);
            return;
        }

        PlayerStatusSnapshot playerSnapshot = sessionData.playerSnapshot;
        EnemyInstance enemySnapshot = sessionData.enemyInstance;

        Player = new Battler(
            "Player",
            Mathf.RoundToInt(playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.heart),
            Mathf.RoundToInt(playerSnapshot.mind),
            Mathf.RoundToInt(playerSnapshot.body),
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
                DefaultDiceCount
            );
        }
        else
        {
            Debug.LogWarning("[Combat] Enemy snapshot missing. Using default enemy.");
            Enemy = new Battler("Enemy", 100, 10, 10, 10, DefaultDiceCount);
        }

        Debug.Log($"[Combat] Session loaded. Player HP: {Player.HP} | Enemy HP: {Enemy.HP}");
    }

    public void ReceivePlayerInput(ActionType type, int attackDice, int defenseDice)
    {
        ActionType expectedType = _playerIsAttacker ? ActionType.Attack : ActionType.Defense;
        if (type != expectedType)
        {
            Debug.Log($"[Input] Ignored invalid action for current role. Expected {expectedType} and received {type}");
            return;
        }

        StartCoroutine(ResolveTurnRoutine(type, attackDice, defenseDice));
    }

    private IEnumerator ResolveTurnRoutine(ActionType playerType, int attackDice, int defenseDice)
    {
        Debug.Log("[Flow] Player ended turn");

        yield return _waitForSeconds0_5;

        GenerateEnemyAction();

        yield return _waitForSeconds0_5;

        RollActions(playerType, attackDice, defenseDice);

        yield return _waitForSeconds0_5;

        Resolve();

        yield return _waitForSeconds0_5;

        EndTurn();
    }

    private void GenerateEnemyAction()
    {
        _pendingEnemyAction = _enemyActionSelector.Select(_attackDef, _defenseDef);
        Debug.Log($"[AI] Enemy selected {_pendingEnemyAction.Definition.Type}");
    }

    private void RollActions(ActionType playerType, int attackDice, int defenseDice)
    {
        ActionDefinition playerDef = playerType == ActionType.Attack ? _attackDef : _defenseDef;

        DiceResult playerDice = _diceService.Roll();
        DiceResult enemyDice = _diceService.Roll();

        _pendingPlayerAction = new ActionInstance(playerDef, playerDice);

        _pendingEnemyAction.Dice = enemyDice;

        Debug.Log("[Flow] Both rolled dice");
    }

    private void Resolve()
    {
        ActionInstance attack;
        ActionInstance defense;

        if (_playerIsAttacker)
        {
            attack = _pendingPlayerAction;
            defense = _pendingEnemyAction;
        }
        else
        {
            attack = _pendingEnemyAction;
            defense = _pendingPlayerAction;
        }

        int damage = _resolver.Resolve(attack, defense);

        if (_playerIsAttacker)
            Enemy.ReceiveDamage(damage);
        else
            Player.ReceiveDamage(damage);

        Debug.Log($"[HP] Player: {Player.HP} | Enemy: {Enemy.HP}");

        view.UpdateView(Player, Enemy);
    }

    private void EndTurn()
    {
        Player.RecoverDice(1);
        Enemy.RecoverDice(1);

        _playerIsAttacker = !_playerIsAttacker;

        UpdateTurnRoleUI();

        Debug.Log("[Flow] Turn End");
    }

    private void UpdateTurnRoleUI()
    {
        ActionType allowedAction = _playerIsAttacker ? ActionType.Attack : ActionType.Defense;

        input.SetAllowedAction(allowedAction);
        view.actionPanel.SetPlayerRoleButtons(_playerIsAttacker);
    }
}
