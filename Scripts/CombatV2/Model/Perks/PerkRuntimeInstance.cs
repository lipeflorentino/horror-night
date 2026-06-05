public class PerkRuntimeInstance
{
    public PerkSO Definition;
    public Battler Source;
    public int RemainingTurns;
    public int Stacks;

    public PerkRuntimeInstance(PerkSO definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        Definition = definition;
        Source = source;
        RemainingTurns = durationTurns >= 0 ? durationTurns : definition?.DefaultDurationTurns ?? -1;
        Stacks = stacks < 1 ? 1 : stacks;
    }
}
