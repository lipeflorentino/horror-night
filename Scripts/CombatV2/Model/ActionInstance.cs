public class ActionInstance
{
    public ActionDefinition Definition;
    public DiceResult Dice;

    public ActionInstance(ActionDefinition definition, DiceResult dice)
    {
        Definition = definition;
        Dice = dice;
    }
}