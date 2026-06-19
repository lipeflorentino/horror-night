using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Avaliador centralizado de Perk Triggers.
/// Responsável por:
/// 1. Validar se as condições de um perk foram satisfeitas
/// 2. Disparar eventos de perk acionado
/// 3. Fornecer contexto completo sobre o trigger
/// 
/// Separado de PerkService para manter responsabilidade única.
/// </summary>
public class PerkTriggerEvaluator
{
    public event Action<PerkTriggeredEvent> OnPerkTriggered;

    public PerkTriggerEvaluator(PerkDatabase database = null)
    {
        // O database é mantido apenas para compatibilidade de construtor.
        // A partir da Fase 3, a fonte única de perks efetivos vem do PerkService.
    }

    /// <summary>
    /// Avalia perks efetivos acionados por roll (BeforeRoll trigger).
    /// Chama esta função ANTES de aplicar modificadores de roll.
    /// </summary>
    public void EvaluateRollTriggers(
        Battler owner,
        CombatRollContext context,
        PerkTrigger expectedTrigger,
        IReadOnlyList<PerkRuntimeInstance> effectivePerks,
        List<DiceResult> rolledDices = null)
    {
        if (owner == null || effectivePerks == null || effectivePerks.Count == 0)
            return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null)
                continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                if (!IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                if (!rule.MatchesRoll(context))
                    continue;

                if (!ValidateCondition(rule, context))
                    continue;

                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }
    }

    /// <summary>
    /// Avalia perks efetivos acionados por dados (PowerMultiplier e AfterResolve triggers).
    /// Chama esta função com os dados já rolados.
    /// </summary>
    public void EvaluateDiceTriggers(
        Battler owner,
        CombatActionContext context,
        DiceResult dice,
        PerkTrigger expectedTrigger,
        IReadOnlyList<PerkRuntimeInstance> effectivePerks,
        List<DiceResult> allDices = null,
        List<DiceResult> opposingDices = null)
    {
        if (owner == null || dice == null || effectivePerks == null || effectivePerks.Count == 0)
            return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null)
                continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                if (!IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                if (!rule.MatchesAction(context))
                    continue;

                if (!rule.MatchesDice(dice))
                    continue;

                if (!ValidateDiceCondition(rule, dice, allDices, opposingDices))
                    continue;

                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }
    }

    // TODO: Avaliar triggers de resolução de ação (AfterResolve), como BlockedAttack, para aplicar efeitos pós-resolução. 
    public void EvaluateActionResolutionTriggers(
        Battler owner,
        ActionResolutionContext context,
        IReadOnlyList<PerkRuntimeInstance> effectivePerks)
    {
        if (owner == null || context == null || effectivePerks == null || effectivePerks.Count == 0)
            return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null)
                continue;
        }
    }

    /// <summary>
    /// Valida condição de roll (Always, RollValueEquals, RollTierEquals, etc).
    /// </summary>
    private bool ValidateCondition(PerkRule rule, CombatRollContext context)
    {
        try
        {
            return PerkConditionFactory.Evaluate(rule.ConditionKey, context, rule.ConditionValue);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Erro ao validar condição {rule.ConditionKey}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Valida condição de dice (análises de valores, tiers, somas).
    /// </summary>
    private bool ValidateDiceCondition(PerkRule rule, DiceResult dice, List<DiceResult> allDices = null, List<DiceResult> opposingDices = null)
    {
        try
        {
            if (rule.ConditionKey == PerkConditionKey.RollValueEquals ||
                rule.ConditionKey == PerkConditionKey.RollTierEquals)
            {
                return PerkConditionFactory.Evaluate(rule.ConditionKey, dice, rule.ConditionValue);
            }

            if (rule.ConditionKey == PerkConditionKey.RollSumEquals && allDices != null)
            {
                if (allDices.Count == 0 || dice != allDices[0])
                    return false;

                var sumContext = new DiceRollSumContext { TotalSum = SumDice(allDices), Dices = allDices };
                return PerkConditionFactory.Evaluate(rule.ConditionKey, sumContext, rule.ConditionValue);
            }

            if (rule.ConditionKey == PerkConditionKey.RollSumEqualsAttackersRollSum && allDices != null && opposingDices != null)
            {
                if (allDices.Count == 0 || dice != allDices[0])
                    return false;

                var comparisonContext = new DefenseRollComparisonContext
                {
                    DefenderRollSum = SumDice(allDices),
                    AttackerRollSum = SumDice(opposingDices)
                };
                return PerkConditionFactory.Evaluate(rule.ConditionKey, comparisonContext, rule.ConditionValue);
            }

            return rule.ConditionKey == PerkConditionKey.Always;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Erro ao validar condição de dice {rule.ConditionKey}: {ex.Message}");
            return false;
        }
    }

    private static int SumDice(List<DiceResult> dices)
    {
        int sum = 0;
        if (dices == null)
            return sum;

        for (int i = 0; i < dices.Count; i++)
            sum += dices[i]?.Value ?? 0;

        return sum;
    }

    /// <summary>
    /// Dispara o evento PerkTriggered com contexto completo.
    /// </summary>
    private void NotifyPerkTriggered(
        Battler owner,
        PerkRuntimeInstance perk,
        PerkRule rule,
        object context,
        float appliedValue)
    {
        if (perk?.Definition == null)
            return;

        var triggerEvent = new PerkTriggeredEvent
        {
            PerkId = perk.Definition.Id,
            Owner = owner,
            SourceTrickId = perk.SourceTrickId,
            SourceTrickInstanceId = perk.SourceTrickInstanceId,
            Trigger = rule.Trigger,
            ModifierTarget = rule.ModifierTarget,
            AppliedValue = appliedValue,
            StacksApplied = perk.Stacks,
            FullContext = context,
            TriggerTime = Time.time
        };

        perk.SourceTrick?.MarkTriggered();
        OnPerkTriggered?.Invoke(triggerEvent);

        Debug.Log($"[Perk Triggered] {perk.Definition.Id} " +
                  $"- Trigger: {rule.Trigger}, Target: {rule.ModifierTarget}, Value: {appliedValue}");
    }

    /// <summary>
    /// Valida se o owner do perk é compatível com o role especificado.
    /// </summary>
    private static bool IsRoleMatch(Battler owner, CombatRollContext context, BattlerStateRole role)
    {
        return role switch
        {
            BattlerStateRole.OwnerAsActor => owner == context.Actor,
            BattlerStateRole.OwnerAsOpponent => owner == context.Opponent,
            BattlerStateRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
    }

    /// <summary>
    /// Valida se o owner do perk é compatível com o role especificado (para ação).
    /// </summary>
    private static bool IsRoleMatch(Battler owner, CombatActionContext context, BattlerStateRole role)
    {
        return role switch
        {
            BattlerStateRole.OwnerAsActor => owner == context.Actor,
            BattlerStateRole.OwnerAsOpponent => owner == context.Opponent,
            BattlerStateRole.OwnerAsAttacker => context.ActionType == ActionType.Attack ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsDefender => context.ActionType == ActionType.Defense ? owner == context.Actor : owner == context.Opponent,
            BattlerStateRole.OwnerAsTarget => context.ActionType == ActionType.Attack ? owner == context.Opponent : owner == context.Actor,
            _ => false
        };
    }
}
