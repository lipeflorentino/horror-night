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
        bool playerAttacking = (PlayerHeart + PlayerBody + PlayerMind) >= (EnemyHeart + EnemyBody + EnemyMind);
        bindings.SetCombatLog("O combate começou.", CombatLogCategory.Action);

        while (PlayerHeart > 0 && EnemyHeart > 0 && Outcome == CombatOutcome.Ongoing)
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

            if (EnemyHeart <= 0 || PlayerHeart <= 0)
                break;

            playerAttacking = !playerAttacking;
            yield return new WaitForSeconds(0.35f);
        }

        if (Outcome == CombatOutcome.Fled)
        {
            bindings.SetTurnText("Fuga");
            bindings.SetCombatLog("Você escapou do combate.", CombatLogCategory.Action);
            yield break;
        }

        if (EnemyHeart <= 0)
        {
            bindings.SetTurnText("Vitória");
            bindings.SetCombatLog("Inimigo derrotado. Placeholder de recompensa gerado.", CombatLogCategory.Victory);
            Outcome = CombatOutcome.Victory;
            yield break;
        }

        bindings.SetTurnText("Derrota");
        bindings.SetCombatLog("Você foi derrotado.", CombatLogCategory.Defeat);
        Outcome = CombatOutcome.Defeat;
    }

    private IEnumerator ExecutePlayerAttackTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Seu turno: Ataque");
        bindings.SetCombatLog("Escolha seu ataque ou ação especial.", CombatLogCategory.Action);
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CachePlayerTurnAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CachePlayerTurnAction;

        PlayerActionType selectedAction = pendingPlayerAction.Value;
        bindings.NotifyPlayerAction($"{playerTurnActions.Format(selectedAction)}");

        if (playerTurnActions.IsAttack(selectedAction))
        {
            EnemyActionType enemyDefense = enemyTurnActions.ChooseEnemyDefenseAction();
            bindings.NotifyEnemyAction($"{enemyTurnActions.Format(enemyDefense)}");

            int playerRoll = 0;
            int enemyRoll = 0;

            yield return turnActions.RollForAction(bindings, playerTurnActions.GetRollType(selectedAction), true, v => playerRoll = v);
            yield return turnActions.RollForAction(bindings, enemyTurnActions.GetRollType(enemyDefense), false, v => enemyRoll = v);

            yield return turnActions.ResolveActions(true, selectedAction, enemyDefense, playerRoll, enemyRoll, bindings);
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

        bindings.SetCombatLog("Inimigo vai atacar. Escolha sua defesa.", CombatLogCategory.Action);
        bindings.NotifyEnemyAction($"{enemyTurnActions.Format(enemyAttack)}");

        bindings.ShowDefenseMenu();
        bindings.OnPlayerActionSelected += CacheDefenseAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CacheDefenseAction;

        PlayerActionType playerDefense = pendingPlayerAction.Value;
        bindings.NotifyPlayerAction($"{playerTurnActions.Format(playerDefense)}");

        int enemyRoll = 0;
        int playerRoll = 0;

        yield return turnActions.RollForAction(bindings, enemyTurnActions.GetRollType(enemyAttack), false, v => enemyRoll = v);
        yield return turnActions.RollForAction(bindings, playerTurnActions.GetRollType(playerDefense), true, v => playerRoll = v);

        yield return turnActions.ResolveActions(false, playerDefense, enemyAttack, playerRoll, enemyRoll, bindings);
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
}
