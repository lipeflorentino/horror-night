using System;
using UnityEngine;

[Serializable]
public class CombatResultSnapshot
{
    [SerializeField] public PlayerStatusSnapshot playerSnapshot;
    [SerializeField] public EnemyInstance enemyInstance;
    [SerializeField] public bool playerWon;
}

public static class CombatResultStore
{
    public static CombatResultSnapshot Current { get; private set; }

    public static void SetResult(CombatResultSnapshot result)
    {
        Current = result;
    }

    public static CombatResultSnapshot Consume()
    {
        CombatResultSnapshot result = Current;
        Current = null;
        return result;
    }
}