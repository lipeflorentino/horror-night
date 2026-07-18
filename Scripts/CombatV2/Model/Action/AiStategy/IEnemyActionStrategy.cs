public interface IEnemyActionStrategy
{
    ActionInstance Build(ActionDefinition attackDefinition, ActionDefinition defenseDefinition);
}
