using System;

[Serializable]
public class CombatResultSnapshot
{
    // Inventory data is embedded in PlayerStatusSnapshot.inventory.
    public PlayerStatusSnapshot playerSnapshot;
    public CombatOutcome outcome;
}
