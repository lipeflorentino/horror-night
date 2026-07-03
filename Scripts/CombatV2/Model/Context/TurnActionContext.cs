public class TurnActionContext : ICombatContext
{
    public Battler Attacker;
    public Battler Defender;

    public ActionInstance AttackAction;
    public ActionInstance DefenseAction;

    public TurnActionContext(Battler attacker, Battler defender)
    {
        Attacker = attacker;
        Defender = defender;
    }
}
