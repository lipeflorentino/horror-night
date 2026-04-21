using System.Collections.Generic;
using UnityEngine;

public class EnemyActionSelector
{
    private readonly List<IEnemyActionStrategy> Strategies;

    public EnemyActionSelector()
    {
        Strategies = new List<IEnemyActionStrategy>
        {
            new EnemyAttackActionStrategy(),
            new EnemyDefenseActionStrategy()
        };
    }

    public ActionInstance Select(ActionDefinition attackDefinition, ActionDefinition defenseDefinition)
    {
        int index = Random.Range(0, Strategies.Count);
        return Strategies[index].Build(attackDefinition, defenseDefinition);
    }
}
