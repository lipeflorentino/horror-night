public class ActionInstance
{
    public ActionDefinition Definition;
    public DiceResult PowerDice;
    public DiceResult AccuracyDice;

    public ActionInstance(ActionDefinition definition, DiceResult powerDice, DiceResult accuracyDice)
    {
        Definition = definition;
        PowerDice = powerDice;
        AccuracyDice = accuracyDice;
    }
}
