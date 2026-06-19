using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Perk", menuName = "Combat/Perk")]
public class PerkSO : ScriptableObject
{
    /// <summary>
    /// ID único do perk (ex: "six_feet_under") para ser referenciado em Tricks.
    /// </summary>
    public string Id;
    public int DefaultDurationTurns = -1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public string Tags;
    public List<PerkRule> Rules = new();
}
