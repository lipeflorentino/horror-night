using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    public CombatView view;
    public CombatInputHandler input;

    public Battler Player { get; private set; }
    public Battler Enemy { get; private set; }

    private DiceService _diceService;
    private ActionResolverService _resolver;

    private ActionDefinition _attackDef;
    private ActionDefinition _defenseDef;

    private bool _playerIsAttacker = true;

    private ActionInstance _pendingPlayerAction;
    private ActionInstance _pendingEnemyAction;

    void Start()
    {
        _diceService = new DiceService();
        _resolver = new ActionResolverService();

        Player = new Battler("Player", 100, 10, 10, 10, 3);
        Enemy = new Battler("Enemy", 100, 10, 10, 10, 3);

        _attackDef = new ActionDefinition("attack", ActionType.Attack, 10);
        _defenseDef = new ActionDefinition("defense", ActionType.Defense, 8);

        input.Init(this);

        view.UpdateView(Player, Enemy);
    }

    public void ReceivePlayerInput(ActionType type, int attackDice, int defenseDice)
    {
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
        ActionType type = Random.value > 0.5f ? ActionType.Attack : ActionType.Defense;
        ActionDefinition def = type == ActionType.Attack ? _attackDef : _defenseDef;

        _pendingEnemyAction = new ActionInstance(def, null);

        Debug.Log($"[AI] Enemy selected {type}");
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

        Debug.Log("[Flow] Turn End");
    }
}