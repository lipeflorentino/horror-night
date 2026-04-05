public static class DamageCalculator
{
    public static int CalculateDamage(int attack, int roll, int defense)
    {
        return System.Math.Max(0, attack + roll - defense);
    }
}
