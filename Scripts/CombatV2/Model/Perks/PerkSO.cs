using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Perk", menuName = "Combat/Perk")]
public class PerkSO : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public bool IsPermanentIdentity;
    public int DefaultDurationTurns = -1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public string Tags;
    public List<PerkRule> Rules = new();
}
