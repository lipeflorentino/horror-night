public class EnemyAttackActionStrategy : IEnemyActionStrategy
{
    public ActionInstance Build(ActionDefinition attackDefinition, ActionDefinition defenseDefinition)
    {
        return new ActionInstance(attackDefinition, null);
    }
}
