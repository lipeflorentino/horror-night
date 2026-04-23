using System;

public class ActionResolverService
{
    public ActionResolutionResult Resolve(ActionInstance attack, ActionInstance defense, Battler attacker, Battler target)
    {
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
        float defensePower = CalculatePower(defense);

        int damage = (int)(attackPower - defensePower);
        if (damage < 0) damage = 0;

        if (damage <= target.Defense)
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
        Console.WriteLine($"[Resolve] Attacker: {attacker.Name} | AttackPower: {attackPower} | DefensePower: {defensePower} | Damage: {damage}");

        return result;
    }

    private ActionAccuracy CalculateAccuracy(ActionInstance action)
    {
        return action.Dice.Tier switch
        {
            DiceTier.Low => ActionAccuracy.Missed,
            DiceTier.Medium => ActionAccuracy.Hit,
            DiceTier.High => ActionAccuracy.Hit,
            _ => ActionAccuracy.Hit,
        };
    }

    private HitQuality CalculateHitQuality(ActionInstance action)
    {
        return action.Dice.Tier == DiceTier.High ? HitQuality.Critical : HitQuality.Normal;
    }

    private float CalculatePower(ActionInstance action)
    {
        float multiplier = GetMultiplier(action.Dice.Tier);
        return action.Definition.BasePower * multiplier;
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
