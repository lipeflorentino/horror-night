using System.Collections.Generic;

public class EnemyActionPlanner
{
    public List<CombatActionData> GenerateActions(EnemyTurnAction enemyTurnAction)
    {
        var actions = new List<CombatActionData>();

        if (enemyTurnAction == EnemyTurnAction.Defend)
        {
            actions.Add(new CombatActionData
            {
                actionType = PlayerActionType.UseSkill,
                diceCost = 0,
                heartCost = 0,
                bodyCost = 0,
                mindCost = 0,
                predictedValue = 0
            });

            return actions;
        }

        actions.Add(new CombatActionData
        {
            actionType = PlayerActionType.Attack,
            diceCost = 0,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0
        });

        return actions;
    }
}
