public class BattlerStateInstance
{
    public BattlerStateDefinition Definition;
    public int RemainingTurns;
    public int Stacks;
    public Battler Source;

    public BattlerStateInstance(BattlerStateDefinition definition, Battler source = null, int remainingTurns = -1, int stacks = 1)
    {
        Definition = definition;
        Source = source;
        RemainingTurns = remainingTurns >= 0 ? remainingTurns : definition != null ? definition.DefaultDurationTurns : 0;
        Stacks = UnityEngine.Mathf.Max(1, stacks);
    }
}
