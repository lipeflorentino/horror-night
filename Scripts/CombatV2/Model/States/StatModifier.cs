using System;

[Serializable]
public class StatModifier : BattlerStateModifier
{
    public BattlerStateStatType StatType;
    public ModifierOperation Operation = ModifierOperation.Multiply;
    public float Value = 1f;

    public bool MatchesStat(CombatActionContext context, BattlerStateStatType statType)
    {
        return StatType == statType && MatchesAction(context.ActionType);
    }
}
