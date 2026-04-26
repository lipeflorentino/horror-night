using System;
using System.Diagnostics;

public class ActionResolverService
{
    public ActionResolutionResult Resolve(ActionInstance attack, ActionInstance defense, Battler attacker, Battler target)
    {
        bool ignoreDefense = attack.AccuracyDice != null && attack.AccuracyDice.IsMaxRoll;
        bool powerMaxTriggered = attack.PowerDice != null && attack.PowerDice.IsMaxRoll;

        ActionResolutionResult result = new()

        {
            Accuracy = CalculateAccuracy(attack),
            HitQuality = CalculateHitQuality(attack)
        };

        if (result.Accuracy == ActionAccuracy.Missed)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Missed;
            result.FeedbackText = "MISSED";
            return result;
        }

        float attackPower = CalculatePower(attack);
        float defensePower = ignoreDefense ? 0f : CalculatePower(defense) * GetDefenseAccuracyMultiplier(defense);
        int damage = (int)(attackPower - defensePower);

        Console.WriteLine($"[Resolve] Calculated attack power: {attackPower} | defense power: {defensePower} | preliminary damage: {damage}");

        if (damage < 0) damage = 0;
        if (!ignoreDefense && damage <= target.Defense)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Blocked;
            result.FeedbackText = "BLOCKED";
            Console.WriteLine($"[Resolve] Attack blocked. Damage: {damage} | TargetDefense: {target.Defense}");
            return result;
        }

        result.Damage = damage;
        result.Outcome = result.HitQuality == HitQuality.Critical ? ActionOutcome.CriticalHit : ActionOutcome.Hit;
        result.FeedbackText = result.HitQuality == HitQuality.Critical ? "CRITICAL HIT!" : string.Empty;
        if (ignoreDefense)
        {
            result.FeedbackText = string.IsNullOrEmpty(result.FeedbackText)
                ? "DEFENSE IGNORED!"
                : $"{result.FeedbackText} | DEFENSE IGNORED!";
        }

        if (powerMaxTriggered)
        {
            TriggerPowerMaxPlaceholder(attacker);
            result.FeedbackText = string.IsNullOrEmpty(result.FeedbackText)
                ? "POWER MAX (EFFECT PLACEHOLDER)"
                : $"{result.FeedbackText} | POWER MAX (EFFECT PLACEHOLDER)";
        }

        Console.WriteLine($"[Resolve] Attacker: {attacker.Name} | AttackPower: {attackPower} | DefensePower: {defensePower} | Damage: {damage}");

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
            DiceTier.High => ActionAccuracy.Hit,
            _ => ActionAccuracy.Hit,
        };
    }

    private HitQuality CalculateHitQuality(ActionInstance action)
    {
        if (action == null || action.AccuracyDice == null)
            return HitQuality.Normal;

        return action.AccuracyDice.Tier == DiceTier.High ? HitQuality.Critical : HitQuality.Normal;
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

    private float GetDefenseAccuracyMultiplier(ActionInstance defense)
    {
        if (defense == null || defense.AccuracyDice == null)
            return 0.5f;

        return defense.AccuracyDice.Tier switch
        {
            DiceTier.Low => 0.5f,
            DiceTier.Medium => 1f,
            DiceTier.High => 1.25f,
            _ => 1f
        };
    }
}
