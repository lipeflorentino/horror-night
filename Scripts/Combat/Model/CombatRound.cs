using System.Collections.Generic;
using System.Linq;

public class CombatRound
{
    public List<ActionInstance> playerActions;
    public List<ActionInstance> enemyActions;
    public PlayerActionType playerMainAction;
    public PlayerActionType enemyMainAction;

    public CombatRound()
    {
        playerActions = new List<ActionInstance>();
        enemyActions = new List<ActionInstance>();
        playerMainAction = PlayerActionType.EndTurn;
        enemyMainAction = PlayerActionType.EndTurn;
    }

    public bool IsPlayerActionQueueValid()
    {
        return playerActions.Any(a => 
            a.definition.type == PlayerActionType.Attack || 
            a.definition.type == PlayerActionType.Defend ||
            a.definition.type == PlayerActionType.Investigate);
    }

    public bool IsEnemyActionQueueValid()
    {
        return enemyActions.Any(a => 
            a.definition.type == PlayerActionType.Attack || 
            a.definition.type == PlayerActionType.Defend);
    }

    public void Clear()
    {
        playerActions.Clear();
        enemyActions.Clear();
        playerMainAction = PlayerActionType.EndTurn;
        enemyMainAction = PlayerActionType.EndTurn;
    }
}
