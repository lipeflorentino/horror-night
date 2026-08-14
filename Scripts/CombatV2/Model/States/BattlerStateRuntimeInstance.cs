using System.Collections.Generic;

public class BattlerStateRuntimeInstance
{
    public BattlerStateSO Definition;
    public Battler Owner;
    public Battler Source;
    public int RemainingTurns;
    public bool IsNew = true;
    public List<PerkRuntimeInstance> ActivePerks = new();

    public BattlerStateRuntimeInstance(BattlerStateSO definition, Battler owner, Battler source = null, int remainingTurns = -1)
    {
        Definition = definition;
        Owner = owner;
        Source = source;
        RemainingTurns = remainingTurns >= 0 ? remainingTurns : definition?.DefaultDurationTurns ?? -1;
    }

    public bool IsActive()
    {
        return Definition != null && (RemainingTurns < 0 || RemainingTurns > 0);
    }

    public void DecreaseDuration()
    {
        if (RemainingTurns > 0)
            RemainingTurns--;
    }
}
