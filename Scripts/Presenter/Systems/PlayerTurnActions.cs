using UnityEngine;

public class PlayerTurnActions
{
    public bool IsAttack(PlayerActionType action)
    {
        return action == PlayerActionType.AttackLife || action == PlayerActionType.AttackPhysical || action == PlayerActionType.AttackMental;
    }

    public RollType GetRollType(PlayerActionType action)
    {
        return action switch
        {
            PlayerActionType.AttackLife => RollType.Life,
            PlayerActionType.AttackPhysical => RollType.Physical,
            PlayerActionType.AttackMental => RollType.Mental,
            PlayerActionType.Defend => RollType.Physical,
            PlayerActionType.Parry => RollType.Physical,
            PlayerActionType.Flee => RollType.Physical,
            PlayerActionType.InstantKill => RollType.Mental,
            PlayerActionType.Learn => RollType.Mental,
            _ => RollType.Physical
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
            PlayerActionType.AttackLife => "Ataque de Vida",
            PlayerActionType.AttackPhysical => "Ataque Físico",
            PlayerActionType.AttackMental => "Ataque Mental",
            PlayerActionType.Defend => "Defesa",
            PlayerActionType.Parry => "Parry",
            PlayerActionType.Flee => "Fuga",
            PlayerActionType.InstantKill => "Instant Kill",
            PlayerActionType.Learn => "Learn",
            PlayerActionType.Item => "Item",
            _ => action.ToString()
        };
    }
}
