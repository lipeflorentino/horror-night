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

    public int PlayerLife { get; private set; }
    public int PlayerPhysical { get; private set; }
    public int PlayerMental { get; private set; }

    public int EnemyLife { get; private set; }
    public int EnemyPhysical { get; private set; }
    public int EnemyMental { get; private set; }

    public int BasePlayerLife { get; private set; }
    public int BasePlayerPhysical { get; private set; }
    public int BasePlayerMental { get; private set; }

    public int BaseEnemyLife { get; private set; }
    public int BaseEnemyPhysical { get; private set; }
    public int BaseEnemyMental { get; private set; }

    private PlayerActionType? pendingPlayerAction;

    public void Initialize(RunStateSnapshot snapshot, EnemyInstance enemy)
    {
        PlayerLife = Mathf.RoundToInt(snapshot.playerStatus.life);
        PlayerPhysical = Mathf.Max(1, Mathf.RoundToInt(snapshot.playerStatus.strength));
        PlayerMental = Mathf.Max(1, Mathf.RoundToInt(snapshot.playerStatus.sanity));

        EnemyLife = Mathf.Max(1, enemy.life);
        EnemyPhysical = Mathf.Max(1, enemy.physical);
        EnemyMental = Mathf.Max(1, enemy.mental);

        BasePlayerLife = PlayerLife;
        BasePlayerPhysical = PlayerPhysical;
        BasePlayerMental = PlayerMental;

        BaseEnemyLife = EnemyLife;
        BaseEnemyPhysical = EnemyPhysical;
        BaseEnemyMental = EnemyMental;

        Outcome = CombatOutcome.Ongoing;
    }

    public IEnumerator RunTurnCombat(CombatSceneBindings bindings)
    {
        bool playerTurn = (PlayerLife + PlayerPhysical + PlayerMental) >= (EnemyLife + EnemyPhysical + EnemyMental);
        bindings.SetCombatLog("O combate começou.");

        while (PlayerLife > 0 && EnemyLife > 0)
        {
            UpdateCombatHud(bindings);

            if (playerTurn)
                yield return ExecutePlayerTurn(bindings);
            else
                yield return ExecuteEnemyTurn(bindings);

            if (EnemyLife <= 0 || PlayerLife <= 0)
                break;

            playerTurn = !playerTurn;
            yield return new WaitForSeconds(0.5f);
        }

        UpdateCombatHud(bindings);

        if (EnemyLife <= 0)
        {
            bindings.SetTurnText("Vitória");
            bindings.SetCombatLog("Inimigo derrotado. Placeholder de recompensa gerado.");
            Outcome = CombatOutcome.Victory;
            yield break;
        }

        bindings.SetTurnText("Derrota");
        bindings.SetCombatLog("Você foi derrotado.");
        Outcome = CombatOutcome.Defeat;
    }

    private IEnumerator ExecutePlayerTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno do Jogador");
        bindings.SetCombatLog("Aguardando ação do jogador...");
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CachePlayerAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CachePlayerAction;
        PlayerActionType action = pendingPlayerAction.Value;

        int playerRoll = RollDice(bindings);
        int enemyRoll = RollDice(bindings);

        ResolveActions(action, ChooseEnemyAction(), playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator ExecuteEnemyTurn(CombatSceneBindings bindings)
    {
        bindings.SetTurnText("Turno do Inimigo");
        bindings.SetCombatLog("Inimigo está escolhendo uma ação...");

        EnemyActionType enemyAction = ChooseEnemyAction();
        PlayerActionType passivePlayerAction = ChooseAutoDefenseAction();

        int enemyRoll = RollDice(bindings);
        int playerRoll = RollDice(bindings);

        ResolveActions(passivePlayerAction, enemyAction, playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private void CachePlayerAction(PlayerActionType action)
    {
        pendingPlayerAction = action;
    }

    private int RollDice(CombatSceneBindings bindings)
    {
        int roll = Random.Range(1, 21);
        bindings.SetDiceValue(roll);
        return roll;
    }

    private void ResolveActions(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        string resultLog;

        if (IsEnemyAttack(enemyAction) && (playerAction == PlayerActionType.Defend || playerAction == PlayerActionType.Parry))
        {
            resultLog = ResolveDefensiveResponse(playerAction, enemyAction, playerRoll, enemyRoll);
            bindings.SetCombatLog(resultLog);
            return;
        }

        if (IsPlayerAttack(playerAction))
        {
            int damage = Mathf.Max(1, playerRoll);
            if (enemyAction == EnemyActionType.Defend)
            {
                damage = Mathf.Max(0, damage - enemyRoll);
                resultLog = $"Você atacou ({playerAction}), mas o inimigo defendeu parte do dano. Dano final: {damage}.";
            }
            else
            {
                resultLog = $"Você atacou ({playerAction}) e causou {damage} de dano.";
            }

            ApplyDamageToEnemy(playerAction, damage);
        }
        else
        {
            resultLog = ResolvePlayerSpecialAction(playerAction, playerRoll);
        }

        if (EnemyLife > 0 && PlayerLife > 0 && IsEnemyAttack(enemyAction))
        {
            ApplyDamageToPlayer(enemyAction, enemyRoll);
            resultLog += $" Inimigo atacou ({enemyAction}) e causou {enemyRoll} de dano.";
        }

        bindings.SetCombatLog(resultLog);
    }

    private string ResolveDefensiveResponse(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll)
    {
        if (playerAction == PlayerActionType.Defend)
        {
            int finalDamage = Mathf.Max(0, enemyRoll - playerRoll);
            ApplyDamageToPlayer(enemyAction, finalDamage);
            return $"Você defendeu. Defesa: {playerRoll}. Dano recebido: {finalDamage}.";
        }

        bool parrySuccess = playerRoll >= enemyRoll;
        if (parrySuccess)
        {
            ApplyDamageToEnemy(PlayerActionType.AttackLife, playerRoll);
            return $"Parry perfeito! Você reverteu {playerRoll} de dano ao inimigo.";
        }

        ApplyDamageToPlayer(enemyAction, enemyRoll);
        return $"Parry falhou. Você recebeu {enemyRoll} de dano.";
    }

    private string ResolvePlayerSpecialAction(PlayerActionType action, int roll)
    {
        switch (action)
        {
            case PlayerActionType.Flee:
                if (roll >= 18)
                {
                    EnemyLife = 0;
                    return "Fuga bem sucedida! Combate encerrado.";
                }

                return "Tentativa de fuga falhou.";
            case PlayerActionType.InstantKill:
                if (roll >= 20)
                {
                    EnemyLife = 0;
                    return "Instant Kill ativado! Inimigo eliminado.";
                }

                return "Instant Kill falhou.";
            case PlayerActionType.Learn:
                if (roll >= 12)
                    return "Learn bem sucedido: informações do inimigo coletadas (placeholder).";

                return "Learn falhou: nenhuma informação nova.";
            case PlayerActionType.Item:
                return "Uso de item ainda é placeholder.";
            default:
                return $"Ação {action} é placeholder.";
        }
    }

    private void ApplyDamageToEnemy(PlayerActionType attackType, int amount)
    {
        if (amount == 0)
            return;

        switch (attackType)
        {
            case PlayerActionType.AttackLife:
                EnemyLife = Mathf.Clamp(EnemyLife - amount, 0, BaseEnemyLife);
                break;
            case PlayerActionType.AttackPhysical:
                EnemyPhysical = Mathf.Clamp(EnemyPhysical - amount, 0, BaseEnemyPhysical);
                break;
            case PlayerActionType.AttackMental:
                EnemyMental = Mathf.Clamp(EnemyMental - amount, 0, BaseEnemyMental);
                break;
            default:
                EnemyLife = Mathf.Clamp(EnemyLife - amount, 0, BaseEnemyLife);
                break;
        }
    }

    private void ApplyDamageToPlayer(EnemyActionType attackType, int amount)
    {
        if (amount <= 0)
            return;

        switch (attackType)
        {
            case EnemyActionType.AttackLife:
                PlayerLife = Mathf.Max(0, PlayerLife - amount);
                break;
            case EnemyActionType.AttackPhysical:
                PlayerPhysical = Mathf.Max(0, PlayerPhysical - amount);
                break;
            case EnemyActionType.AttackMental:
                PlayerMental = Mathf.Max(0, PlayerMental - amount);
                break;
        }
    }

    private EnemyActionType ChooseEnemyAction()
    {
        int roll = Random.Range(0, 100);
        if (roll < 30)
            return EnemyActionType.AttackLife;
        if (roll < 55)
            return EnemyActionType.AttackPhysical;
        if (roll < 80)
            return EnemyActionType.AttackMental;
        return EnemyActionType.Defend;
    }

    private PlayerActionType ChooseAutoDefenseAction()
    {
        return Random.value < 0.75f ? PlayerActionType.Defend : PlayerActionType.Parry;
    }

    private bool IsPlayerAttack(PlayerActionType action)
    {
        return action == PlayerActionType.AttackLife || action == PlayerActionType.AttackPhysical || action == PlayerActionType.AttackMental;
    }

    private bool IsEnemyAttack(EnemyActionType action)
    {
        return action == EnemyActionType.AttackLife || action == EnemyActionType.AttackPhysical || action == EnemyActionType.AttackMental;
    }

    private void UpdateCombatHud(CombatSceneBindings bindings)
    {
        bindings.UpdateHud(
            PlayerLife,
            BasePlayerLife,
            PlayerPhysical,
            BasePlayerPhysical,
            PlayerMental,
            BasePlayerMental,
            EnemyLife,
            BaseEnemyLife,
            EnemyPhysical,
            BaseEnemyPhysical,
            EnemyMental,
            BaseEnemyMental);
    }
}
