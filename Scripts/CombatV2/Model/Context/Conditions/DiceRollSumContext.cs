using System.Collections.Generic;

/// <summary>
/// Context para avaliação de soma de dados.
/// </summary>
public class DiceRollSumContext : IPerkConditionContext
{
    public int TotalSum { get; set; }
    public List<DiceResult> Dices { get; set; }
}
