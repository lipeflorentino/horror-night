using UnityEngine;

public class EnemyTurnActions
{
    public EnemyActionType ChooseEnemyAction(bool isPlayerTurn)
    {
        if (isPlayerTurn)
            return EnemyActionType.Defend;

        int roll = Random.Range(0, 100);

        if (roll < 30)
            return EnemyActionType.AttackLife;

        if (roll < 55)
            return EnemyActionType.AttackPhysical;

        if (roll < 80)
            return EnemyActionType.AttackMental;

        return EnemyActionType.Defend;
    }

    public PlayerActionType ChooseAutoDefenseAction()
    {
        return Random.value < 0.75f ? PlayerActionType.Defend : PlayerActionType.Parry;
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
