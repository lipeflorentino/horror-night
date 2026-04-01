using UnityEngine;

public class PlayerTurnActions
{
    public bool IsAttack(PlayerActionType action)
    {
        return action == PlayerActionType.AttackHeart || action == PlayerActionType.AttackBody || action == PlayerActionType.AttackMind;
    }

    public RollType GetRollType(PlayerActionType action)
    {
        return action switch
        {
            PlayerActionType.AttackHeart => RollType.Heart,
            PlayerActionType.AttackBody => RollType.Body,
            PlayerActionType.AttackMind => RollType.Mind,
            PlayerActionType.Defend => RollType.Body,
            PlayerActionType.Parry => RollType.Body,
            PlayerActionType.Flee => RollType.Body,
            PlayerActionType.InstantKill => RollType.Mind,
            PlayerActionType.Learn => RollType.Mind,
            _ => RollType.Body
        };
    }

    public RollType GetDefenseRollTypeByIncomingAttack(EnemyActionType enemyAttack)
    {
        return enemyAttack switch
        {
            EnemyActionType.AttackHeart => RollType.Heart,
            EnemyActionType.AttackBody => RollType.Body,
            EnemyActionType.AttackMind => RollType.Mind,
            _ => RollType.Body
        };
    }

    public int GetSpecialChance(PlayerActionType action, TurnManagerStats stats)
    {
        return action switch
        {
            PlayerActionType.Flee => stats.fleeChance,
            PlayerActionType.InstantKill => stats.instantKillChance,
            PlayerActionType.Learn => stats.learnChance,
            _ => 0
        };
    }

    public string Format(PlayerActionType action)
    {
        return action switch
        {
            PlayerActionType.AttackHeart => "Heart Atack",
            PlayerActionType.AttackBody => "Body Atack",
            PlayerActionType.AttackMind => "Mind Atack",
            PlayerActionType.Defend => "Defend",
            PlayerActionType.Parry => "Parry",
            PlayerActionType.Flee => "Flee",
            PlayerActionType.InstantKill => "Instant Kill",
            PlayerActionType.Learn => "Learn",
            PlayerActionType.Item => "Use Item",
            _ => action.ToString()
        };
    }
}
