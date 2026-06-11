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
    public event System.Action<PerkTriggeredEvent> OnPerkTriggered;

    private readonly PerkDatabase database;
    private readonly Dictionary<string, float> perkLastTriggerTime = new();

    public PerkTriggerEvaluator(PerkDatabase database)
    {
        this.database = database ?? PerkDatabase.GetOrCreateRuntimeDatabase();
    }

    /// <summary>
    /// Avalia perks acionados por roll (BeforeRoll trigger).
    /// Chama esta função ANTES de aplicar modificadores de roll.
    /// </summary>
    public void EvaluateRollTriggers(
        Battler owner,
        CombatRollContext context,
        PerkTrigger expectedTrigger,
        List<DiceResult> rolledDices = null)
    {
        if (owner?.Perks == null || owner.Perks.Count == 0)
            return;

        for (int i = 0; i < owner.Perks.Count; i++)
        {
            PerkRuntimeInstance perk = owner.Perks[i];
            if (perk?.Definition?.Rules == null)
                continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                // Validação 1: Role check (actor/opponent/defender/attacker)
                if (!IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                // Validação 2: Filtros de contexto (action type, roll type, stat type)
                if (!rule.MatchesRoll(context))
                    continue;

                // Validação 3: Condição específica (Always, RollValueEquals, etc)
                if (!ValidateCondition(rule, context))
                    continue;

                // ✅ TRIGGER! Dispara o evento
                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }

        // Avalia Identity Perks (sempre acionados)
        EvaluateIdentityPerks(owner, context, expectedTrigger);
    }

    /// <summary>
    /// Avalia perks acionados por dados (PowerMultiplier e AfterResolve triggers).
    /// Chama esta função com os dados já rolados.
    /// </summary>
    public void EvaluateDiceTriggers(
        Battler owner,
        CombatActionContext context,
        DiceResult dice,
        PerkTrigger expectedTrigger,
        List<DiceResult> allDices = null)
    {
        if (owner?.Perks == null || owner.Perks.Count == 0)
            return;

        if (dice == null)
            return;

        for (int i = 0; i < owner.Perks.Count; i++)
        {
            PerkRuntimeInstance perk = owner.Perks[i];
            if (perk?.Definition?.Rules == null)
                continue;

            for (int j = 0; j < perk.Definition.Rules.Count; j++)
            {
                PerkRule rule = perk.Definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                // Validação 1: Role check
                if (!IsRoleMatch(owner, context, rule.OwnerRole))
                    continue;

                // Validação 2: Filtros de ação
                if (!rule.MatchesAction(context))
                    continue;

                // Validação 3: Filtros de dado (tipo, stat, tier)
                if (!rule.MatchesDice(dice))
                    continue;

                // Validação 4: Condição específica para dice
                if (!ValidateDiceCondition(rule, dice, allDices))
                    continue;

                // ✅ TRIGGER! Dispara o evento
                NotifyPerkTriggered(owner, perk, rule, context, rule.Value);
            }
        }

        // Avalia Identity Perks
        EvaluateIdentityPerksForDice(owner, context, dice, expectedTrigger, allDices);
    }

    /// <summary>
    /// Avalia perks de identidade que são sempre ativos.
    /// </summary>
    private void EvaluateIdentityPerks(Battler owner, CombatRollContext context, PerkTrigger expectedTrigger)
    {
        if (owner == null)
            return;

        List<PerkSO> identityPerks = database.GetIdentityPerks();
        for (int i = 0; i < identityPerks.Count; i++)
        {
            PerkSO definition = identityPerks[i];
            if (definition?.Rules == null)
                continue;

            for (int j = 0; j < definition.Rules.Count; j++)
            {
                PerkRule rule = definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                if (!rule.MatchesRoll(context))
                    continue;

                // Identity perks com Always condition são sempre acionados
                if (rule.ConditionKey != PerkConditionKey.Always)
                    continue;

                var tempPerk = new PerkRuntimeInstance(definition, null, -1, 1);
                NotifyPerkTriggered(owner, tempPerk, rule, context, rule.Value);
            }
        }
    }

    private void EvaluateIdentityPerksForDice(
        Battler owner,
        CombatActionContext context,
        DiceResult dice,
        PerkTrigger expectedTrigger,
        List<DiceResult> allDices)
    {
        if (owner == null)
            return;

        List<PerkSO> identityPerks = database.GetIdentityPerks();
        for (int i = 0; i < identityPerks.Count; i++)
        {
            PerkSO definition = identityPerks[i];
            if (definition?.Rules == null)
                continue;

            for (int j = 0; j < definition.Rules.Count; j++)
            {
                PerkRule rule = definition.Rules[j];
                if (rule == null || rule.Trigger != expectedTrigger)
                    continue;

                if (!rule.MatchesDice(dice))
                    continue;

                if (rule.ConditionKey != PerkConditionKey.Always)
                    continue;

                var tempPerk = new PerkRuntimeInstance(definition, null, -1, 1);
                NotifyPerkTriggered(owner, tempPerk, rule, context, rule.Value);
            }
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
    private bool ValidateDiceCondition(PerkRule rule, DiceResult dice, List<DiceResult> allDices = null)
    {
        try
        {
            // Para RollValueEquals e RollTierEquals, usa o dice diretamente
            if (rule.ConditionKey == PerkConditionKey.RollValueEquals ||
                rule.ConditionKey == PerkConditionKey.RollTierEquals)
            {
                return PerkConditionFactory.Evaluate(rule.ConditionKey, dice, rule.ConditionValue);
            }

            // Para RollSumEquals, precisa de contexto com a soma
            if (rule.ConditionKey == PerkConditionKey.RollSumEquals && allDices != null)
            {
                int totalSum = 0;
                for (int i = 0; i < allDices.Count; i++)
                    totalSum += allDices[i].Value;

                var sumContext = new DiceRollSumContext { TotalSum = totalSum, Dices = allDices };
                return PerkConditionFactory.Evaluate(rule.ConditionKey, sumContext, rule.ConditionValue);
            }

            // Para Always
            return rule.ConditionKey == PerkConditionKey.Always;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Erro ao validar condição de dice {rule.ConditionKey}: {ex.Message}");
            return false;
        }
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
            Trigger = rule.Trigger,
            ModifierTarget = rule.ModifierTarget,
            AppliedValue = appliedValue,
            StacksApplied = perk.Stacks,
            FullContext = context,
            TriggerTime = Time.time
        };

        OnPerkTriggered?.Invoke(triggerEvent);

        // Debug log
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
