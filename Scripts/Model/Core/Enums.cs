public enum NodeType { Default, Portal, Boss, Event, Story, Treasure }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
public enum NodeActivityType { Loot, Event, Encounter, Treasure, None }

[System.Flags]
public enum NodeFlags
{
    None            = 0,
    CanSpawnLoot    = 1 << 0,
    CanSpawnEnemy   = 1 << 1,
    CanSpawnEvent   = 1 << 2,
    OneTimeOnly     = 1 << 3,
    Dangerous       = 1 << 4
}
