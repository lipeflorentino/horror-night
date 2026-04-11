using System.Collections.Generic;

public class CombatActionQueue
{
    private readonly List<CombatActionData> actions = new List<CombatActionData>();

    public void Add(CombatActionData action)
    {
        actions.Add(action);
    }

    public void Clear()
    {
        actions.Clear();
    }

    public IReadOnlyList<CombatActionData> GetAll()
    {
        return actions;
    }
}
