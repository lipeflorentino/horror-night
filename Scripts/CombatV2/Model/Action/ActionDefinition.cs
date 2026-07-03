public enum ActionType
{
    Attack,
    Defense
}

public class ActionDefinition
{
    public string Id;
    public ActionType Type;
    public int BasePower;

    public ActionDefinition(string id, ActionType type, int basePower)
    {
        Id = id;
        Type = type;
        BasePower = basePower;
    }
}