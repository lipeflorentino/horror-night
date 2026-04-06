public static class CombatResultStore
{
    public static CombatResultSnapshot Current { get; private set; }

    public static void SetResult(CombatResultSnapshot result)
    {
        Current = result;
    }

    public static CombatResultSnapshot Consume()
    {
        CombatResultSnapshot result = Current;
        Current = null;
        return result;
    }
}
