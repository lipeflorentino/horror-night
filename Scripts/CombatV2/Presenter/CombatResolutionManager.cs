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
        
        Logger.Log($"[Resolve] Outcome: {result.Outcome} | Damage: {result.Damage} | Feedback: {result.FeedbackText}");
        view.ShowAttackEffect(state.PlayerIsAttacker);

        if (result.AppliesDamage)
        {
            Logger.Log($"[Resolve] Applying {result.Damage} damage to {result.FinalTarget.Name}");
            result.FinalTarget.ReceiveDamage(result.Damage);
        }

        view.ShowResolveFeedback(result, state.PlayerIsAttacker == false);

        Logger.Log($"[HP] Player: {player.HP} | Enemy: {enemy.HP}");

        view.UpdateView(player, enemy);
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
        int basePower = perkService != null ? perkService.GetEffectiveActionPower(battler, opponent, actionType) : battler?.GetBattlerActionPower(actionType == ActionType.Attack) ?? 0;
        string id = actionType == ActionType.Attack ? "attack" : "defense";
        return new ActionDefinition(id, actionType, basePower);
    }
}
