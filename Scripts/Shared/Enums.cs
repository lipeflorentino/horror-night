public enum NodeType { Default, Portal }
public enum LevelType { Default, Boss, Story }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
public enum NodeActivityType { Loot, Occurrence, Encounter, Treasure, None }

[System.Flags]
public enum NodeFlags
{
    None            = 0,
    CanSpawnLoot    = 1 << 0,
    CanSpawnEnemy   = 1 << 1,
    CanSpawnOccurrence   = 1 << 2,
    canSpawnTreasure = 1 << 3,
    OneTimeOnly     = 1 << 4,
    Dangerous       = 1 << 5
}


public enum CombatLogStyle
{
    Neutral,
    Positive,
    Negative,
    Action,
    Info
}
public enum CombatFlowState
{
    PlayerTurn,
    EnemyTurn,
    Finished
}

public enum CombatOutcome
{
    Victory,
    Defeat,
    Fled
}

public enum PlayerActionType
{
    Attack,
    Defend,
    Investigate,
    UseItem,
    UseSkill,
    EndTurn
}

public enum EnemyTurnAction
{
    None,
    Attack,
    Defend
}