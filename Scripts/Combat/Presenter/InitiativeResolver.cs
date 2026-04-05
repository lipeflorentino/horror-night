public class InitiativeResolver
{
    private readonly DiceService dice;

    public InitiativeResolver(DiceService dice)
    {
        this.dice = dice ?? new DiceService();
    }

    public bool PlayerStarts(CombatBattlerModel player, CombatBattlerModel enemy)
    {
        if (player == null || enemy == null)
        {
            return true;
        }

        int playerTotal = dice.RollD6() + player.initiative;
        int enemyTotal = dice.RollD6() + enemy.initiative;

        return playerTotal >= enemyTotal;
    }
}
