/// <summary>
/// Interface para condições de Perks extensível e escalável.
/// Permite adicionar novos tipos de condições sem modificar código existente.
/// </summary>
public interface IPerkCondition
{
    /// <summary>
    /// Avalia se a condição é satisfeita dado o contexto e o valor esperado.
    /// </summary>
    bool Evaluate(object context, string conditionValue);

    /// <summary>
    /// Retorna o tipo de condição que este avaliador suporta.
    /// </summary>
    PerkConditionKey ConditionType { get; }
}
