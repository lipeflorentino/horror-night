/// <summary>
/// Context para comparação de somas entre atacante e defensor.
/// </summary>
public class DefenseRollComparisonContext : IPerkConditionContext
{
    public int DefenderRollSum { get; set; }
    public int AttackerRollSum { get; set; }
}
