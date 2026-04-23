using System;
using UnityEngine;

[Serializable]
public class CombatResultSnapshot
{
    [SerializeField] public PlayerStatusSnapshot PlayerSnapshot;
    [SerializeField] public EnemyInstance EnemyInstance;
    [SerializeField] public bool PlayerWon;
}

[Serializable]
public class CombatReturnSnapshot
{
    [SerializeField] public string SceneName;
    [SerializeField] public LevelSO Level;
    [SerializeField] public int LevelIndex;
    [SerializeField] public bool[] ExploredNodes;
    [SerializeField] public Vector3 PlayerPosition;
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

public static class CombatReturnStore
{
    public static CombatReturnSnapshot Current { get; private set; }

    public static void Set(CombatReturnSnapshot snapshot)
    {
        Current = snapshot;
    }

    public static CombatReturnSnapshot Consume()
    {
        CombatReturnSnapshot snapshot = Current;
        Current = null;
        return snapshot;
    }
}
