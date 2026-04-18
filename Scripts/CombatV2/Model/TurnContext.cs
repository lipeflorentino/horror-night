public class TurnContext
{
    public Battler Attacker;
    public Battler Defender;

    public ActionInstance AttackAction;
    public ActionInstance DefenseAction;

    public TurnContext(Battler attacker, Battler defender)
    {
        Attacker = attacker;
        Defender = defender;
    }
}