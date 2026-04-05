public class CombatResolutionService
{
    public int ApplyDamage(CombatBattlerModel target, int damage)
    {
        if (target == null)
        {
            return 0;
        }

        return target.TakeDamage(damage);
    }

    public void ApplyRecovery(CombatBattlerModel target, int amount)
    {
        if (target == null)
        {
            return;
        }

        target.RecoverResources(amount);
    }
}
