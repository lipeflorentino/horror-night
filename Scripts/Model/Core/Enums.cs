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
