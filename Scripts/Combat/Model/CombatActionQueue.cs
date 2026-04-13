using System.Collections.Generic;

public class CombatActionQueue
{
    private readonly List<ActionInstance> actions = new();

    public void Add(ActionInstance action)
    {
        actions.Add(action);
    }

    public void Clear()
    {
        actions.Clear();
    }

    public IReadOnlyList<ActionInstance> GetAll()
    {
        return actions;
    }
}
