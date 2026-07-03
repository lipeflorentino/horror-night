using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattlerState", menuName = "Combat/Battler State")]
public class BattlerStateSO : ScriptableObject
{
    public string Id;
    public string DisplayName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite Icon;
    public int DefaultDurationTurns = 1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public List<string> PerkIds = new();
    [TextArea(1, 2)]
    public string FlavorText;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Id) && PerkIds.Count > 0;
    }
}
