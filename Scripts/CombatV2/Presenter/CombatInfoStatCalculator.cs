using System.Collections.Generic;

// Presenter-side helper: computes display-ready stat data (value, delta, sign).
// Kept out of CombatInfoPanelView to respect MVP separation (View must not decide game logic).
public static class CombatInfoStatCalculator
{
    public readonly struct StatDisplayData
    {
        public readonly string Value;
        public readonly string DeltaText;
        public readonly bool ShowDelta;
        public readonly bool PositiveDelta;

        public StatDisplayData(string value, string deltaText, bool showDelta, bool positiveDelta)
        {
            Value = value;
            DeltaText = deltaText;
            ShowDelta = showDelta;
            PositiveDelta = positiveDelta;
        }
    }

    // Ordered stat definitions: resource key (used for icon lookup) and display label (Pt-BR).
    public static readonly (string Key, string Label)[] StatDefinitions =
    {
        ("Atk", "Attack"),
        ("Def", "Defense"),
        ("Mind", "Mind"),
        ("Heart", "Heart"),
        ("Body", "Body"),
        ("Init", "Initiative"),
        ("Focus", "Focus"),
        ("Str", "Strength"),
        ("Agi", "Agility"),
        ("PowerDices", "Action dices"),
    };

    public static StatDisplayData GetDisplayData(string statKey, Battler battler, Battler opposingBattler, PerkService perkService)
    {
        if (battler == null)
            return new StatDisplayData("0", string.Empty, false, true);

        switch (statKey)
        {
            case "Atk":
                {
                    int baseValue = battler.Attack;
                    int effectiveValue = perkService != null
                        ? perkService.GetEffectiveActionPower(battler, opposingBattler, ActionType.Attack)
                        : baseValue;
                    return BuildDisplayData(effectiveValue, effectiveValue - baseValue);
                }
            case "Def":
                {
                    int baseValue = battler.Defense;
                    int effectiveValue = perkService != null
                        ? perkService.GetEffectiveActionPower(battler, opposingBattler, ActionType.Defense)
                        : baseValue;
                    return BuildDisplayData(effectiveValue, effectiveValue - baseValue);
                }
            case "Mind":
                {
                    int baseValue = battler.GetBaseStatValue(DiceStatType.Mind);
                    int effectiveValue = perkService != null ? perkService.GetEffectiveMind(battler) : battler.Mind;
                    return BuildDisplayData(effectiveValue, effectiveValue - baseValue);
                }
            case "Heart":
                {
                    int baseValue = battler.GetBaseStatValue(DiceStatType.Heart);
                    int effectiveValue = perkService != null ? perkService.GetEffectiveHeart(battler) : battler.Heart;
                    return BuildDisplayData(effectiveValue, effectiveValue - baseValue);
                }
            case "Body":
                {
                    int baseValue = battler.GetBaseStatValue(DiceStatType.Body);
                    int effectiveValue = perkService != null ? perkService.GetEffectiveBody(battler) : battler.Body;
                    return BuildDisplayData(effectiveValue, effectiveValue - baseValue);
                }
            case "Init":
                return BuildDisplayData(battler.Initiative, 0);
            case "Focus":
                return BuildDisplayData(battler.Focus, 0);
            case "Str":
                return BuildDisplayData(battler.Strength, 0);
            case "Agi":
                return BuildDisplayData(battler.Agility, 0);
            case "PowerDices":
                return new StatDisplayData($"{battler.CurrentDices}/{battler.MaxDices}", string.Empty, false, true);
            default:
                return new StatDisplayData("0", string.Empty, false, true);
        }
    }

    private static StatDisplayData BuildDisplayData(int effectiveValue, int delta)
    {
        if (delta == 0)
            return new StatDisplayData(effectiveValue.ToString(), string.Empty, false, true);

        string deltaText = delta > 0 ? $"+{delta}" : delta.ToString();
        return new StatDisplayData(effectiveValue.ToString(), deltaText, true, delta > 0);
    }

    // Ready-to-render row: icon key, Pt-BR label, and computed display data for a single stat.
    public readonly struct StatRowEntry
    {
        public readonly string Key;
        public readonly string Label;
        public readonly string Value;
        public readonly string DeltaText;
        public readonly bool ShowDelta;
        public readonly bool PositiveDelta;

        public StatRowEntry(string key, string label, StatDisplayData data)
        {
            Key = key;
            Label = label;
            Value = data.Value;
            DeltaText = data.DeltaText;
            ShowDelta = data.ShowDelta;
            PositiveDelta = data.PositiveDelta;
        }
    }

    // Builds the full ordered list of stat rows for a battler, ready for the View to render.
    public static List<StatRowEntry> BuildStatRows(Battler battler, Battler opposingBattler, PerkService perkService)
    {
        var rows = new List<StatRowEntry>(StatDefinitions.Length);

        foreach (var (Key, Label) in StatDefinitions)
        {
            StatDisplayData data = GetDisplayData(Key, battler, opposingBattler, perkService);
            rows.Add(new StatRowEntry(Key, Label, data));
        }

        return rows;
    }
}