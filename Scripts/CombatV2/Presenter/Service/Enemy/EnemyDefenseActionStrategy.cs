public class EnemyDefenseActionStrategy : IEnemyActionStrategy
{
    public ActionInstance Build(ActionDefinition attackDefinition, ActionDefinition defenseDefinition)
    {
        return new ActionInstance(defenseDefinition, null);
    }
}
