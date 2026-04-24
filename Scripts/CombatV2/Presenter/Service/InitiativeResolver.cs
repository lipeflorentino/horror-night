public class InitiativeResolver
{
    public Battler ResolveStartingBattler(Battler firstBattler, Battler secondBattler)
    {
        if (firstBattler == null)
            return secondBattler;

        if (secondBattler == null)
            return firstBattler;

        return firstBattler.Initiative >= secondBattler.Initiative ? firstBattler : secondBattler;
    }
}