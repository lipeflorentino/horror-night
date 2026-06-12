public class PerkRuntimeInstance
{
    public PerkSO Definition;
    public Battler Source;
    public int RemainingTurns;
    public int Stacks;
    public string SourceTrickId;
    public string SourceTrickInstanceId;
    public TrickRuntimeInstance SourceTrick;

    public PerkRuntimeInstance(
        PerkSO definition,
        Battler source = null,
        int durationTurns = -1,
        int stacks = 1,
        TrickRuntimeInstance sourceTrick = null)
    {
        Definition = definition;
        Source = source;
        RemainingTurns = durationTurns >= 0 ? durationTurns : definition?.DefaultDurationTurns ?? -1;
        Stacks = stacks < 1 ? 1 : stacks;
        SetSourceTrick(sourceTrick);
    }

    public void SetSourceTrick(TrickRuntimeInstance sourceTrick)
    {
        SourceTrick = sourceTrick;
        SourceTrickId = sourceTrick?.Definition?.Id;
        SourceTrickInstanceId = sourceTrick?.InstanceId;
    }
}
