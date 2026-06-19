using System;
using System.Collections.Generic;

/// <summary>
/// Factory centralizado para criar e validar condições de Perks.
/// Permite registro dinâmico de novos tipos de condição.
/// </summary>
public static class PerkConditionFactory
{
    private static readonly Dictionary<PerkConditionKey, IPerkCondition> conditionRegistry = new();

    static PerkConditionFactory()
    {
        RegisterDefaultConditions();
    }

    private static void RegisterDefaultConditions()
    {
        Register(PerkConditionKey.Always, new AlwaysCondition());
        Register(PerkConditionKey.RollValueEquals, new RollValueEqualsCondition());
        Register(PerkConditionKey.RollTierEquals, new RollTierEqualsCondition());
        Register(PerkConditionKey.RollSumEquals, new RollSumEqualsCondition());
        Register(PerkConditionKey.RollSumEqualsAttackersRollSum, new RollSumEqualsAttackersCondition());
        Register(PerkConditionKey.BlockedAttack, new BlockedAttackCondition());
    }

    /// <summary>
    /// Registra um novo tipo de condição para uso posterior.
    /// </summary>
    public static void Register(PerkConditionKey key, IPerkCondition condition)
    {
        conditionRegistry[key] = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    /// <summary>
    /// Obtém o avaliador de condição apropriado.
    /// </summary>
    public static IPerkCondition GetCondition(PerkConditionKey key)
    {
        if (!conditionRegistry.TryGetValue(key, out var condition))
            throw new InvalidOperationException($"Nenhuma condição registrada para {key}");

        return condition;
    }

    /// <summary>
    /// Avalia se a condição é satisfeita.
    /// </summary>
    public static bool Evaluate(PerkConditionKey key, object context, string conditionValue)
    {
        return GetCondition(key).Evaluate(context, conditionValue);
    }
}

/// <summary>
/// Condição que sempre retorna verdadeiro.
/// </summary>
public class AlwaysCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.Always;

    public bool Evaluate(object context, string conditionValue) => true;
}

/// <summary>
/// Avalia se um valor de dado é igual ao esperado.
/// </summary>
public class RollValueEqualsCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.RollValueEquals;

    public bool Evaluate(object context, string conditionValue)
    {
        if (context is not DiceResult dice)
            return false;

        return int.TryParse(conditionValue, out int expectedValue) && dice.Value == expectedValue;
    }
}

/// <summary>
/// Avalia se um tier de dado é igual ao esperado.
/// </summary>
public class RollTierEqualsCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.RollTierEquals;

    public bool Evaluate(object context, string conditionValue)
    {
        if (context is not DiceResult dice)
            return false;

        return Enum.TryParse(conditionValue, true, out DiceTier expectedTier) && dice.Tier == expectedTier;
    }
}

/// <summary>
/// Avalia se a soma de dados é igual ao valor esperado.
/// NOTA: Requer um wrapper especial que contenha a soma.
/// </summary>
public class RollSumEqualsCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.RollSumEquals;

    public bool Evaluate(object context, string conditionValue)
    {
        // Context deve ser um DiceRollSumContext que contém a soma
        if (context is not DiceRollSumContext sumContext)
            return false;

        return int.TryParse(conditionValue, out int expectedSum) && sumContext.TotalSum == expectedSum;
    }
}

/// <summary>
/// Avalia se a soma de dados do defensor é igual à soma do atacante.
/// </summary>
public class RollSumEqualsAttackersCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.RollSumEqualsAttackersRollSum;

    public bool Evaluate(object context, string conditionValue)
    {
        if (context is not DefenseRollComparisonContext compContext)
            return false;

        return compContext.DefenderRollSum == compContext.AttackerRollSum;
    }
}

public class BlockedAttackCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.BlockedAttack;
    public bool Evaluate(object context, string conditionValue)
    {
        return context is ActionResolutionContext resolution
            && resolution.Outcome == ActionOutcome.Blocked;
    }
}

/// <summary>
/// Context para avaliação de soma de dados.
/// </summary>
public class DiceRollSumContext
{
    public int TotalSum { get; set; }
    public List<DiceResult> Dices { get; set; }
}

/// <summary>
/// Context para comparação de somas entre atacante e defensor.
/// </summary>
public class DefenseRollComparisonContext
{
    public int DefenderRollSum { get; set; }
    public int AttackerRollSum { get; set; }
}

/// <summary>
/// Context para avaliação de resolução de ação.
/// </summary>
public class ActionResolutionContext
{
    public Battler Actor;
    public Battler Opponent;
    public ActionType ActionType;
    public ActionOutcome Outcome;
}
