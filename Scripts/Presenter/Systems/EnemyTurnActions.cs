using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnActions
{
    public EnemyActionType ChooseEnemyDefenseAction()
    {
        return EnemyActionType.Defend;
    }

    public EnemyActionType ChooseEnemyAction(
        int playerLife,
        int playerPhysical,
        int playerMental,
        int enemyLife,
        int enemyPhysical,
        int enemyMental)
    {
        List<(EnemyActionType action, float weight)> weightedActions = new()
        {
            (EnemyActionType.AttackLife, BuildWeight(playerLife, enemyLife)),
            (EnemyActionType.AttackPhysical, BuildWeight(playerPhysical, enemyPhysical)),
            (EnemyActionType.AttackMental, BuildWeight(playerMental, enemyMental))
        };

        float totalWeight = 0f;
        for (int i = 0; i < weightedActions.Count; i++)
            totalWeight += weightedActions[i].weight;

        if (totalWeight <= 0f)
            return EnemyActionType.AttackLife;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < weightedActions.Count; i++)
        {
            cumulative += weightedActions[i].weight;
            if (roll <= cumulative)
                return weightedActions[i].action;
        }

        return EnemyActionType.AttackLife;
    }

    private float BuildWeight(int playerStat, int enemyStat)
    {
        if (playerStat <= 0)
            return 0f;

        float vulnerableWeight = 1f / (playerStat + 1f);
        float enemyStrengthWeight = Mathf.Max(0f, enemyStat) / 10f;
        return 0.25f + vulnerableWeight * 6f + enemyStrengthWeight;
    }

    public bool IsAttack(EnemyActionType action)
    {
        return action == EnemyActionType.AttackLife || action == EnemyActionType.AttackPhysical || action == EnemyActionType.AttackMental;
    }

    public RollType GetRollType(EnemyActionType action)
    {
        return action switch
        {
            EnemyActionType.AttackLife => RollType.Life,
            EnemyActionType.AttackPhysical => RollType.Physical,
            EnemyActionType.AttackMental => RollType.Mental,
            EnemyActionType.Defend => RollType.Physical,
            _ => RollType.Physical
        };
    }

    public string Format(EnemyActionType action)
    {
        return action switch
        {
            EnemyActionType.AttackLife => "Ataque de Vida",
            EnemyActionType.AttackPhysical => "Ataque Físico",
            EnemyActionType.AttackMental => "Ataque Mental",
            EnemyActionType.Defend => "Defesa",
            _ => action.ToString()
        };
    }
}
