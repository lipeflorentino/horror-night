using System.Collections;
using UnityEngine;

public enum CombatOutcome
{
    Ongoing,
    Victory,
    Defeat
}

public class TurnManager
{
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.Ongoing;

    public int PlayerLife => turnActions.PlayerLife;
    public int PlayerPhysical => turnActions.PlayerPhysical;
    public int PlayerMental => turnActions.PlayerMental;

    public int EnemyLife => turnActions.EnemyLife;
    public int EnemyPhysical => turnActions.EnemyPhysical;
    public int EnemyMental => turnActions.EnemyMental;

    public TurnManagerStats PlayerCombatStats => turnActions.PlayerStats;

    private PlayerActionType? pendingPlayerAction;
    private readonly PlayerTurnActions playerTurnActions = new();
    private readonly EnemyTurnActions enemyTurnActions = new();
    private TurnActions turnActions;

    public void Initialize(RunStateSnapshot snapshot, EnemyInstance enemy)
    {
        turnActions = new TurnActions(playerTurnActions, enemyTurnActions);
        turnActions.Initialize(snapshot, enemy);
        Outcome = CombatOutcome.Ongoing;
    }

    public IEnumerator RunTurnCombat(CombatSceneBindings bindings)
    {
        bool playerAttacking = (PlayerLife + PlayerPhysical + PlayerMental) >= (EnemyLife + EnemyPhysical + EnemyMental);
        bindings.SetCombatLog("O combate começou.", CombatLogCategory.Action);

        while (PlayerLife > 0 && EnemyLife > 0)
        {
            bindings.UpdateAttackButtonAvailability(turnActions.CanAttackEnemyLife(), turnActions.CanAttackEnemyPhysical(), turnActions.CanAttackEnemyMental());

            if (playerAttacking)
                yield return ExecutePlayerAttackTurn(bindings);
            else
                yield return ExecuteEnemyAttackTurn(bindings);

            if (EnemyLife <= 0 || PlayerLife <= 0)
                break;

            playerAttacking = !playerAttacking;
            yield return new WaitForSeconds(0.35f);
        }

        if (EnemyLife <= 0)
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
        bindings.SetCombatLog("Escolha seu ataque.", CombatLogCategory.Action);
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CacheAttackAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CacheAttackAction;

        PlayerActionType playerAttack = pendingPlayerAction.Value;
        EnemyActionType enemyDefense = enemyTurnActions.ChooseEnemyDefenseAction();

        bindings.NotifyPlayerAction($"{playerTurnActions.Format(playerAttack)}");
        bindings.NotifyEnemyAction($"{enemyTurnActions.Format(enemyDefense)}");

        int playerRoll = 0;
        int enemyRoll = 0;

        yield return turnActions.RollForAction(bindings, playerTurnActions.GetRollType(playerAttack), true, v => playerRoll = v);
        yield return turnActions.RollForAction(bindings, enemyTurnActions.GetRollType(enemyDefense), false, v => enemyRoll = v);

        yield return turnActions.ResolveActions(true, playerAttack, enemyDefense, playerRoll, enemyRoll, bindings);
    }

    private IEnumerator ExecuteEnemyAttackTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno inimigo: Defesa");

        EnemyActionType enemyAttack = enemyTurnActions.ChooseEnemyAction(
            PlayerLife,
            PlayerPhysical,
            PlayerMental,
            EnemyLife,
            EnemyPhysical,
            EnemyMental);

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

    private void CacheAttackAction(PlayerActionType action)
    {
        if (playerTurnActions.IsAttack(action))
            pendingPlayerAction = action;
    }

    private void CacheDefenseAction(PlayerActionType action)
    {
        if (action == PlayerActionType.Defend || action == PlayerActionType.Parry)
            pendingPlayerAction = action;
    }
}
