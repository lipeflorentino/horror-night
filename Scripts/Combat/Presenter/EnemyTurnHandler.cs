using System.Collections.Generic;

public class EnemyTurnHandler
{
    private readonly CombatTurnService combatTurnService;
    private readonly EnemyActionPlanner enemyActionPlanner;
    private readonly CombatTurnResolver combatTurnResolver;

    public EnemyTurnHandler(
        CombatTurnService combatTurnService,
        EnemyActionPlanner enemyActionPlanner,
        CombatTurnResolver combatTurnResolver)
    {
        this.combatTurnService = combatTurnService;
        this.enemyActionPlanner = enemyActionPlanner;
        this.combatTurnResolver = combatTurnResolver;
    }

    public IReadOnlyList<string> ResolveEnemyTurn(CombatBattlerModel enemy, CombatBattlerModel player)
    {
        combatTurnService.StartEnemyTurn();

        List<CombatActionData> enemyActions = enemyActionPlanner.GenerateActions(combatTurnService.lastEnemyAction);
        combatTurnResolver.ResolvePlayerTurn(enemy, player, enemyActions);

        return new List<string>(combatTurnResolver.lastResolutionLog);
    }
}
