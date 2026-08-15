using UnityEngine;

public static class Colorization
{
    public const string WhiteColorHex = "#FFFFFF";
    public const string BadColorHex = "#f50000";
    public const string GoodColorHex = "#4CAF50";
    public const string PowerColorHex = "#EAA00E";
    public const string AccuracyColorHex = "#FFFB00";
    public const string LowColorHex = "#4A90E2"; 
    public const string MediumColorHex = "#F59E0B";
    public const string HighColorHex = "#E05C5C";
    public const string DisabledColorHex = "#808080";
    public const string DefaultTextColorHex = "#FFFFFF";
    public const string IdentityColorHex = "#0005FF";
    public const string ActiveColorHex = "#FF2100";
    public const string PassiveColorHex = "#F108EB";
    public const string CommonColorHex = "#FFFFFF";
    public const string UncommonColorHex = "#00FF00";
    public const string RareColorHex = "#0000FF";
    public const string EpicColorHex = "#800080";
    public const string LegendaryColorHex = "#FFA500";
    public const string MindColorHex = "#a924ca";
    public const string HeartColorHex = "#cb2228";
    public const string BodyColorHex = "#8be958";
    
    public static string GetRarityColor(TrickRarity rarity)
    {
        return rarity switch
        {
            TrickRarity.Common => CommonColorHex,
            TrickRarity.Uncommon => UncommonColorHex,
            TrickRarity.Rare => RareColorHex,
            TrickRarity.Epic => EpicColorHex,
            TrickRarity.Legendary => LegendaryColorHex,
            _ => CommonColorHex
        };
    }

    public static string GetTrickTypeColor(TrickSlotType slotType)
    {
        return slotType switch
        {
            TrickSlotType.CastedActive => ActiveColorHex,
            TrickSlotType.CastedPassive => PassiveColorHex,
            _ => IdentityColorHex
        };
    }

    public static string GetStatColor(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => MindColorHex,
            DiceStatType.Heart => HeartColorHex,
            DiceStatType.Body => BodyColorHex,
            _ => DefaultTextColorHex
        };
    }

    public static string GetStatColor(string statType)
    {
        return statType switch
        {
            "Mind" => MindColorHex,
            "Heart" => HeartColorHex,
            "Body" => BodyColorHex,
            _ => DefaultTextColorHex
        };
    }

    public static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        else
        {
            Debug.LogWarning($"Invalid hex color string: {hex}. Returning white color.");
            return Color.white; 
        }
    }
}