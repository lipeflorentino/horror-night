using System.Collections.Generic;
using UnityEngine;

public class EnemyActionSelector
{
    private readonly List<IEnemyActionStrategy> _strategies;

    public EnemyActionSelector()
    {
        _strategies = new List<IEnemyActionStrategy>
        {
            new EnemyAttackActionStrategy(),
            new EnemyDefenseActionStrategy()
        };
    }

    public ActionInstance Select(ActionDefinition attackDefinition, ActionDefinition defenseDefinition)
    {
        int index = Random.Range(0, _strategies.Count);
        return _strategies[index].Build(attackDefinition, defenseDefinition);
    }
}
