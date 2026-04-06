using System;

[Serializable]
public class CombatSessionData
{
    public PlayerStatusSnapshot playerSnapshot;
    public EnemyInstance enemyInstance;
    public float riskModifier;
}
