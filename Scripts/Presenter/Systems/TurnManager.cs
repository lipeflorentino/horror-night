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

    private TurnManagerStats playerStats;
    private TurnManagerStats enemyStats;

    private PlayerActionType? pendingPlayerAction;
    private readonly PlayerTurnActions playerTurnActions = new();
    private readonly EnemyTurnActions enemyTurnActions = new();

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

        playerStats = snapshot.playerStatus.combatStats;
        if (playerStats.attack <= 0)
            playerStats = TurnManagerStats.BuildDefault(PlayerLife, PlayerPhysical, PlayerMental);
        playerStats.Normalize();

        enemyStats = enemy.combatStats;
        if (enemyStats.attack <= 0)
            enemyStats = TurnManagerStats.BuildDefault(EnemyLife, EnemyPhysical, EnemyMental);
        enemyStats.Normalize();

        Outcome = CombatOutcome.Ongoing;
    }

    public IEnumerator RunTurnCombat(CombatSceneBindings bindings)
    {
        bool playerTurn = (PlayerLife + PlayerPhysical + PlayerMental) >= (EnemyLife + EnemyPhysical + EnemyMental);
        bindings.SetCombatLog("O combate começou.", CombatLogCategory.Action);

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
            bindings.SetCombatLog("Inimigo derrotado. Placeholder de recompensa gerado.", CombatLogCategory.Victory);
            Outcome = CombatOutcome.Victory;
            yield break;
        }

        bindings.SetTurnText("Derrota");
        bindings.SetCombatLog("Você foi derrotado.", CombatLogCategory.Defeat);
        Outcome = CombatOutcome.Defeat;
    }

    private IEnumerator ExecutePlayerTurn(CombatSceneBindings bindings)
    {
        pendingPlayerAction = null;
        bindings.SetTurnText("Turno do Jogador");
        bindings.SetCombatLog("Aguardando ação do jogador...", CombatLogCategory.Action);
        bindings.SetActionsVisible(true);
        bindings.OnPlayerActionSelected += CachePlayerAction;

        while (!pendingPlayerAction.HasValue)
            yield return null;

        bindings.OnPlayerActionSelected -= CachePlayerAction;

        PlayerActionType action = pendingPlayerAction.Value;
        EnemyActionType enemyAction = enemyTurnActions.ChooseEnemyAction(true);

        bindings.NotifyPlayerAction($"Ação: {playerTurnActions.Format(action)}");

        int playerRoll = 0;
        int enemyRoll = 0;

        yield return RollForAction(bindings, playerTurnActions.GetRollType(action), true, v => playerRoll = v);
        yield return RollForAction(bindings, enemyTurnActions.GetRollType(enemyAction), false, v => enemyRoll = v);

        ResolveActions(action, enemyAction, playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator ExecuteEnemyTurn(CombatSceneBindings bindings)
    {
        bindings.SetTurnText("Turno do Inimigo");
        bindings.SetCombatLog("Inimigo está escolhendo uma ação...", CombatLogCategory.Action);

        EnemyActionType enemyAction = enemyTurnActions.ChooseEnemyAction(false);
        PlayerActionType passivePlayerAction = enemyTurnActions.ChooseAutoDefenseAction();

        bindings.NotifyEnemyAction($"Ação: {enemyTurnActions.Format(enemyAction)}");

        int enemyRoll = 0;
        int playerRoll = 0;

        yield return RollForAction(bindings, enemyTurnActions.GetRollType(enemyAction), false, v => enemyRoll = v);
        yield return RollForAction(bindings, playerTurnActions.GetRollType(passivePlayerAction), true, v => playerRoll = v);

        ResolveActions(passivePlayerAction, enemyAction, playerRoll, enemyRoll, bindings);
        yield return new WaitForSeconds(0.6f);
    }

    private void CachePlayerAction(PlayerActionType action)
    {
        pendingPlayerAction = action;
    }

    private IEnumerator RollForAction(CombatSceneBindings bindings, RollType rollType, bool isPlayer, System.Action<int> onFinished)
    {
        int statValue = GetStatByRollType(rollType, isPlayer);
        int roll = Random.Range(0, statValue + 1);

        if (bindings != null)
            yield return bindings.PlayDiceRoll(roll);

        onFinished?.Invoke(roll);
    }

    private int RollPercent(CombatSceneBindings bindings)
    {
        int roll = Random.Range(0, 100);
        if (bindings != null)
            bindings.StartCoroutine(bindings.PlayDiceRoll(roll));
        return roll;
    }

    private void ResolveActions(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        string resultLog;

        if (enemyTurnActions.IsAttack(enemyAction) && (playerAction == PlayerActionType.Defend || playerAction == PlayerActionType.Parry))
        {
            resultLog = ResolveDefensiveResponse(playerAction, enemyAction, playerRoll, enemyRoll, bindings);
            bindings.SetCombatLog(resultLog, CombatLogCategory.Action);
            return;
        }

        if (playerTurnActions.IsAttack(playerAction))
        {
            bool attackSuccess = playerRoll > enemyRoll;
            if (!attackSuccess)
            {
                resultLog = $"Seu ataque ({playerAction}) falhou: {playerRoll} <= defesa inimiga {enemyRoll}.";
                bindings.SetCombatLog(resultLog, CombatLogCategory.Action);
            }
            else
            {
                int damage = Mathf.Max(1, playerStats.attack - enemyStats.defense);
                bool criticalHit = RollPercent(bindings) < playerStats.criticalHitChance;
                if (criticalHit)
                    damage *= 2;

                ApplyDamageToEnemy(playerAction, damage);
                bindings.NotifyEnemyDamage(damage, criticalHit);
                resultLog = criticalHit
                    ? $"Ataque crítico ({playerAction})! Dano causado: {damage}."
                    : $"Você atacou ({playerAction}) e causou {damage} de dano.";
                bindings.SetCombatLog(resultLog, CombatLogCategory.Damage);
            }
        }
        else
        {
            resultLog = ResolvePlayerSpecialAction(playerAction, bindings);
            bindings.SetCombatLog(resultLog, CombatLogCategory.Action);
        }

        if (EnemyLife > 0 && PlayerLife > 0 && enemyTurnActions.IsAttack(enemyAction))
        {
            bool enemyAttackSuccess = enemyRoll > playerRoll;
            if (!enemyAttackSuccess)
            {
                bindings.SetCombatLog($"Ataque inimigo falhou: {enemyRoll} <= sua defesa {playerRoll}.", CombatLogCategory.Action);
                return;
            }

            int enemyDamage = Mathf.Max(1, enemyStats.attack - playerStats.defense);
            bool enemyCrit = RollPercent(bindings) < enemyStats.criticalHitChance;
            if (enemyCrit)
                enemyDamage *= 2;

            ApplyDamageToPlayer(enemyAction, enemyDamage);
            bindings.NotifyPlayerDamage(enemyDamage, enemyCrit);
            bindings.SetCombatLog($"Inimigo atacou ({enemyAction}) e causou {enemyDamage} de dano.", CombatLogCategory.Damage);
        }
    }

    private string ResolveDefensiveResponse(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        bool defenseSuccess = playerRoll >= enemyRoll;

        if (playerAction == PlayerActionType.Defend)
        {
            if (defenseSuccess)
                return $"Defesa bem sucedida: {playerRoll} >= ataque inimigo {enemyRoll}. Nenhum dano recebido.";

            int damage = Mathf.Max(1, enemyStats.attack - playerStats.defense);
            ApplyDamageToPlayer(enemyAction, damage);
            bindings.NotifyPlayerDamage(damage);
            return $"Defesa falhou ({playerRoll} < {enemyRoll}). Você recebeu {damage} de dano.";
        }

        int parryRoll = RollPercent(bindings);
        bool parryChanceSuccess = parryRoll < playerStats.parryChance;

        if (defenseSuccess && parryChanceSuccess)
        {
            int reflectedDamage = Mathf.Max(1, playerStats.attack - enemyStats.defense);
            ApplyDamageToEnemy(PlayerActionType.AttackLife, reflectedDamage);
            bindings.NotifyEnemyDamage(reflectedDamage);
            return $"Parry perfeito! Você refletiu {reflectedDamage} de dano ao inimigo.";
        }

        int parryFailDamage = Mathf.Max(1, enemyStats.attack - playerStats.defense);
        ApplyDamageToPlayer(enemyAction, parryFailDamage);
        bindings.NotifyPlayerDamage(parryFailDamage);
        return $"Parry falhou. Você recebeu {parryFailDamage} de dano.";
    }

    private string ResolvePlayerSpecialAction(PlayerActionType action, CombatSceneBindings bindings)
    {
        int chance = playerTurnActions.GetSpecialChance(action, playerStats);
        int roll = RollPercent(bindings);

        switch (action)
        {
            case PlayerActionType.Flee:
                if (roll < chance)
                {
                    EnemyLife = 0;
                    return $"Fuga bem sucedida! Chance {chance}% (rolagem {roll}).";
                }

                return $"Tentativa de fuga falhou. Chance {chance}% (rolagem {roll}).";
            case PlayerActionType.InstantKill:
                if (roll < chance)
                {
                    EnemyLife = 0;
                    return $"Instant Kill ativado! Chance {chance}% (rolagem {roll}).";
                }

                return $"Instant Kill falhou. Chance {chance}% (rolagem {roll}).";
            case PlayerActionType.Learn:
                if (roll < chance)
                    return $"Learn bem sucedido! Chance {chance}% (rolagem {roll}).";

                return $"Learn falhou. Chance {chance}% (rolagem {roll}).";
            case PlayerActionType.Item:
                return "Uso de item ainda é placeholder.";
            default:
                return $"Ação {action} é placeholder.";
        }
    }

    private void ApplyDamageToEnemy(PlayerActionType attackType, int amount)
    {
        if (amount <= 0)
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

    private int GetStatByRollType(RollType rollType, bool isPlayer)
    {
        return rollType switch
        {
            RollType.Life => isPlayer ? PlayerLife : EnemyLife,
            RollType.Physical => isPlayer ? PlayerPhysical : EnemyPhysical,
            RollType.Mental => isPlayer ? PlayerMental : EnemyMental,
            _ => 0
        };
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
