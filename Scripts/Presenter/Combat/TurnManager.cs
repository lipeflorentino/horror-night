using System.Collections;
using UnityEngine;

public enum CombatOutcome
{
    Ongoing,
    Victory,
    Defeat,
    Fled
}

public class TurnManager
{
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.Ongoing;

    public int PlayerHeart => turnActions.PlayerHeart;
    public int PlayerBody => turnActions.PlayerBody;
    public int PlayerMind => turnActions.PlayerMind;

    public int EnemyHeart => turnActions.EnemyHeart;
    public int EnemyBody => turnActions.EnemyBody;
    public int EnemyMind => turnActions.EnemyMind;

    public int EnemyRevealLevel => turnActions.EnemyRevealLevel;

    public TurnManagerStats PlayerCombatStats => turnActions.PlayerStats;

    private PlayerActionType? pendingPlayerAction;
    private readonly PlayerTurnActions playerTurnActions = new();
    private readonly EnemyTurnActions enemyTurnActions = new();
    private TurnActions turnActions;

    public void Initialize(CombatSceneBindings bindings, RunStateSnapshot snapshot, EnemyInstance enemy)
    {
        turnActions = new TurnActions(playerTurnActions, enemyTurnActions);
        turnActions.Initialize(bindings, snapshot, enemy);
        Outcome = CombatOutcome.Ongoing;
    }

    public IEnumerator RunTurnCombat(CombatSceneBindings bindings)
    {
        bool playerAttacking = turnActions.PlayerStats.initiative >= turnActions.EnemyStats.initiative;
        bindings.SetCombatLog("O combate começou.", CombatLogCategory.Action);

        while (IsPlayerAlive() && IsEnemyAlive() && Outcome == CombatOutcome.Ongoing)
        {
            bindings.UpdateAttackButtonAvailability(turnActions.CanAttackEnemyHeart(), turnActions.CanAttackEnemyBody(), turnActions.CanAttackEnemyMind());
            bindings.UpdateSpecialActionAvailability(turnActions.CanUseInstantKill(), turnActions.CanUseLearn());
            bindings.SetEnemyLearnState(turnActions.EnemyRevealLevel, CombatManager.Instance.CurrentEnemySource, EnemyHeart, EnemyBody, EnemyMind, turnActions.EnemyStats);

            if (playerAttacking)
                yield return ExecutePlayerAttackTurn(bindings);
            else
                yield return ExecuteEnemyAttackTurn(bindings);

            if (Outcome != CombatOutcome.Ongoing)
                break;

            if (!IsPlayerAlive() || !IsEnemyAlive())
                break;

            playerAttacking = !playerAttacking;
            yield return new WaitForSeconds(0.7f);
        }

        if (Outcome == CombatOutcome.Fled)
        {
            bindings.SetTurnText("Fuga");
            bindings.SetCombatLog("Você escapou do combate.", CombatLogCategory.Action);
            yield break;
        }

        if (!IsEnemyAlive())
        {
            bindings.SetTurnText("Vitória");
            bindings.SetCombatLog("Inimigo derrotado! Placeholder de recompensa gerado.", CombatLogCategory.Victory);
            Outcome = CombatOutcome.Victory;
            yield break;
        }

        bindings.SetTurnText("Derrota");
        bindings.SetCombatLog("Você foi derrotado!", CombatLogCategory.Defeat);
        Outcome = CombatOutcome.Defeat;
    }

    private IEnumerator ExecutePlayerAttackTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno do jogador");
        bindings.SetCombatLog("Escolha sua ação.", CombatLogCategory.Action);
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CachePlayerTurnAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CachePlayerTurnAction;

        PlayerActionType selectedAction = pendingPlayerAction.Value;
        bindings.NotifyPlayerAction(playerTurnActions.Format(selectedAction), selectedAction);

        if (playerTurnActions.IsAttack(selectedAction))
        {
            EnemyActionType enemyDefense = enemyTurnActions.ChooseEnemyDefenseAction();

            int playerRoll = 0;
            int enemyRoll = 0;

            yield return turnActions.RollForAction(bindings, playerTurnActions.GetRollType(selectedAction), true, v => playerRoll = v);
            RollType defenseRollType = enemyTurnActions.GetDefenseRollTypeByIncomingAttack(selectedAction);
            if (turnActions.CanRollForAction(defenseRollType, false))
                yield return turnActions.RollForAction(bindings, defenseRollType, false, v => enemyRoll = v);
            else
                enemyRoll = -1;

        yield return turnActions.ResolveActions(true, selectedAction, enemyDefense, playerRoll, enemyRoll, bindings);
            
            // NOTIFICAÇÂO DE DEFESA DO INIMIGO
            bindings.NotifyEnemyAction(enemyTurnActions.Format(enemyDefense), enemyDefense);
            bindings.ResetDiceValue();
            yield break;
        }

        TurnActions.SpecialActionResolution specialResult = turnActions.ResolvePlayerSpecialAction(selectedAction, bindings);
        bindings.NotifyPlayerAction(specialResult.feedback);
        bindings.SetCombatLog(specialResult.log, CombatLogCategory.Action);

        if (selectedAction == PlayerActionType.Learn)
            bindings.SetEnemyLearnState(turnActions.EnemyRevealLevel, CombatManager.Instance.CurrentEnemySource, EnemyHeart, EnemyBody, EnemyMind, turnActions.EnemyStats);

        if (specialResult.endCombat)
        {
            if (specialResult.postDelay > 0f)
                yield return new WaitForSeconds(specialResult.postDelay);

            Outcome = specialResult.forcedOutcome;
        }

        bindings.ResetDiceValue();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteEnemyAttackTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno inimigo: Defesa");

        EnemyActionType enemyAttack = enemyTurnActions.ChooseEnemyAction(
            PlayerHeart,
            PlayerBody,
            PlayerMind,
            EnemyHeart,
            EnemyBody,
            EnemyMind);

        bindings.SetCombatLog("Inimigo vai atacar, prepare-se!");

        yield return new WaitForSeconds(1f);

        bindings.NotifyEnemyAction(enemyTurnActions.Format(enemyAttack), enemyAttack);

        bindings.BeginDefenseSelection();
        bindings.ShowDefenseMenu();
        bindings.SetCombatLog("Escolha sua defesa.", CombatLogCategory.Action);
        bindings.OnPlayerActionSelected += CacheDefenseAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CacheDefenseAction;
        bindings.EndDefenseSelection();

        PlayerActionType playerDefense = pendingPlayerAction.Value;

        int enemyRoll = 0;
        int playerRoll = 0;

        RollType enemyAttackRollType = enemyTurnActions.GetRollType(enemyAttack);
        yield return turnActions.RollForAction(bindings, enemyAttackRollType, false, v => enemyRoll = v);

        RollType defenseRollType = playerTurnActions.GetDefenseRollTypeByIncomingAttack(enemyAttack);
        if (turnActions.CanRollForAction(defenseRollType, true))
            yield return turnActions.RollForAction(bindings, defenseRollType, true, v => playerRoll = v);
        else
            playerRoll = -1;

        yield return turnActions.ResolveActions(false, playerDefense, enemyAttack, playerRoll, enemyRoll, bindings);

        // NOTIFICAÇÂO DE DEFESA DO PLAYER
        bindings.NotifyPlayerAction(playerTurnActions.Format(playerDefense), playerDefense);
        bindings.ResetDiceValue();
        yield return new WaitForSeconds(0.5f);
    }

    private void CachePlayerTurnAction(PlayerActionType action)
    {
        if (playerTurnActions.IsAttack(action) || action == PlayerActionType.Flee || action == PlayerActionType.InstantKill || action == PlayerActionType.Learn)
            pendingPlayerAction = action;
    }

    private void CacheDefenseAction(PlayerActionType action)
    {
        if (action == PlayerActionType.Defend || action == PlayerActionType.Parry)
            pendingPlayerAction = action;
    }

    private bool IsPlayerAlive()
    {
        return PlayerHeart > 0 || PlayerMind > 0 || PlayerBody > 0;
    }

    private bool IsEnemyAlive()
    {
        return EnemyHeart > 0 || EnemyMind > 0 || EnemyBody > 0;
    }
}
