public static class CombatSessionStore
{
    public static CombatSessionData Current { get; private set; }

    public static void SetSession(CombatSessionData data)
    {
        Current = data;
    }

    public static CombatSessionData Consume()
    {
        CombatSessionData session = Current;
        Current = null;
        return session;
    }
}
