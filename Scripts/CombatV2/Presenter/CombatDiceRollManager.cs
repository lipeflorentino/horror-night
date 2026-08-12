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
        CombatTurnContext combatContext)
    {
        ActionDefinition playerAction = CombatResolutionManager.BuildDefinitionFromBattler(player, enemy, action, perkService);
        ActionType enemyActionType = combatContext.PlayerIsAttacker ? combatContext.CurrentTurn.DefenseAction.Definition.Type : combatContext.CurrentTurn.AttackAction.Definition.Type;

        List<DiceResult> playerAccuracyRolls = diceService.RollMany(player, enemy, accuracyDiceTypes, action, DiceRollType.Accuracy, player.Level, enemy.Level);
        List<DiceResult> enemyAccuracyRolls = diceService.RollMany(enemy, player, combatContext.PendingEnemyAccuracyDiceTypes, enemyActionType, DiceRollType.Accuracy, enemy.Level, player.Level);

        combatContext.PendingPlayerAccuracyRolls = playerAccuracyRolls;
        combatContext.PendingEnemyAccuracyRolls = enemyAccuracyRolls;
        DiceResult playerAccuracyDice = diceService.GetBestResult(playerAccuracyRolls);
        DiceResult enemyAccuracyDice = diceService.GetBestResult(enemyAccuracyRolls);

        int playerExtraPowerDice = perkService.GetExtraPowerDiceAfterAccuracy(player, enemy, playerAccuracyDice, action, out DiceStatType playerExtraStatType);
        int enemyExtraPowerDice = perkService.GetExtraPowerDiceAfterAccuracy(enemy, player, enemyAccuracyDice, enemyActionType, out DiceStatType enemyExtraStatType);

        List<DiceResult> playerPowerRolls = diceService.RollMany(player, enemy, powerDiceTypes, action, DiceRollType.Power, player.Level, enemy.Level);
        List<DiceResult> enemyPowerRolls = diceService.RollMany(enemy, player, combatContext.PendingEnemyPowerDiceTypes, enemyActionType, DiceRollType.Power, enemy.Level, player.Level);

        combatContext.PendingPlayerPowerRolls = playerPowerRolls;
        combatContext.PendingEnemyPowerRolls = enemyPowerRolls;

        if (playerExtraPowerDice > 0)
        {
            List<DiceResult> playerExtraPowerRolls = RollExtraPowerDiceWithoutPool(player, playerExtraPowerDice, playerExtraStatType, player.Level, enemy.Level, diceService);
            diceService.ApplyExtraPowerDiceToAggregates(combatContext.PendingPlayerPowerRolls, playerExtraPowerRolls, player, enemy, action, player.Level, enemy.Level);
        }

        if (enemyExtraPowerDice > 0)
        {
            List<DiceResult> enemyExtraPowerRolls = RollExtraPowerDiceWithoutPool(enemy, enemyExtraPowerDice, enemyExtraStatType, enemy.Level, player.Level, diceService);
            diceService.ApplyExtraPowerDiceToAggregates(combatContext.PendingEnemyPowerRolls, enemyExtraPowerRolls, enemy, player, enemyActionType, enemy.Level, player.Level);
        }

        DiceResult playerPowerDice = diceService.GetBestResult(combatContext.PendingPlayerPowerRolls);
        DiceResult enemyPowerDice = diceService.GetBestResult(combatContext.PendingEnemyPowerRolls);

        ActionInstance playerActionInstance = new(playerAction, playerPowerDice, playerAccuracyDice, powerDiceTypes?.Count ?? 0, accuracyDiceTypes?.Count ?? 0);
        ActionInstance enemyActionInstance = combatContext.PlayerIsAttacker ? combatContext.CurrentTurn.DefenseAction : combatContext.CurrentTurn.AttackAction;

        enemyActionInstance.Definition = CombatResolutionManager.BuildDefinitionFromBattler(enemy, player, enemyActionInstance.Definition.Type, perkService);
        enemyActionInstance = new ActionInstance(
            enemyActionInstance.Definition,
            enemyPowerDice,
            enemyAccuracyDice,
            combatContext.PendingEnemyPowerDiceTypes?.Count ?? 0,
            combatContext.PendingEnemyAccuracyDiceTypes?.Count ?? 0);

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

        Logger.Log($"[Flow] Player rolled POWER best:{playerPowerDice?.Value} | ACCURACY best:{playerAccuracyDice?.Value} using {combatContext.PendingPlayerPowerRolls.Count} Power dices and {combatContext.PendingPlayerAccuracyRolls.Count} Accuracy dices.");
        Logger.Log($"[Flow] Enemy rolled POWER best:{enemyPowerDice?.Value} | ACCURACY best:{enemyAccuracyDice?.Value} using {combatContext.PendingEnemyPowerRolls.Count} Power dices and {combatContext.PendingEnemyAccuracyRolls.Count} Accuracy dices.");
    }

    public static Dictionary<DiceStatType, int> ApplyStatDiceCost(
        Battler player, Battler enemy,
        IReadOnlyList<DiceStatType> playerPowerDiceTypes,
        IReadOnlyList<DiceStatType> playerAccuracyDiceTypes,
        CombatTurnContext combatContext)
    {
        Dictionary<DiceStatType, int> playerCosts = new();
        SpendDiceListCost(player, playerPowerDiceTypes, playerCosts);
        SpendDiceListCost(player, playerAccuracyDiceTypes, playerCosts);
        SpendDiceListCost(enemy, combatContext.PendingEnemyPowerDiceTypes);
        SpendDiceListCost(enemy, combatContext.PendingEnemyAccuracyDiceTypes);
        return playerCosts;
    }

    private static void SpendDiceListCost(Battler battler, IReadOnlyList<DiceStatType> diceTypes, Dictionary<DiceStatType, int> costAcc = null)
    {
        if (battler == null || diceTypes == null) return;
        for (int i = 0; i < diceTypes.Count; i++)
        {
            battler.SpendStatDice(diceTypes[i], 1);
            if (costAcc != null)
            {
                costAcc.TryGetValue(diceTypes[i], out int cur);
                costAcc[diceTypes[i]] = cur + 1;
            }
        }
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
            DiceResult extra = diceService.Roll(maxValue, actorLevel, opponentLevel, statType, DiceRollType.Power, 1, 0, 0, true);
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
        PerkService perkService,
        CombatTurnContext combatContext,
        int allocatedDiceCount = 1)
    {
        ActionType actionType = combatContext.PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        int focus = perkService.GetEffectiveFocus(player, enemy, actionType);
        int strength = perkService.GetEffectiveStrength(player, enemy, actionType);
        return new CombatRollContext(player, enemy, actionType, rollType, statType, player.Level, enemy.Level, focus, strength, maxValue, allocatedDiceCount);
    }

    public static (int lowMax, int mediumMax, int highMin, int maxValue) GetPlayerTierBoundaries(
        Battler player,
        Battler enemy,
        int maxValue,
        DiceStatType statType,
        DiceRollType rollType,
        DiceService diceService,
        PerkService perkService,
        CombatTurnContext combatContext,
        int allocatedDiceCount = 1)
    {
        CombatRollContext context = BuildPlayerRollContext(player, enemy, maxValue, statType, rollType, perkService, combatContext, allocatedDiceCount);
        return diceService.GetTierBoundaries(context);
    }
}
