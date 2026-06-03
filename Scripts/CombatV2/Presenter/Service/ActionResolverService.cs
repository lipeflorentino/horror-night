using System;

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

        bool attackPowerMaxTriggered = attack.PowerDice != null && attack.PowerDice.IsMaxRoll;
        bool defensePowerMaxTriggered = defense.PowerDice != null && defense.PowerDice.IsMaxRoll;
        bool attackAccuracyMaxTriggered = attack.AccuracyDice != null && attack.AccuracyDice.IsMaxRoll;
        bool defenseAccuracyMaxTriggered = defense.AccuracyDice != null && defense.AccuracyDice.IsMaxRoll;
        
        bool hasEvaded = false;
        bool hasParried = false;

        bool ignoreAttack = (defenseAccuracyMaxTriggered && defense.AccuracyDice.Value > attack.AccuracyDice.Value && !attackAccuracyMaxTriggered) || attackAccuracy == ActionAccuracy.Missed;
        bool ignoreDefense = (attackAccuracyMaxTriggered && attack.AccuracyDice.Value > defense.AccuracyDice.Value && !defenseAccuracyMaxTriggered) || defenseAccuracy == ActionAccuracy.Missed;

        ActionResolutionResult result = new()
        {
            Accuracy = attackAccuracy,
            FinalTarget = target
        };

        int attackPower = ignoreAttack ? 0 : CalculatePower(attack, attacker, target, ActionType.Attack);
        int defensePower = ignoreDefense ? 0 : CalculatePower(defense, target, attacker, ActionType.Defense);
        int damage = attackPower - defensePower;
        damage = perkService?.ApplyDamageModifiers(damage, attack, attacker, target, ActionType.Attack) ?? damage;

        Logger.Log($"Damage Calculation: Attack Power ({attackPower}) - Defense Power ({defensePower}) = {damage}");
        
        result.AttackPowerLogText = $"Attack Power: {attackPower}";
        result.DefensePowerLogText = $"Defense Power: {defensePower}";
        result.AttackAccuracyLogText = $"Attack Accuracy: {attackAccuracy}";
        result.DefenseAccuracyLogText = $"Defense Accuracy: {defenseAccuracy}";

        if (result.Accuracy == ActionAccuracy.Missed)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Missed;
            result.FeedbackText = "MISSED";
            return result;
        }

        if (ignoreAttack)
        {
            if (defensePowerMaxTriggered)
            {
                hasParried = true;
            } 
            else
            {
                hasEvaded = true;
            }
        }

        if (hasEvaded)
        {
            result.Outcome = ActionOutcome.Evaded;
            result.FeedbackText = "EVADED";
            return result;
        }

        if (hasParried)
        {
            result.Outcome = ActionOutcome.Parried;
            result.FeedbackText = "PARRIED";
        }

        if (damage <= 0 && !hasParried && !hasEvaded)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Blocked;
            result.FeedbackText = "BLOCKED";
            return result;
        }

        result.Damage = damage;
        result.Outcome = result.Accuracy == ActionAccuracy.Critical ? ActionOutcome.CriticalHit : ActionOutcome.Hit;
        result.FeedbackText = result.Accuracy == ActionAccuracy.Critical ? "CRITICAL HIT!" : string.Empty;

        if (ignoreAttack)
        {
             result.FeedbackText = string.IsNullOrEmpty(result.FeedbackText)
                ? "ATTACK IGNORED!"
                : $"{result.FeedbackText} | ATTACK IGNORED!";
        }

        if (ignoreDefense)
        {
            result.FeedbackText = string.IsNullOrEmpty(result.FeedbackText)
                ? "DEFENSE IGNORED!"
                : $"{result.FeedbackText} | DEFENSE IGNORED!";
        }

        if (attackPowerMaxTriggered || defensePowerMaxTriggered)
        {
            TriggerPowerMaxPlaceholder(attacker);
            result.FeedbackText = string.IsNullOrEmpty(result.FeedbackText)
                ? "POWER MAX"
                : $"{result.FeedbackText} | POWER MAX";
        }

        return result;
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

    private void TriggerPowerMaxPlaceholder(Battler attacker)
    {
        Logger.Log($"[Resolve] {attacker.Name} triggered POWER MAX effect.");
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
