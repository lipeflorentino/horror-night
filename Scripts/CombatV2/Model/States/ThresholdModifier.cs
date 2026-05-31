using System;

[Serializable]
public class ThresholdModifier : BattlerStateModifier
{
    public DiceRollType RollType;
    public bool FilterByRollType = true;
    public DiceStatType StatType;
    public bool FilterByStatType;
    public ThresholdBand Band;
    public ModifierOperation Operation = ModifierOperation.Add;
    public float Value;

    public bool MatchesRoll(CombatRollContext context)
    {
        if (!MatchesAction(context.ActionType))
            return false;

        if (FilterByRollType && RollType != context.RollType)
            return false;

        if (FilterByStatType && StatType != context.StatType)
            return false;

        return true;
    }
}
