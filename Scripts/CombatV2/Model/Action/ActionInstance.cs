public class ActionInstance
{
    public ActionDefinition Definition;
    public DiceResult PowerDice;
    public DiceResult AccuracyDice;
    public int AllocatedPowerDiceCount;
    public int AllocatedAccuracyDiceCount;

    public ActionInstance(ActionDefinition definition, DiceResult powerDice, DiceResult accuracyDice, int allocatedPowerDiceCount = 0, int allocatedAccuracyDiceCount = 0)
    {
        Definition = definition;
        PowerDice = powerDice;
        AccuracyDice = accuracyDice;
        AllocatedPowerDiceCount = allocatedPowerDiceCount;
        AllocatedAccuracyDiceCount = allocatedAccuracyDiceCount;
    }
}
