using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnActions
{
    public EnemyActionType ChooseEnemyDefenseAction()
    {
        return EnemyActionType.Defend;
    }

    public EnemyActionType ChooseEnemyAction(
        int playerHeart,
        int playerBody,
        int playerMind,
        int enemyHeart,
        int enemyBody,
        int enemyMind)
    {
        List<(EnemyActionType action, float weight)> weightedActions = new()
        {
            (EnemyActionType.AttackHeart, BuildWeight(playerHeart, enemyHeart)),
            (EnemyActionType.AttackBody, BuildWeight(playerBody, enemyBody)),
            (EnemyActionType.AttackMind, BuildWeight(playerMind, enemyMind))
        };

        float totalWeight = 0f;
        for (int i = 0; i < weightedActions.Count; i++)
            totalWeight += weightedActions[i].weight;

        if (totalWeight <= 0f)
            return EnemyActionType.AttackHeart;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < weightedActions.Count; i++)
        {
            cumulative += weightedActions[i].weight;
            if (roll <= cumulative)
                return weightedActions[i].action;
        }

        return EnemyActionType.AttackHeart;
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
        return action == EnemyActionType.AttackHeart || action == EnemyActionType.AttackBody || action == EnemyActionType.AttackMind;
    }

    public RollType GetRollType(EnemyActionType action)
    {
        return action switch
        {
            EnemyActionType.AttackHeart => RollType.Heart,
            EnemyActionType.AttackBody => RollType.Body,
            EnemyActionType.AttackMind => RollType.Mind,
            EnemyActionType.Defend => RollType.Body,
            _ => RollType.Body
        };
    }

    public RollType GetDefenseRollTypeByIncomingAttack(PlayerActionType playerAttack)
    {
        return playerAttack switch
        {
            PlayerActionType.AttackHeart => RollType.Heart,
            PlayerActionType.AttackBody => RollType.Body,
            PlayerActionType.AttackMind => RollType.Mind,
            _ => RollType.Body
        };
    }

    public string Format(EnemyActionType action)
    {
        return action switch
        {
            EnemyActionType.AttackHeart => "Heart Atack",
            EnemyActionType.AttackBody => "Body Atack",
            EnemyActionType.AttackMind => "Mind Atack",
            EnemyActionType.Defend => "Defend",
            _ => action.ToString()
        };
    }
}
