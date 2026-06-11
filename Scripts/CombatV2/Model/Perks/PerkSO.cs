using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Perk", menuName = "Combat/Perk")]
public class PerkSO : ScriptableObject
{
    /// <summary>
    /// ID único do perk (ex: "six_feet_under").
    /// Metadados como DisplayName e Description agora vivem em TrickSO.
    /// </summary>
    public string Id;
    
    /// <summary>
    /// Se este perk é parte de uma identidade (sempre ativo para aquela identidade).
    /// </summary>
    public bool IsPermanentIdentity;
    
    public int DefaultDurationTurns = -1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public string Tags;
    public List<PerkRule> Rules = new();
}
