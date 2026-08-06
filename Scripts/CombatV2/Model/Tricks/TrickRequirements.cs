using System.Collections.Generic;

[System.Serializable]
public class TrickRequirements
{
    public int MinLevel = 1;
    public int MinMind = 0;
    public int MinHeart = 0;
    public int MinBody = 0;
    public int MinAttack = 0;
    public int MinDefense = 0;
    public int MinInitiative = 0;
    public int MinFocus = 0;
    public int MinStrength = 0;
    public int MinAgility = 0;

    public bool HasAnyRequirement =>
        MinLevel > 1 ||
        MinMind > 0 ||
        MinHeart > 0 ||
        MinBody > 0 ||
        MinAttack > 0 ||
        MinDefense > 0 ||
        MinInitiative > 0 ||
        MinFocus > 0 ||
        MinStrength > 0 ||
        MinAgility > 0;

    public bool IsSatisfiedBy(Battler battler, PerkService perkService = null)
    {
        if (battler == null)
            return false;

        if (battler.Level < MinLevel)
            return false;

        int mind = perkService != null ? perkService.GetEffectiveMind(battler) : battler.Mind;
        int heart = perkService != null ? perkService.GetEffectiveHeart(battler) : battler.Heart;
        int body = perkService != null ? perkService.GetEffectiveBody(battler) : battler.Body;

        if (mind < MinMind || heart < MinHeart || body < MinBody)
            return false;

        int attack = perkService != null ? perkService.GetEffectiveAttack(battler) : battler.Attack;
        int defense = perkService != null ? perkService.GetEffectiveDefense(battler) : battler.Defense;

        if (attack < MinAttack || defense < MinDefense || battler.Initiative < MinInitiative)
            return false;

        int focus = perkService != null ? perkService.GetEffectiveFocus(battler, null, ActionType.Attack) : battler.Focus;
        int strength = perkService != null ? perkService.GetEffectiveStrength(battler, null, ActionType.Attack) : battler.Strength;

        if (focus < MinFocus || strength < MinStrength || battler.Agility < MinAgility)
            return false;

        return true;
    }

    public IEnumerable<(string Key, int Value)> GetActiveRequirements()
    {
        if (MinLevel > 1) yield return ("Level", MinLevel);
        if (MinMind > 0) yield return ("Mind", MinMind);
        if (MinHeart > 0) yield return ("Heart", MinHeart);
        if (MinBody > 0) yield return ("Body", MinBody);
        if (MinAttack > 0) yield return ("Attack", MinAttack);
        if (MinDefense > 0) yield return ("Defense", MinDefense);
        if (MinInitiative > 0) yield return ("Initiative", MinInitiative);
        if (MinFocus > 0) yield return ("Focus", MinFocus);
        if (MinStrength > 0) yield return ("Strength", MinStrength);
        if (MinAgility > 0) yield return ("Agility", MinAgility);
    }

    public string ToDisplayString()
    {
        List<string> parts = new();
        if (MinLevel > 1) parts.Add($"Lvl {MinLevel}");
        if (MinMind > 0) parts.Add($"Mind {MinMind}");
        if (MinHeart > 0) parts.Add($"Heart {MinHeart}");
        if (MinBody > 0) parts.Add($"Body {MinBody}");
        if (MinAttack > 0) parts.Add($"Atk {MinAttack}");
        if (MinDefense > 0) parts.Add($"Def {MinDefense}");
        if (MinInitiative > 0) parts.Add($"Init {MinInitiative}");
        if (MinFocus > 0) parts.Add($"Focus {MinFocus}");
        if (MinStrength > 0) parts.Add($"Str {MinStrength}");
        if (MinAgility > 0) parts.Add($"Agi {MinAgility}");

        return parts.Count > 0 ? string.Join(", ", parts) : "Nenhum";
    }
}