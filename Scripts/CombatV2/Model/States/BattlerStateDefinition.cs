using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattlerState", menuName = "Combat/Battler State")]
public class BattlerStateDefinition : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public Sprite Icon;
    public int DefaultDurationTurns = 1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public List<ThresholdModifier> ThresholdModifiers = new();
    public List<StatModifier> StatModifiers = new();
}
