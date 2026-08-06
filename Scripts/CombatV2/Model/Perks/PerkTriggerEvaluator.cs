using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Avaliador centralizado de Perk Triggers. Responsável exclusivamente por validar condições de disparo e calcular os valores aplicados, notificando o sistema sem mutar estado diretamente.
/// </summary>
public class PerkTriggerEvaluator
{
    public event Action<PerkTriggeredEvent> OnPerkTriggered;

    public PerkTriggerEvaluator(){}

    /// <summary>
    /// Avalia perks efetivos acionados por roll (BeforeRoll trigger).
    /// Chama esta função ANTES de aplicar modificadores de roll.
    /// </summary>
    public void EvaluateRollTriggers(Battler owner, CombatRollContext context, PerkTrigger expectedTrigger, IReadOnlyList<PerkRuntimeInstance> effectivePerks, List<DiceResult> rolledDices = null)
    {
        if (owner == null || effectivePerks == null || effectivePerks.Count == 0) return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null) continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger) continue;
                if (!PerkRuntimeHelper.IsRoleMatch(owner, context, rule.OwnerRole)) continue;
                if (!rule.MatchesRoll(context)) continue;
                if (!ValidateCondition(rule, context)) continue;

                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }
    }

    /// <summary>
    /// Avalia perks AfterAccuracyRoll: disparados após o resultado de Accuracy ser conhecido,
    /// antes dos dados de Poder serem rolados.
    /// Retorna a quantidade total de dados extras de Poder a adicionar e o StatType a usar.
    /// </summary>
    public int EvaluateAfterAccuracyTriggers(Battler owner, DiceResult accuracyResult, ActionType actionType, IReadOnlyList<PerkRuntimeInstance> effectivePerks, out DiceStatType extraDiceStatType)
    {
        extraDiceStatType = DiceStatType.Body;
        int totalExtraDice = 0;

        if (owner == null || accuracyResult == null || effectivePerks == null || effectivePerks.Count == 0) return 0;

        CombatActionContext actionContext = new(owner, null, actionType);

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null) continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != PerkTrigger.AfterAccuracyRoll) continue;
                if (rule.ModifierTarget != PerkModifierTarget.ExtraDice) continue;
                if (!PerkRuntimeHelper.IsRoleMatch(owner, actionContext, rule.OwnerRole)) continue;
                if (rule.FilterByActionType && rule.ActionType != actionType) continue;
                if (rule.FilterByTier && rule.Tier != accuracyResult.Tier) continue;
                if (rule.FilterByStatType && rule.StatType != accuracyResult.StatType) continue;

                int extraDice = Mathf.Max(0, Mathf.RoundToInt(rule.Value * Mathf.Max(1, perk.Stacks)));
                if (extraDice <= 0) continue;

                if (rule.FilterByStatType) extraDiceStatType = rule.StatType;
                else extraDiceStatType = accuracyResult.StatType;

                totalExtraDice += extraDice;
                NotifyPerkTriggered(owner, perk, rule, actionContext, rule.Value);
            }
        }

        return totalExtraDice;
    }

    /// <summary>
    /// Avalia perks efetivos acionados por dados (PowerMultiplier e AfterResolve triggers).
    /// Chama esta função com os dados já rolados.
    /// </summary>
    public void EvaluateDiceTriggers(Battler owner, CombatActionContext context, DiceResult dice, PerkTrigger expectedTrigger, IReadOnlyList<PerkRuntimeInstance> effectivePerks, List<DiceResult> allDices = null, List<DiceResult> opposingDices = null)
    {
        if (owner == null || dice == null || effectivePerks == null || effectivePerks.Count == 0) return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null) continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger) continue;
                if (!PerkRuntimeHelper.IsRoleMatch(owner, context, rule.OwnerRole)) continue;
                if (!rule.MatchesAction(context)) continue;
                if (!rule.MatchesDice(dice)) continue;
                if (!ValidateDiceCondition(rule, dice, allDices, opposingDices)) continue;

                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }
    }

    /// <summary>
    /// Avalia perks efetivos acionados após a resolução de uma ação.
    /// </summary>
    public void EvaluateActionResolutionTriggers(Battler owner, ActionResolutionContext context, IReadOnlyList<PerkRuntimeInstance> effectivePerks)
    {
        if (owner == null || context == null || effectivePerks == null || effectivePerks.Count == 0) return;

        for (int i = 0; i < effectivePerks.Count; i++)
        {
            PerkRuntimeInstance perk = effectivePerks[i];
            if (perk?.Definition?.Rules == null) continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != PerkTrigger.OnActionResolved) continue;
                if (!PerkRuntimeHelper.IsRoleMatch(owner, context, rule.OwnerRole)) continue;
                if (!rule.MatchesAction(context)) continue;
                if (!ValidateCondition(rule, context)) continue;
                    
                float appliedValue = rule.Value;
                NotifyPerkTriggered(owner, perk, rule, context, appliedValue);
            }
        }
    }

    /// <summary>
    /// Avalia e dispara regras de um perk recém-aplicado via ativação manual de um Trick.
    /// </summary>
    public void EvaluateManualActivationTriggers(Battler owner, ActionType actionType, PerkRuntimeInstance perk)
    {
        if (owner == null || perk?.Definition?.Rules == null) return;
        
        CombatActionContext manualContext = new(owner, null, actionType);
        for (int i = 0; i < perk.Definition.Rules.Count; i++)
        {
            PerkRule rule = perk.Definition.Rules[i];
            
            if (rule == null || rule.Trigger != PerkTrigger.OnManualActivation) continue;
            
            NotifyPerkTriggered(owner, perk, rule, manualContext, rule.Value);
        }
    }

    private bool ValidateCondition(PerkRule rule, ActionResolutionContext context)
    {
        try { return PerkConditionFactory.Evaluate(rule.ConditionKey, context, rule.ConditionValue); }
        catch { return false; }
    }

    /// <summary>
    /// Valida condição de roll (Always, RollValueEquals, RollTierEquals, etc).
    /// </summary>
    private bool ValidateCondition(PerkRule rule, CombatRollContext context)
    {
        try { return PerkConditionFactory.Evaluate(rule.ConditionKey, context, rule.ConditionValue); }
        catch { return false; }
    }

    /// <summary>
    /// Valida condição de dice (análises de valores, tiers, somas).
    /// </summary>
    private bool ValidateDiceCondition(PerkRule rule, DiceResult dice, List<DiceResult> allDices = null, List<DiceResult> opposingDices = null)
    {
        try
        {
            if (rule.ConditionKey == PerkConditionKey.RollValueEquals || rule.ConditionKey == PerkConditionKey.RollTierEquals)
                return PerkConditionFactory.Evaluate(rule.ConditionKey, dice, rule.ConditionValue);

            if (rule.ConditionKey == PerkConditionKey.RollSumEquals && allDices != null)
            {
                if (allDices.Count == 0 || dice != allDices[0]) return false;
                var sumContext = new DiceRollSumContext { TotalSum = DiceRuntimeHelper.SumDice(allDices), Dices = allDices };
                return PerkConditionFactory.Evaluate(rule.ConditionKey, sumContext, rule.ConditionValue);
            }

            if (rule.ConditionKey == PerkConditionKey.RollSumEqualsAttackersRollSum && allDices != null && opposingDices != null)
            {
                if (allDices.Count == 0 || dice != allDices[0]) return false;
                var comparisonContext = new DefenseRollComparisonContext { DefenderRollSum = DiceRuntimeHelper.SumDice(allDices), AttackerRollSum = DiceRuntimeHelper.SumDice(opposingDices) };
                return PerkConditionFactory.Evaluate(rule.ConditionKey, comparisonContext, rule.ConditionValue);
            }

            return rule.ConditionKey == PerkConditionKey.Always;
        }
        catch { return false; }
    }

    /// <summary>
    /// Dispara o evento PerkTriggered com contexto completo.
    /// </summary>
    private void NotifyPerkTriggered(Battler owner, PerkRuntimeInstance perk, PerkRule rule, ICombatContext context, float appliedValue)
    {
        if (perk?.Definition == null) return;

        var triggerEvent = new PerkTriggeredEvent
        {
            PerkId = perk.Definition.Id,
            Owner = owner,
            SourceTrickId = perk.SourceTrickId,
            SourceTrickInstanceId = perk.SourceTrickInstanceId,
            SourceTrick = perk.SourceTrick,
            Trigger = rule.Trigger,
            ModifierTarget = rule.ModifierTarget,
            Operation = rule.Operation,
            AppliedValue = appliedValue,
            StacksApplied = perk.Stacks,
            FullContext = context,
            TriggerTime = Time.time
        };

        OnPerkTriggered?.Invoke(triggerEvent);
    }
}
