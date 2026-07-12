using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ActionResolverService
{
    private readonly PerkService perkService;

    public ActionResolverService(PerkService perkService = null)
    {
        this.perkService = perkService;
    }
    public ActionResolutionResult Resolve(ActionInstance attack, ActionInstance defense, Battler attacker, Battler target)
    {
        ActionAccuracy attackAccuracy = CalculateAccuracy(attack);
        ActionAccuracy defenseAccuracy = CalculateAccuracy(defense);

        bool attackPowerMaxTriggered = attack?.PowerDice != null && attack.PowerDice.IsMaxRoll;
        bool defensePowerMaxTriggered = defense?.PowerDice != null && defense.PowerDice.IsMaxRoll;
        bool attackAccuracyMaxTriggered = attack?.AccuracyDice != null && attack.AccuracyDice.IsMaxRoll;
        bool defenseAccuracyMaxTriggered = defense?.AccuracyDice != null && defense.AccuracyDice.IsMaxRoll;

        PowerMaxSource powerMaxSource = PowerMaxSource.None;
        if (attackPowerMaxTriggered)
            powerMaxSource |= PowerMaxSource.Attack;
        if (defensePowerMaxTriggered)
            powerMaxSource |= PowerMaxSource.Defense;

        bool ignoreAttack = (defenseAccuracyMaxTriggered && defense?.AccuracyDice != null && attack?.AccuracyDice != null && defense.AccuracyDice.Value > attack.AccuracyDice.Value) || attackAccuracy == ActionAccuracy.Missed;
        bool ignoreDefense = (attackAccuracyMaxTriggered && attack?.AccuracyDice != null && defense?.AccuracyDice != null && attack.AccuracyDice.Value > defense.AccuracyDice.Value) || defenseAccuracy == ActionAccuracy.Missed;

        ActionResolutionResult result = new()
        {
            Accuracy = attackAccuracy,
            FinalTarget = target
        };

        if (attackAccuracy == ActionAccuracy.Missed)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Missed;
            result.DefenseOutcome = DefenseOutcome.None;
            result.IgnoreAttack = false;
            result.IgnoreDefense = false;
            result.PowerMaxSource = powerMaxSource;
            result.ResolutionVariation = ActionResolutionVariation.Missed;
            ApplyFeedback(result);

            EvaluateTriggers(attacker, target, attack, defense, result);
            return result;
        }

        if (ignoreAttack)
        {
            result.Outcome = defensePowerMaxTriggered ? ActionOutcome.Parried : ActionOutcome.Evaded;
            result.DefenseOutcome = defensePowerMaxTriggered ? DefenseOutcome.Parried : DefenseOutcome.Evaded;
            result.Damage = 0;
            result.IgnoreAttack = true;
            result.IgnoreDefense = false;
            result.PowerMaxSource = powerMaxSource;
            result.ResolutionVariation = attackAccuracy == ActionAccuracy.Critical
                ? ActionResolutionVariation.FierceDefense
                : (defensePowerMaxTriggered ? ActionResolutionVariation.Parried : ActionResolutionVariation.Evaded);
            ApplyFeedback(result);

            EvaluateTriggers(attacker, target, attack, defense, result);
            return result;
        }

        int attackPower = CalculatePower(attack, attacker, target, ActionType.Attack);
        int defensePower = !ignoreDefense ? CalculatePower(defense, target, attacker, ActionType.Defense) : 0;
        int damage = attackPower - defensePower;

        damage = perkService?.ApplyDamageModifiers(damage, attack, attacker, target, ActionType.Attack, defense) ?? damage;
        damage = perkService?.ApplyDamageModifiers(damage, defense, target, attacker, ActionType.Defense, attack) ?? damage;

        Logger.Log($"Damage Calculation: Attack Power ({attackPower}) - Defense Power ({defensePower}) = {damage}");

        if (damage <= 0)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Blocked;
            result.DefenseOutcome = DefenseOutcome.Blocked;
            result.IgnoreAttack = false;
            result.IgnoreDefense = false;
            result.PowerMaxSource = powerMaxSource;
            result.ResolutionVariation = ActionResolutionVariation.Blocked;
            ApplyFeedback(result);

            EvaluateTriggers(attacker, target, attack, defense, result);
            return result;
        }

        result.Damage = damage;
        result.Outcome = attackAccuracy == ActionAccuracy.Critical ? ActionOutcome.CriticalHit : ActionOutcome.Hit;
        result.DefenseOutcome = DefenseOutcome.None;
        result.IgnoreAttack = false;
        result.IgnoreDefense = ignoreDefense;
        result.PowerMaxSource = powerMaxSource;
        result.ResolutionVariation = ResolveVariation(result);

        if (powerMaxSource.HasFlag(PowerMaxSource.Attack))
            TriggerPowerMaxPlaceholder(attacker, isAttackerSource: true);

        if (powerMaxSource.HasFlag(PowerMaxSource.Defense))
            TriggerPowerMaxPlaceholder(target, isAttackerSource: false);

        ApplyFeedback(result);

        EvaluateTriggers(attacker, target, attack, defense, result);
        return result;
    }

    // Diferencia Power Max de ataque, defesa ou ambos ao decidir a variação de resolução.
    private ActionResolutionVariation ResolveVariation(ActionResolutionResult result)
    {
        bool attackPowerMax = result.PowerMaxSource.HasFlag(PowerMaxSource.Attack);
        bool defensePowerMax = result.PowerMaxSource.HasFlag(PowerMaxSource.Defense);
        bool bothPowerMax = attackPowerMax && defensePowerMax;

        if (result.Outcome == ActionOutcome.CriticalHit)
        {
            if (bothPowerMax)
                return ActionResolutionVariation.LegendaryClash;

            if (result.IgnoreDefense && attackPowerMax)
                return ActionResolutionVariation.Deathstroke;

            if (result.IgnoreDefense)
                return ActionResolutionVariation.DevastatingStrike;

            if (attackPowerMax)
                return ActionResolutionVariation.Overpower;

            if (defensePowerMax)
                return ActionResolutionVariation.IronWall;

            return ActionResolutionVariation.CriticalHit;
        }

        if (result.Outcome == ActionOutcome.Hit)
        {
            if (bothPowerMax)
                return ActionResolutionVariation.LegendaryClash;

            if (result.IgnoreDefense && attackPowerMax)
                return ActionResolutionVariation.ArmorShatter;

            if (result.IgnoreDefense)
                return ActionResolutionVariation.PiercingHit;

            if (attackPowerMax)
                return ActionResolutionVariation.PowerHit;

            if (defensePowerMax)
                return ActionResolutionVariation.Stronghold;

            return ActionResolutionVariation.Hit;
        }

        return result.Outcome switch
        {
            ActionOutcome.Missed => ActionResolutionVariation.Missed,
            ActionOutcome.Blocked => ActionResolutionVariation.Blocked,
            ActionOutcome.Parried => ActionResolutionVariation.Parried,
            ActionOutcome.Evaded => ActionResolutionVariation.Evaded,
            _ => ActionResolutionVariation.None
        };
    }

    // Ponto único de geração dos feedbacks (ataque e defesa), a partir do estado já resolvido.
    // Feedback de ataque só existe quando o ataque teve sucesso (Hit/CriticalHit) - sem block, evade ou parry.
    private void ApplyFeedback(ActionResolutionResult result)
    {
        bool showAttackFeedback = result.Outcome == ActionOutcome.Hit || result.Outcome == ActionOutcome.CriticalHit || result.Outcome == ActionOutcome.Missed;

        result.AttackFeedbackText = showAttackFeedback ? BuildAttackFeedback(result) : string.Empty;
        result.DefenseFeedbackText = BuildDefenseFeedback(result);
    }

    private string BuildAttackFeedback(ActionResolutionResult result)
    {
        return result.ResolutionVariation switch
        {
            ActionResolutionVariation.LegendaryClash => "LEGENDARY CLASH",
            ActionResolutionVariation.Deathstroke => "DEATHSTROKE",
            ActionResolutionVariation.Overpower => "OVERPOWER",
            ActionResolutionVariation.DevastatingStrike => "DEVASTATING STRIKE",
            ActionResolutionVariation.ArmorShatter => "ARMOR SHATTER",
            ActionResolutionVariation.PowerHit => "POWER HIT",
            ActionResolutionVariation.PiercingHit => "PIERCING HIT",
            ActionResolutionVariation.CriticalHit => "CRITICAL HIT!",
            ActionResolutionVariation.Hit => "HIT",
            ActionResolutionVariation.Missed => "MISSED",
            ActionResolutionVariation.IronWall => "CRITICAL HIT!",
            ActionResolutionVariation.Stronghold => "HIT",
            _ => string.Empty
        };
    }

    private string BuildDefenseFeedback(ActionResolutionResult result)
    {
        if (result.ResolutionVariation == ActionResolutionVariation.LegendaryClash)
            return "LEGENDARY CLASH";

        if (result.ResolutionVariation == ActionResolutionVariation.FierceDefense)
            return "FIERCE DEFENSE";

        if (result.ResolutionVariation == ActionResolutionVariation.IronWall)
            return "IRON WALL";

        if (result.ResolutionVariation == ActionResolutionVariation.Stronghold)
            return "STRONGHOLD";

        if (result.IgnoreDefense)
            return "GUARD BROKEN";

        return result.DefenseOutcome switch
        {
            DefenseOutcome.Evaded => "EVADED",
            DefenseOutcome.Parried => "PARRIED",
            DefenseOutcome.Blocked => "BLOCKED",
            _ => string.Empty
        };
    }

    private void EvaluateTriggers(Battler attacker, Battler target, ActionInstance attack, ActionInstance defense, ActionResolutionResult result)
    {
        if (perkService != null)
        {
            perkService.EvaluateActionResolutionTriggers(attacker, target, attack.Definition?.Type ?? ActionType.Attack, result.Outcome, result.ResolutionVariation);
            perkService.EvaluateActionResolutionTriggers(target, attacker, defense?.Definition?.Type ?? ActionType.Defense, result.Outcome, result.ResolutionVariation);
        }
    }

    private ActionAccuracy CalculateAccuracy(ActionInstance action)
    {
        if (action == null || action.AccuracyDice == null)
            return ActionAccuracy.Missed;

        return action.AccuracyDice.Tier switch
        {
            DiceTier.Low => ActionAccuracy.Missed,
            DiceTier.Medium => ActionAccuracy.Hit,
            DiceTier.High => ActionAccuracy.Critical,
            _ => ActionAccuracy.Hit,
        };
    }

    public int CalculatePower(ActionInstance action)
    {
        return CalculatePower(action, null, null, action?.Definition != null ? action.Definition.Type : ActionType.Attack);
    }

    public int CalculatePower(ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action == null || action.PowerDice == null)
            return 0;

        float multiplier = GetMultiplier(action.PowerDice.Tier);
        multiplier = perkService?.GetPowerMultiplier(multiplier, action, actor, opponent, actionType) ?? multiplier;
        return UnityEngine.Mathf.RoundToInt(action.Definition.BasePower * multiplier);
    }

    private void TriggerPowerMaxPlaceholder(Battler battler, bool isAttackerSource)
    {
        string role = isAttackerSource ? "attack" : "defense";
        Logger.Log($"[Resolve] {battler.Name} triggered POWER MAX effect ({role}).");
    }

    private float GetMultiplier(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.Low => 0.5f,
            DiceTier.Medium => 1f,
            DiceTier.High => 1.5f,
            _ => 1f,
        };
    }
}