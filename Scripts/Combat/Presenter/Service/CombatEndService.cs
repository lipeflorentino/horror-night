public class CombatEndService
{
    public CombatOutcome? CheckEnd(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        if (enemy != null && enemy.IsDead())
        {
            return CombatOutcome.Victory;
        }

        if (player != null && player.IsDead())
        {
            return CombatOutcome.Defeat;
        }

        return null;
    }
}
