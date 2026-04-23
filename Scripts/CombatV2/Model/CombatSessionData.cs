using System;
using UnityEngine;

[Serializable]
public class CombatSessionData
{
    [SerializeField] public PlayerStatusSnapshot PlayerSnapshot;
    [SerializeField] public EnemyInstance EnemyInstance;
    [SerializeField] public float RiskModifier = 1f;
    [SerializeField] public string ReturnSceneName;
    [SerializeField] public LevelSO ReturnLevel;
    [SerializeField] public int ReturnLevelIndex;
    [SerializeField] public bool[] ReturnExploredNodes;
    [SerializeField] public Vector3 ReturnPlayerPosition;
}

public static class CombatSessionStore
{
    public static CombatSessionData Current { get; private set; }

    public static void SetSession(CombatSessionData session)
    {
        Current = session;
    }

    public static CombatSessionData Consume()
    {
        CombatSessionData session = Current;
        Current = null;
        return session;
    }
}
