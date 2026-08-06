public class CombatResolutionManager
{
    public static void Resolve(
        ActionResolverService resolver, 
        CombatTurnContext state, 
        CombatView view, 
        Battler player, 
        Battler enemy)
    {
        ActionResolutionResult result = resolver.Resolve(
            state.CurrentTurn.AttackAction,
            state.CurrentTurn.DefenseAction,
            state.CurrentTurn.Attacker,
            state.CurrentTurn.Defender
        );
        
        view.ShowCombatLog($"Outcome: <color=yellow>{result.Outcome}</color> <br>Damage: <color=red>{result.Damage}</color> <color=yellow>({(result.DamageBonus >= 0 ? "+" : string.Empty)}{result.DamageBonus} bonus)</color>");
        
        if (result.Outcome == ActionOutcome.CriticalHit || result.Outcome == ActionOutcome.Hit) 
        {
            view.ShowAttackEffect(state.PlayerIsAttacker);
        }

        if (result.AppliesDamage)
        {
            view.ShowCombatLog($"Applying <color=red>{result.Damage}</color> damage to <color=green>{result.FinalTarget.Name}</color>");
            result.FinalTarget.ReceiveDamage(result.Damage);
        }

        view.ShowResolveFeedback(result, state.PlayerIsAttacker);

        view.ShowCombatLog($"Player: <color=green>{player.HP}</color> | Enemy: <color=orange>{enemy.HP}</color>");

        view.UpdateView(player, enemy);
        view.RefreshActiveTricks();
    }

    public static bool ResolveAttackAccuracy(CombatTurnContext state)
    {
        ActionInstance attack = state.CurrentTurn.AttackAction;
        return attack != null && attack.AccuracyDice != null && attack.AccuracyDice.Tier != DiceTier.Low;
    }

    public static bool ResolveDefenseAccuracy(CombatTurnContext state)
    {
        ActionInstance defense = state.CurrentTurn.DefenseAction;
        return defense != null && defense.AccuracyDice != null && defense.AccuracyDice.Tier != DiceTier.Low;
    }

    public static ActionDefinition BuildDefinitionFromBattler(Battler battler, Battler opponent, ActionType actionType, PerkService perkService)
    {
        int basePower = actionType == ActionType.Attack
            ? perkService != null 
                ? perkService.GetEffectiveAttack(battler) 
                : battler?.Attack ?? 0
            : perkService != null 
                ? perkService.GetEffectiveDefense(battler) 
                : battler?.Defense ?? 0;

        string id = actionType == ActionType.Attack ? "attack" : "defense";
        return new ActionDefinition(id, actionType, basePower);
    }
}
