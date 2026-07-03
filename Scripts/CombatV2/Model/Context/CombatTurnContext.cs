using System.Collections.Generic;

public class CombatTurnContext
{
    public bool PlayerIsAttacker { get; set; } = true;
    public TurnActionContext CurrentTurn { get; set; }
    public bool CombatEnded { get; set; }
    public int LastGrantedXp { get; set; }
    public Dictionary<ItemSO, int> LastGrantedItens { get; set; }

    public List<DiceResult> PendingPlayerPowerRolls { get; set; } = new();
    public List<DiceResult> PendingPlayerAccuracyRolls { get; set; } = new();
    public List<DiceResult> PendingEnemyPowerRolls { get; set; } = new();
    public List<DiceResult> PendingEnemyAccuracyRolls { get; set; } = new();
    public List<DiceStatType> PendingEnemyPowerDiceTypes { get; set; } = new();
    public List<DiceStatType> PendingEnemyAccuracyDiceTypes { get; set; } = new();
}
