using UnityEngine;

public static class BattlerStateCatalog
{
    public const string ExposedId = "exposed";
    public const string AngryId = "angry";
    public const string CautiousId = "cautious";

    public static BattlerStateDefinition CreateExposed()
    {
        BattlerStateDefinition state = CreateState(ExposedId, "Exposed");
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsTarget,
            ActionType.Attack,
            DiceRollType.Accuracy,
            ThresholdBand.High,
            -0.15f));
        state.BattlerStatModifiers.Add(CreateStatModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            BattlerStateStatType.Defense,
            0.90f));
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            DiceRollType.Accuracy,
            ThresholdBand.High,
            0.10f));
        return state;
    }

    public static BattlerStateDefinition CreateAngry()
    {
        BattlerStateDefinition state = CreateState(AngryId, "Angry");
        state.BattlerStatModifiers.Add(CreateStatModifier(
            BattlerStateRole.OwnerAsAttacker,
            ActionType.Attack,
            BattlerStateStatType.Attack,
            1.10f));
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsAttacker,
            ActionType.Attack,
            DiceRollType.Power,
            ThresholdBand.High,
            -0.15f));
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsAttacker,
            ActionType.Attack,
            DiceRollType.Power,
            ThresholdBand.Low,
            -0.15f));
        state.BattlerStatModifiers.Add(CreateStatModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            BattlerStateStatType.Defense,
            0.90f));
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            DiceRollType.Accuracy,
            ThresholdBand.High,
            0.15f));
        return state;
    }

    public static BattlerStateDefinition CreateCautious()
    {
        BattlerStateDefinition state = CreateState(CautiousId, "Cautious");
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            DiceRollType.Accuracy,
            ThresholdBand.High,
            -0.15f));
        state.ThresholdModifiers.Add(CreateThresholdModifier(
            BattlerStateRole.OwnerAsDefender,
            ActionType.Defense,
            DiceRollType.Accuracy,
            ThresholdBand.Low,
            -0.15f));
        return state;
    }

    private static BattlerStateDefinition CreateState(string id, string displayName)
    {
        BattlerStateDefinition state = ScriptableObject.CreateInstance<BattlerStateDefinition>();
        state.Id = id;
        state.DisplayName = displayName;
        state.DefaultDurationTurns = 1;
        state.MaxStacks = 1;
        state.StackMode = BattlerStateStackMode.RefreshDuration;
        return state;
    }

    private static ThresholdModifier CreateThresholdModifier(BattlerStateRole role, ActionType actionType, DiceRollType rollType, ThresholdBand band, float value)
    {
        return new ThresholdModifier
        {
            Role = role,
            FilterByActionType = true,
            ActionType = actionType,
            FilterByRollType = true,
            RollType = rollType,
            Band = band,
            Operation = ModifierOperation.Add,
            Value = value
        };
    }

    private static BattlerStatModifier CreateStatModifier(BattlerStateRole role, ActionType actionType, BattlerStateStatType statType, float multiplier)
    {
        return new BattlerStatModifier
        {
            Role = role,
            FilterByActionType = true,
            ActionType = actionType,
            StatType = statType,
            Operation = ModifierOperation.Multiply,
            Value = multiplier
        };
    }
}
