using System;

[Serializable]
public class PerkRule
{
    public PerkTrigger Trigger = PerkTrigger.BeforeRoll;
    public PerkModifierTarget ModifierTarget = PerkModifierTarget.ExtraDice;
    public PerkOperation Operation = PerkOperation.Add;
    public BattlerStateRole OwnerRole = BattlerStateRole.OwnerAsActor;
    public ActionType ActionType;
    public bool FilterByActionType;
    public DiceRollType RollType;
    public bool FilterByRollType;
    public DiceStatType StatType;
    public bool FilterByStatType;
    public DiceTier Tier;
    public bool FilterByTier;
    public PerkConditionKey ConditionKey = PerkConditionKey.Always;
    public string ConditionValue;
    public float Value;

    public bool MatchesRoll(CombatRollContext context)
    {
        if (FilterByActionType && ActionType != context.ActionType)
            return false;

        if (FilterByRollType && RollType != context.RollType)
            return false;

        if (FilterByStatType && StatType != context.StatType)
            return false;

        return true;
    }

    public bool MatchesAction(CombatActionContext context)
    {
        return !FilterByActionType || ActionType == context.ActionType;
    }

    public bool MatchesAction(ActionResolutionContext context)
    {
        return !FilterByActionType || ActionType == context.ActionType;
    }

    public bool MatchesDice(DiceResult dice)
    {
        if (dice == null)
            return false;

        if (FilterByRollType && RollType != dice.RollType)
            return false;

        if (FilterByStatType && StatType != dice.StatType)
            return false;

        if (FilterByTier && Tier != dice.Tier)
            return false;

        return ConditionKey switch
        {
            PerkConditionKey.RollValueEquals => int.TryParse(ConditionValue, out int expectedValue) && dice.Value == expectedValue,
            PerkConditionKey.RollTierEquals => Enum.TryParse(ConditionValue, true, out DiceTier expectedTier) && dice.Tier == expectedTier,
            _ => true
        };
    }
}
