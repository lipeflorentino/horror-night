using System;
using System.Collections;
using UnityEngine;

public class TurnActions
{
    public struct SpecialActionResolution
    {
        public bool success;
        public bool endCombat;
        public CombatOutcome forcedOutcome;
        public string log;
        public string feedback;
        public float postDelay;
    }

    public int PlayerHeart { get; private set; }
    public int PlayerBody { get; private set; }
    public int PlayerMind { get; private set; }

    public int EnemyHeart { get; private set; }
    public int EnemyBody { get; private set; }
    public int EnemyMind { get; private set; }

    public int BasePlayerHeart { get; private set; }
    public int BasePlayerBody { get; private set; }
    public int BasePlayerMind { get; private set; }

    public int BaseEnemyHeart { get; private set; }
    public int BaseEnemyBody { get; private set; }
    public int BaseEnemyMind { get; private set; }

    public int EnemyRevealLevel { get; private set; }

    public TurnManagerStats PlayerStats { get; private set; }
    public TurnManagerStats EnemyStats { get; private set; }

    private readonly PlayerTurnActions playerTurnActions;
    private readonly EnemyTurnActions enemyTurnActions;

    public TurnActions(PlayerTurnActions playerTurnActions, EnemyTurnActions enemyTurnActions)
    {
        this.playerTurnActions = playerTurnActions;
        this.enemyTurnActions = enemyTurnActions;
    }

    public void Initialize(CombatSceneBindings bindings, RunStateSnapshot snapshot, EnemyInstance enemy)
    {
        PlayerHeart = Mathf.Max(0, Mathf.RoundToInt(snapshot.playerStatus.heart));
        PlayerBody = Mathf.Max(0, Mathf.RoundToInt(snapshot.playerStatus.body));
        PlayerMind = Mathf.Max(0, Mathf.RoundToInt(snapshot.playerStatus.mind));

        EnemyHeart = Mathf.Max(0, enemy.heart);
        EnemyBody = Mathf.Max(0, enemy.body);
        EnemyMind = Mathf.Max(0, enemy.mind);

        BasePlayerHeart = Mathf.Max(1, Mathf.RoundToInt(snapshot.playerStatus.maxHeart > 0f ? snapshot.playerStatus.maxHeart : PlayerHeart));
        BasePlayerBody = Mathf.Max(1, Mathf.RoundToInt(snapshot.playerStatus.maxBody > 0f ? snapshot.playerStatus.maxBody : PlayerBody));
        BasePlayerMind = Mathf.Max(1, Mathf.RoundToInt(snapshot.playerStatus.maxMind > 0f ? snapshot.playerStatus.maxMind : PlayerMind));

        BaseEnemyHeart = Mathf.Max(1, EnemyHeart);
        BaseEnemyBody = Mathf.Max(1, EnemyBody);
        BaseEnemyMind = Mathf.Max(1, EnemyMind);

        EnemyRevealLevel = 0;

        PlayerStats = snapshot.playerStatus.combatStats;
        if (PlayerStats.attack <= 0)
            PlayerStats = TurnManagerStats.BuildDefault(PlayerHeart, PlayerBody, PlayerMind);
        PlayerStats.Normalize();

        EnemyStats = enemy.combatStats;
        if (EnemyStats.attack <= 0)
            EnemyStats = TurnManagerStats.BuildDefault(EnemyHeart, EnemyBody, EnemyMind);
        EnemyStats.Normalize();

        bindings.UpdateHud(PlayerHeart, BasePlayerHeart, PlayerBody, BasePlayerBody, PlayerMind, BasePlayerMind, EnemyHeart, BaseEnemyHeart, EnemyBody, BaseEnemyBody, EnemyMind, BaseEnemyMind);
        bindings.UpdateSpecialActionAvailability(CanUseInstantKill(), CanUseLearn());
        bindings.SetEnemyLearnState(EnemyRevealLevel, enemy.source, EnemyHeart, EnemyBody, EnemyMind, EnemyStats);
    }

    public IEnumerator RollForAction(CombatSceneBindings bindings, RollType rollType, bool isPlayer, Action<int> onFinished)
    {
        int statValue = GetStatByRollType(rollType, isPlayer);
        int roll = statValue <= 0 ? 0 : UnityEngine.Random.Range(0, statValue + 1);

        if (bindings != null)
            yield return bindings.PlayDiceRoll(roll);

        onFinished?.Invoke(roll);
    }

    public IEnumerator ResolveActions(bool attackerIsPlayer, PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        yield return new WaitForSeconds(1f);

        if (attackerIsPlayer)
        {
            if (playerTurnActions.IsAttack(playerAction))
            {
                bool success = playerRoll > enemyRoll;
                bindings.NotifyPlayerAction(success ? "Sucesso no ataque!" : "Falha no ataque!");

                if (!success)
                {
                    bindings.SetCombatLog($"Seu ataque falhou: {playerRoll} <= defesa inimiga {enemyRoll}.", CombatLogCategory.Action);
                    bindings.ResetDiceValue();
                    yield break;
                }

                int damage = Mathf.Max(1, PlayerStats.attack - EnemyStats.defense);
                bool criticalHit = RollPercent() < PlayerStats.criticalHitChance;
                if (criticalHit)
                    damage *= 2;

                ApplyDamageToEnemy(playerAction, damage, bindings);
                bindings.NotifyEnemyDamage(damage, criticalHit);
                bindings.SetCombatLog(criticalHit
                    ? $"Ataque crítico! Dano causado: {damage}."
                    : $"Ataque bem sucedido! Dano causado: {damage}.", CombatLogCategory.Damage);
                bindings.ResetDiceValue();
                yield break;
            }

            bindings.ResetDiceValue();
            yield break;
        }

        string resultLog = ResolveDefensiveResponse(playerAction, enemyAction, playerRoll, enemyRoll, bindings);
        bindings.SetCombatLog(resultLog, CombatLogCategory.Action);
        bindings.ResetDiceValue();
    }

    public SpecialActionResolution ResolvePlayerSpecialAction(PlayerActionType action, CombatSceneBindings bindings)
    {
        int chance = playerTurnActions.GetSpecialChance(action, PlayerStats);
        int roll = RollPercent();
        bool success = roll < chance;

        SpecialActionResolution result = new SpecialActionResolution
        {
            success = success,
            endCombat = false,
            forcedOutcome = CombatOutcome.Ongoing,
            postDelay = 0f
        };

        switch (action)
        {
            case PlayerActionType.Flee:
                if (success)
                {
                    result.endCombat = true;
                    result.forcedOutcome = CombatOutcome.Fled;
                    result.log = $"Fuga bem sucedida! Chance {chance}% (rolagem {roll}).";
                    result.feedback = "Você escapou, mas ficou mais tenso.";
                }
                else
                {
                    result.log = $"Tentativa de fuga falhou. Chance {chance}% (rolagem {roll}).";
                    result.feedback = "A fuga falhou.";
                }

                return result;

            case PlayerActionType.InstantKill:
                if (!CanUseInstantKill())
                {
                    result.success = false;
                    result.log = "Instant Kill indisponível: reduza Coração, Corpo ou Mente do inimigo a 0.";
                    result.feedback = "Instant Kill bloqueado.";
                    return result;
                }

                if (success)
                {
                    EnemyHeart = 0;
                    result.endCombat = true;
                    result.forcedOutcome = CombatOutcome.Victory;
                    result.log = $"Instant Kill ativado! Chance {chance}% (rolagem {roll}).";
                    result.feedback = "Golpe fatal!";
                    result.postDelay = 0.8f;
                }
                else
                {
                    result.log = $"Instant Kill falhou. Chance {chance}% (rolagem {roll}).";
                    result.feedback = "O golpe fatal falhou.";
                }

                bindings.UpdateAttackButtonAvailability(CanAttackEnemyHeart(), CanAttackEnemyBody(), CanAttackEnemyMind());
                bindings.UpdateHud(PlayerHeart, BasePlayerHeart, PlayerBody, BasePlayerBody, PlayerMind, BasePlayerMind, EnemyHeart, BaseEnemyHeart, EnemyBody, BaseEnemyBody, EnemyMind, BaseEnemyMind);
                bindings.UpdateSpecialActionAvailability(CanUseInstantKill(), CanUseLearn());
                return result;

            case PlayerActionType.Learn:
                if (!CanUseLearn())
                {
                    result.success = false;
                    result.log = "Learn indisponível: inimigo já totalmente revelado.";
                    result.feedback = "Learn bloqueado.";
                    return result;
                }

                if (success)
                {
                    EnemyRevealLevel = Mathf.Clamp(EnemyRevealLevel + 1, 0, 2);
                    result.log = $"Learn bem sucedido! Chance {chance}% (rolagem {roll}).";
                    result.feedback = EnemyRevealLevel == 1
                        ? "Informações básicas reveladas."
                        : "Informações avançadas reveladas.";
                }
                else
                {
                    result.log = $"Learn falhou. Chance {chance}% (rolagem {roll}).";
                    result.feedback = "Não conseguiu aprender nada novo.";
                }

                bindings.UpdateSpecialActionAvailability(CanUseInstantKill(), CanUseLearn());
                return result;
            case PlayerActionType.Item:
                result.log = "Uso de item ainda é placeholder.";
                result.feedback = "Item ainda não implementado.";
                return result;
            default:
                result.log = $"Ação {action} é placeholder.";
                result.feedback = "Ação especial inválida.";
                return result;
        }
    }

    public string ResolveDefensiveResponse(PlayerActionType playerAction, EnemyActionType enemyAction, int playerRoll, int enemyRoll, CombatSceneBindings bindings)
    {
        bool defenseSuccess = playerRoll >= enemyRoll;
        bindings.NotifyPlayerAction(defenseSuccess ? "Defesa bem sucedida!" : "Defesa falhou!");

        if (playerAction == PlayerActionType.Defend)
        {
            if (defenseSuccess)
                return $"Defesa bem sucedida: {playerRoll} >= {enemyRoll}. Nenhum dano recebido.";

            int damage = Mathf.Max(1, EnemyStats.attack - PlayerStats.defense);
            ApplyDamageToPlayer(enemyAction, damage, bindings);
            bindings.NotifyPlayerDamage(damage);
            return $"Defesa falhou ({playerRoll} < {enemyRoll}). Você recebeu {damage} de dano.";
        }

        int parryRoll = RollPercent();
        bool parryChanceSuccess = parryRoll < PlayerStats.parryChance;

        if (defenseSuccess && parryChanceSuccess)
        {
            int reflectedDamage = Mathf.Max(1, PlayerStats.attack - EnemyStats.defense);
            ApplyDamageToEnemy(PlayerActionType.AttackHeart, reflectedDamage, bindings);
            bindings.NotifyEnemyDamage(reflectedDamage);
            return $"Parry perfeito! Você refletiu {reflectedDamage} de dano ao inimigo.";
        }

        int parryFailDamage = Mathf.Max(1, EnemyStats.attack - PlayerStats.defense);
        ApplyDamageToPlayer(enemyAction, parryFailDamage, bindings);
        bindings.NotifyPlayerDamage(parryFailDamage);
        return $"Parry falhou. Você recebeu {parryFailDamage} de dano.";
    }

    public void ApplyDamageToEnemy(PlayerActionType attackType, int amount, CombatSceneBindings bindings)
    {
        if (amount <= 0)
            return;

        switch (attackType)
        {
            case PlayerActionType.AttackHeart:
                EnemyHeart = Mathf.Clamp(EnemyHeart - amount, 0, BaseEnemyHeart);
                break;
            case PlayerActionType.AttackBody:
                EnemyBody = Mathf.Clamp(EnemyBody - amount, 0, BaseEnemyBody);
                break;
            case PlayerActionType.AttackMind:
                EnemyMind = Mathf.Clamp(EnemyMind - amount, 0, BaseEnemyMind);
                break;
            default:
                EnemyHeart = Mathf.Clamp(EnemyHeart - amount, 0, BaseEnemyHeart);
                break;
        }

        bindings.UpdateAttackButtonAvailability(CanAttackEnemyHeart(), CanAttackEnemyBody(), CanAttackEnemyMind());
        bindings.UpdateSpecialActionAvailability(CanUseInstantKill(), CanUseLearn());
        bindings.UpdateHud(PlayerHeart, BasePlayerHeart, PlayerBody, BasePlayerBody, PlayerMind, BasePlayerMind, EnemyHeart, BaseEnemyHeart, EnemyBody, BaseEnemyBody, EnemyMind, BaseEnemyMind);
    }

    public void ApplyDamageToPlayer(EnemyActionType attackType, int amount, CombatSceneBindings bindings)
    {
        if (amount <= 0)
            return;

        switch (attackType)
        {
            case EnemyActionType.AttackHeart:
                PlayerHeart = Mathf.Max(0, PlayerHeart - amount);
                break;
            case EnemyActionType.AttackBody:
                PlayerBody = Mathf.Max(0, PlayerBody - amount);
                break;
            case EnemyActionType.AttackMind:
                PlayerMind = Mathf.Max(0, PlayerMind - amount);
                break;
        }

        bindings.UpdateHud(PlayerHeart, BasePlayerHeart, PlayerBody, BasePlayerBody, PlayerMind, BasePlayerMind, EnemyHeart, BaseEnemyHeart, EnemyBody, BaseEnemyBody, EnemyMind, BaseEnemyMind);
    }

    private int RollPercent()
    {
        return UnityEngine.Random.Range(0, 100);
    }

    private int GetStatByRollType(RollType rollType, bool isPlayer)
    {
        return rollType switch
        {
            RollType.Heart => isPlayer ? PlayerHeart : EnemyHeart,
            RollType.Body => isPlayer ? PlayerBody : EnemyBody,
            RollType.Mind => isPlayer ? PlayerMind : EnemyMind,
            _ => 0
        };
    }

    public bool CanAttackEnemyHeart() => EnemyHeart > 0;
    public bool CanAttackEnemyBody() => EnemyBody > 0;
    public bool CanAttackEnemyMind() => EnemyMind > 0;
    public bool CanUseInstantKill() => EnemyHeart <= 0 || EnemyBody <= 0 || EnemyMind <= 0;
    public bool CanUseLearn() => EnemyRevealLevel < 2;
}
