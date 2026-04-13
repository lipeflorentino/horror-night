using System;

[Serializable]
public class CombatResultSnapshot
{
    public PlayerStatusSnapshot playerSnapshot;
    public CombatOutcome outcome;
}
