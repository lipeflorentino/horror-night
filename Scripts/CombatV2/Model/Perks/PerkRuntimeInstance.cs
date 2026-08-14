public class PerkRuntimeInstance
{
    public PerkSO Definition;
    public Battler Source;
    public int RemainingTurns;
    public int Stacks;
    public string SourceTrickId;
    public string SourceTrickInstanceId;
    public TrickRuntimeInstance SourceTrick;
    public DrawbackRuntimeInstance SourceDrawback;
    public BattlerStateRuntimeInstance SourceState;

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

    public bool IsActive()
    {
        return Definition != null && (RemainingTurns < 0 || RemainingTurns > 0);
    }

    public void SetSourceTrick(TrickRuntimeInstance sourceTrick)
    {
        SourceTrick = sourceTrick;
        SourceTrickId = sourceTrick?.Definition?.Id;
        SourceTrickInstanceId = sourceTrick?.InstanceId;
    }

    public void SetSourceDrawback(DrawbackRuntimeInstance sourceDrawback)
    {
        SourceDrawback = sourceDrawback;
    }

    public void SetSourceState(BattlerStateRuntimeInstance sourceState)
    {
        SourceState = sourceState;
    }
}
