using System.Collections.Generic;
using UnityEngine;

public class TurnManager
{
    public static bool CanReceivePlayerInput(ActionType type, CombatTurnContext state, out string rejectionReason)
    {
        rejectionReason = null;
        if (state.CombatEnded)
            return false;

        ActionType expectedType = state.PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        if (type != expectedType)
        {
            rejectionReason = $"[Input] Ignored invalid action for current role. Expected {expectedType} and received {type}";
            return false;
        }

        return true;
    }

    public static bool CanReceivePlayerSkipTurn(CombatTurnContext state)
    {
        return !state.CombatEnded;
    }

    public static void DefineStartingTurnByInitiative(Battler player, Battler enemy, InitiativeResolverService initiativeResolver, CombatTurnContext state)
    {
        Battler firstBattler = initiativeResolver.ResolveStartingBattler(player, enemy);
        state.PlayerIsAttacker = firstBattler == player;
        state.CurrentTurn = state.PlayerIsAttacker ? new TurnActionContext(player, enemy) : new TurnActionContext(enemy, player);
    }

    public static void GenerateEnemyAction(Battler enemy, CombatSessionData sessionData, EnemyTurnPlanner planner, ActionDefinition attackDef, ActionDefinition defenseDef, CombatTurnContext state)
    {
        EnemyTurnPlan plan = planner.BuildPlan(enemy, sessionData?.EnemyInstance, attackDef, defenseDef);
        
        if (state.PlayerIsAttacker)
            state.CurrentTurn.DefenseAction = plan.Action;
        else
            state.CurrentTurn.AttackAction = plan.Action;

        state.PendingEnemyPowerDiceTypes = plan.PowerDiceTypes;
        state.PendingEnemyAccuracyDiceTypes = plan.AccuracyDiceTypes;
    }

    public static void EndTurn(
        Battler player, 
        Battler enemy, 
        PerkService perkService, 
        TrickService trickService, 
        ITrickInventory playerTrickInventory, 
        ITrickInventory enemyTrickInventory, 
        CombatTurnContext state)
    {
        if (state.CombatEnded)
            return;

        player.RecoverDices(1);
        enemy.RecoverDices(1);
        player.AddMomentum(1);
        enemy.AddMomentum(1);
        perkService.TickTurnEnd(player);
        perkService.TickTurnEnd(enemy);
        trickService.TickTrickEnd(player, playerTrickInventory);
        trickService.TickTrickEnd(enemy, enemyTrickInventory);
        
        state.PlayerIsAttacker = !state.PlayerIsAttacker;
        state.CurrentTurn = state.PlayerIsAttacker ? new TurnActionContext(player, enemy) : new TurnActionContext(enemy, player);
    }

    public static void UpdateTurnRoleUI(CombatTurnContext state, CombatView view, CombatInputHandler input)
    {
        ActionType allowedAction = state.PlayerIsAttacker ? ActionType.Attack : ActionType.Defense;
        view.UpdateTurnOwner(state.PlayerIsAttacker);
        input.SetAllowedAction(allowedAction);
        view.ActionPanel.SetPlayerRoleButtons(state.PlayerIsAttacker);
    }
}
