using System;

public class ActionResolverService
{
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

        bool ignoreAttack = (defenseAccuracyMaxTriggered && !attackAccuracyMaxTriggered) || attackAccuracy == ActionAccuracy.Missed;
        bool ignoreDefense = (attackAccuracyMaxTriggered && !defenseAccuracyMaxTriggered) || defenseAccuracy == ActionAccuracy.Missed;

        ActionResolutionResult result = new()
        {

            Accuracy = attackAccuracy,
            FinalTarget = target
        };

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

        float attackPower = ignoreAttack ? 0f : CalculatePower(attack);
        float defensePower = ignoreDefense ? 0f : CalculatePower(defense);
        int damage = (int)(attackPower - defensePower);

        if (damage < 0) damage = 0;
        if (damage <= target.Defense && !hasParried)
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
                ? "POWER MAX (EFFECT PLACEHOLDER)"
                : $"{result.FeedbackText} | POWER MAX (EFFECT PLACEHOLDER)";
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

    private float CalculatePower(ActionInstance action)
    {
        if (action == null || action.PowerDice == null)
            return 0f;

        float multiplier = GetMultiplier(action.PowerDice.Tier);
        return action.Definition.BasePower * multiplier;
    }

    private void TriggerPowerMaxPlaceholder(Battler attacker)
    {
        Console.WriteLine($"[Resolve] {attacker.Name} triggered POWER MAX placeholder effect.");
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
