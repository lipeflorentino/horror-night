using UnityEngine;

public class ActionResolverService
{
    private readonly PerkService perkService;
    private readonly DrawbackService drawbackService;
    private readonly BattlerStateService battlerStateService;

    public ActionResolverService(PerkService perkService = null, DrawbackService drawbackService = null, BattlerStateService battlerStateService = null)
    {
        this.perkService = perkService;
        this.drawbackService = drawbackService;
        this.battlerStateService = battlerStateService;
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

        int attackAccuracyContest = GetAccuracyContestValue(attack?.AccuracyDice);
        int defenseAccuracyContest = GetAccuracyContestValue(defense?.AccuracyDice);
        bool ignoreAttack = (defenseAccuracyMaxTriggered && defense?.AccuracyDice != null && attack?.AccuracyDice != null && defenseAccuracyContest > attackAccuracyContest) || attackAccuracy == ActionAccuracy.Missed;
        bool ignoreDefense = (attackAccuracyMaxTriggered && attack?.AccuracyDice != null && defense?.AccuracyDice != null && attackAccuracyContest > defenseAccuracyContest) || defenseAccuracy == ActionAccuracy.Missed;

        ActionResolutionResult result = new()
        {
            Accuracy = attackAccuracy,
            FinalTarget = target,
            DamageBonus = 0,
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

        result.Outcome = attackAccuracy == ActionAccuracy.Critical ? ActionOutcome.CriticalHit : ActionOutcome.Hit;
        result.DefenseOutcome = DefenseOutcome.None;
        result.IgnoreAttack = false;
        result.IgnoreDefense = ignoreDefense;
        result.PowerMaxSource = powerMaxSource;
        result.ResolutionVariation = ResolveVariation(result);

        bool isDefensiveReductionVariation = result.ResolutionVariation == ActionResolutionVariation.IronWall ||
            result.ResolutionVariation == ActionResolutionVariation.Stronghold;

        if (damage <= 0 && !isDefensiveReductionVariation)
        {
            result.Damage = 0;
            result.Outcome = ActionOutcome.Blocked;
            result.DefenseOutcome = DefenseOutcome.Blocked;
            result.IgnoreAttack = false;
            result.IgnoreDefense = false;
            result.PowerMaxSource = powerMaxSource;
            result.ResolutionVariation = ActionResolutionVariation.Blocked;
            result.DamageBonus = 0;
            ApplyFeedback(result);

            EvaluateTriggers(attacker, target, attack, defense, result);
            return result;
        }
        
        result.DamageBonus = CombatRules.GetDamageBonus(result.ResolutionVariation);
        result.Damage = Mathf.Max(0, damage + result.DamageBonus);

        ApplyFeedback(result);
        EvaluateTriggers(attacker, target, attack, defense, result);

        if (attacker.ActionSecondaryEffects.TryGetValue(result.ResolutionVariation, out var effectsToApply))
        {
            foreach (var payload in effectsToApply)
            {
                ApplySecondaryEffectToTarget(target, payload, attacker);
            }
        }

        return result;
    }

    private int GetAccuracyContestValue(DiceResult accuracyDice)
    {
        if (accuracyDice == null)
            return 0;

        return accuracyDice.Value + (accuracyDice.StatType == DiceStatType.Mind && accuracyDice.IsMaxRoll ? 1 : 0);
    }

    private void EvaluateTriggers(Battler attacker, Battler target, ActionInstance attack, ActionInstance defense, ActionResolutionResult result)
    {
        if (perkService != null)
        {
            perkService.EvaluateActionResolutionTriggers(attacker, target, attack.Definition?.Type ?? ActionType.Attack, result.Outcome, result.ResolutionVariation, result.Damage, result.FinalTarget);
            perkService.EvaluateActionResolutionTriggers(target, attacker, defense?.Definition?.Type ?? ActionType.Defense, result.Outcome, result.ResolutionVariation, result.Damage, result.FinalTarget);
        }
    }

    private ActionAccuracy CalculateAccuracy(ActionInstance action)
    {
        if (action == null || action.AccuracyDice == null)
            return ActionAccuracy.Missed;

        return CombatRules.GetAccuracyOutcome(action.AccuracyDice.Tier);
    }

    public int CalculatePower(ActionInstance action)
    {
        return CalculatePower(action, null, null, action?.Definition != null ? action.Definition.Type : ActionType.Attack);
    }

    public int CalculatePower(ActionInstance action, Battler actor, Battler opponent, ActionType actionType)
    {
        if (action == null || action.PowerDice == null)
            return 0;

        float combinedMultiplier = CombatRules.GetPowerMultiplier(action.PowerDice.StatType, action.PowerDice.Tier);
        combinedMultiplier *= CombatRules.GetCommitmentMultiplier(action.AllocatedPowerDiceCount);
        combinedMultiplier = perkService?.GetPowerMultiplier(combinedMultiplier, action, actor, opponent, actionType) ?? combinedMultiplier;
        
        return Mathf.RoundToInt(action.Definition.BasePower * combinedMultiplier);
    }
    
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
    
    // Feedback de ataque só existe quando o ataque teve sucesso (Hit/CriticalHit) - sem block, evade ou parry.
    private void ApplyFeedback(ActionResolutionResult result)
    {
        bool showAttackFeedback = result.Outcome == ActionOutcome.Hit || result.Outcome == ActionOutcome.CriticalHit || result.Outcome == ActionOutcome.Missed;
        
        result.DamageBonusFeedbackText = result.DamageBonus > 0 
            ? $"  <color=#FFFFFF>{"with"} </color> <color=#FFD700>{result.Damage} (+{result.DamageBonus}) DMG</color>" 
            : (result.DamageBonus < 0 
                ? $"  <color=#FFFFFF>{"with"}</color> <color=#FFD700>{result.Damage} ({result.DamageBonus}) DMG</color>" 
                : string.Empty);
                
        result.AttackFeedbackText = showAttackFeedback ? BuildAttackFeedback(result) : string.Empty;
        result.DefenseFeedbackText = BuildDefenseFeedback(result);

        if (result.DamageBonus > 0 && showAttackFeedback)
        {
            result.AttackFeedbackText += result.DamageBonusFeedbackText;
        }
        else if (result.DamageBonus < 0)
        {
            result.DefenseFeedbackText += result.DamageBonusFeedbackText;
        }
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

    private void ApplySecondaryEffectToTarget(Battler target, ActionEffectPayload payload, Battler source)
    {
        if (target == null || string.IsNullOrWhiteSpace(payload.EffectId) || drawbackService == null)
            return;
        
        if (payload.Type == ActionEffectType.Drawback)
        {
            DrawbackRuntimeInstance drawback = drawbackService.ApplyDrawback(target, payload.EffectId, source);
            if (drawback == null)
            {
                Logger.Log($"[Resolve] Secondary effect '{payload.EffectName}' was not applied because no drawback definition exists yet.");
            }
        }
        if (payload.Type == ActionEffectType.BattlerState)
        {
            BattlerStateRuntimeInstance state = battlerStateService.ApplyBattlerState(target, payload.EffectId, source);
            if (state == null)
            {
                Logger.Log($"[Resolve] Secondary effect '{payload.EffectName}' was not applied because no state definition exists yet.");
            }
        }
    }
}
