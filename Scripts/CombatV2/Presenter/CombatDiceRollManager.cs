using System.Collections.Generic;

public class CombatDiceRollManager
{
    public static void RollActions(
        Battler player, 
        Battler enemy, 
        ActionType action, 
        IReadOnlyList<DiceStatType> powerDiceTypes, 
        IReadOnlyList<DiceStatType> accuracyDiceTypes, 
        DiceService diceService, 
        PerkService perkService, 
        BattlerStateService battlerStateService,
        CombatTurnContext combatContext)
    {
        ActionDefinition playerAction = CombatResolutionManager.BuildDefinitionFromBattler(player, enemy, action, battlerStateService);
        ActionType enemyActionType = combatContext.PlayerIsAttacker ? combatContext.CurrentTurn.DefenseAction.Definition.Type : combatContext.CurrentTurn.AttackAction.Definition.Type;

        combatContext.PendingPlayerAccuracyRolls = diceService.RollMany(player, enemy, accuracyDiceTypes, action, DiceRollType.Accuracy, player.Level, enemy.Level);
        combatContext.PendingEnemyAccuracyRolls = diceService.RollMany(enemy, player, combatContext.PendingEnemyAccuracyDiceTypes, enemyActionType, DiceRollType.Accuracy, enemy.Level, player.Level);

        DiceResult playerAccuracyDice = diceService.GetBestResult(combatContext.PendingPlayerAccuracyRolls);
        DiceResult enemyAccuracyDice = diceService.GetBestResult(combatContext.PendingEnemyAccuracyRolls);

        int playerExtraPowerDice = perkService.GetExtraPowerDiceAfterAccuracy(player, enemy, playerAccuracyDice, action, out DiceStatType playerExtraStatType);
        int enemyExtraPowerDice = perkService.GetExtraPowerDiceAfterAccuracy(enemy, player, enemyAccuracyDice, enemyActionType, out DiceStatType enemyExtraStatType);

        combatContext.PendingPlayerPowerRolls = diceService.RollMany(player, enemy, powerDiceTypes, action, DiceRollType.Power, player.Level, enemy.Level);
        combatContext.PendingEnemyPowerRolls = diceService.RollMany(enemy, player, combatContext.PendingEnemyPowerDiceTypes, enemyActionType, DiceRollType.Power, enemy.Level, player.Level);

        if (playerExtraPowerDice > 0)
        {
            combatContext.PendingPlayerPowerRolls.AddRange(RollExtraPowerDiceWithoutPool(player, playerExtraPowerDice, playerExtraStatType, player.Level, enemy.Level, diceService));
            Logger.Log($"[Adrenaline Surge] Player ganhou {playerExtraPowerDice} dado(s) extra(s) de Poder ({playerExtraStatType}) pelo Accuracy tier {playerAccuracyDice?.Tier}.");
        }

        if (enemyExtraPowerDice > 0)
        {
            combatContext.PendingEnemyPowerRolls.AddRange(RollExtraPowerDiceWithoutPool(enemy, enemyExtraPowerDice, enemyExtraStatType, enemy.Level, player.Level, diceService));
            Logger.Log($"[Adrenaline Surge] Enemy ganhou {enemyExtraPowerDice} dado(s) extra(s) de Poder ({enemyExtraStatType}) pelo Accuracy tier {enemyAccuracyDice?.Tier}.");
        }

        DiceResult playerPowerDice = diceService.GetBestResult(combatContext.PendingPlayerPowerRolls);
        DiceResult enemyPowerDice = diceService.GetBestResult(combatContext.PendingEnemyPowerRolls);

        ActionInstance playerActionInstance = new(playerAction, playerPowerDice, playerAccuracyDice);
        ActionInstance enemyActionInstance = combatContext.PlayerIsAttacker ? combatContext.CurrentTurn.DefenseAction : combatContext.CurrentTurn.AttackAction;
        
        enemyActionInstance.Definition = CombatResolutionManager.BuildDefinitionFromBattler(enemy, player, enemyActionInstance.Definition.Type, battlerStateService);
        enemyActionInstance = new ActionInstance(enemyActionInstance.Definition, enemyPowerDice, enemyAccuracyDice);

        if (combatContext.PlayerIsAttacker)
        {
            combatContext.CurrentTurn.AttackAction = playerActionInstance;
            combatContext.CurrentTurn.DefenseAction = enemyActionInstance;
        }
        else
        {
            combatContext.CurrentTurn.AttackAction = enemyActionInstance;
            combatContext.CurrentTurn.DefenseAction = playerActionInstance;
        }

        Logger.Log($"[Flow] Player rolled POWER best:{playerPowerDice?.Value} | ACCURACY best:{playerAccuracyDice?.Value} using {combatContext.PendingPlayerPowerRolls.Count + combatContext.PendingPlayerAccuracyRolls.Count} dice.");
        Logger.Log($"[Flow] Enemy rolled POWER best:{enemyPowerDice?.Value} | ACCURACY best:{enemyAccuracyDice?.Value} using {combatContext.PendingEnemyPowerRolls.Count + combatContext.PendingEnemyAccuracyRolls.Count} dice.");
    }

    public static List<DiceResult> RollExtraPowerDiceWithoutPool(
        Battler actor, 
        int count, 
        DiceStatType statType, 
        int actorLevel, 
        int opponentLevel, 
        DiceService diceService)
    {
        List<DiceResult> extras = new();
        int maxValue = diceService.GetDiceMaxValueForType(actor, statType);
        if (maxValue <= 0 || count <= 0)
            return extras;

        for (int i = 0; i < count; i++)
        {
            DiceResult extra = diceService.Roll(maxValue, actorLevel, opponentLevel, statType, DiceRollType.Power);
            extras.Add(extra);
        }

        return extras;
    }

    public static CombatRollContext BuildPlayerRollContext(
        Battler player, 
        Battler enemy, 
        int maxValue, 
        DiceStatType statType, 
        DiceRollType rollType, 
        BattlerStateService battlerStateService, 
        CombatTurnContext combatContext)
    {
        ActionType actionType = combatContext.PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        int focus = battlerStateService.GetEffectiveFocus(player, enemy, actionType);
        int strength = battlerStateService.GetEffectiveStrength(player, enemy, actionType);
        return new CombatRollContext(player, enemy, actionType, rollType, statType, player.Level, enemy.Level, focus, strength, maxValue);
    }

    public static (int lowMax, int mediumMax, int highMin, int maxValue) GetPlayerTierBoundaries(
        Battler player, 
        Battler enemy, 
        int maxValue, 
        DiceStatType statType, 
        DiceRollType rollType, 
        DiceService diceService, 
        BattlerStateService battlerStateService, 
        CombatTurnContext combatContext)
    {
        CombatRollContext context = BuildPlayerRollContext(player, enemy, maxValue, statType, rollType, battlerStateService, combatContext);
        return diceService.GetTierBoundaries(context);
    }
}
