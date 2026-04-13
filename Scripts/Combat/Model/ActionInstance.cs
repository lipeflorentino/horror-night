public class ActionInstance
{
    public ActionDefinition definition;
    public int allocatedDice;
    public int allocatedHeart;
    public int allocatedBody;
    public int allocatedMind;
    public int itemId;
    public int skillId;

    public int TotalDiceCost()
    {
        int baseCost = definition != null ? definition.baseDiceCost : 0;
        return baseCost + allocatedDice;
    }
}
