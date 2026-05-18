using System;
using System.Collections.Generic;
using System.Globalization;

public readonly struct ItemStatBonus
{
    public readonly string statName;
    public readonly int value;

    public ItemStatBonus(string statName, int value)
    {
        this.statName = statName;
        this.value = value;
    }
}

public static class ItemStatBonusParser
{
    public static IEnumerable<ItemStatBonus> Parse(string statBonus)
    {
        if (string.IsNullOrWhiteSpace(statBonus) || string.Equals(statBonus, "none", StringComparison.OrdinalIgnoreCase))
            yield break;

        string[] entries = statBonus.Split(':', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i].Trim();
            int plusIndex = entry.IndexOf('+');
            int minusIndex = entry.IndexOf('-', 1);
            int splitIndex = plusIndex >= 0 ? plusIndex : minusIndex;

            if (splitIndex <= 0 || splitIndex >= entry.Length - 1)
                continue;

            string statName = entry.Substring(0, splitIndex).Trim();
            string valueRaw = entry.Substring(splitIndex);

            if (!int.TryParse(valueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                continue;

            yield return new ItemStatBonus(statName, value);
        }
    }
}
