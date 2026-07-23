using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatEndService
{
    public static bool TryHandleCombatEnd(
        Battler player, 
        Battler enemy, 
        CombatTurnContext state, 
        CombatView view, 
        RewardService rewardService, 
        CombatSessionData sessionData,
        System.Action onProceed,
        System.Action onRestart,
        System.Action onQuit)
    {
        if (player.IsAlive() && enemy.IsAlive())
            return false;

        player.ResetMomentum();
        enemy.ResetMomentum();

        state.CombatEnded = true;
        view.SetCombatInputEnabled(false);

        bool playerWon = player.IsAlive() && !enemy.IsAlive();

        if (playerWon)
        {
            state.LastGrantedXp = rewardService.GrantXpRewardIfEligible(enemy.Level, player.Level);
            if (sessionData?.EnemyInstance?.source != null)
            {
                state.LastGrantedItens = rewardService.GetRandomLoot(enemy.Level);
            }
            else
            {
                state.LastGrantedItens = new Dictionary<ItemSO, int>();
            }
            view.CombatEndView.ShowVictory(state.LastGrantedXp, state.LastGrantedItens, () => onProceed?.Invoke());
        }
        else
        {
            view.CombatEndView.ShowGameOver(() => onRestart?.Invoke(), () => onQuit?.Invoke());
        }

        return true;
    }

    public static void RestartCombat(string gameplaySceneName)
    {
        CombatSessionStore.Clear();
        CombatResultStore.Clear();
        CombatReturnStore.Clear();
        SceneManager.LoadScene(gameplaySceneName);
    }

    public static void QuitCombat()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public static void ProceedToGameplayScene(
        Battler player,
        ITrickInventory playerTrickInventory,
        ICombatInventory combatPlayerInventory,
        CombatSessionData sessionData,
        CombatTurnContext state,
        int coreStatCap,
        string gameplaySceneName)
    {
        CombatResultStore.SetResult(new CombatResultSnapshot
        {
            PlayerSnapshot = BuildResultPlayerSnapshot(player, playerTrickInventory, combatPlayerInventory, sessionData, coreStatCap),
            EnemyInstance = sessionData?.EnemyInstance,
            PlayerWon = true,
            XpGained = state.LastGrantedXp,
            ItensGained = state.LastGrantedItens,
        });

        CombatReturnStore.Set(new CombatReturnSnapshot
        {
            SceneName = sessionData != null ? sessionData.ReturnSceneName : gameplaySceneName,
            Level = sessionData?.ReturnLevel,
            LevelIndex = sessionData != null ? sessionData.ReturnLevelIndex : 0,
            ExploredNodes = sessionData?.ReturnExploredNodes,
            PlayerPosition = sessionData != null ? sessionData.ReturnPlayerPosition : Vector3.zero
        });

        string targetScene = sessionData != null && !string.IsNullOrWhiteSpace(sessionData.ReturnSceneName)
            ? sessionData.ReturnSceneName
            : gameplaySceneName;
        SceneManager.LoadScene(targetScene);
    }

    public static PlayerStatusSnapshot BuildResultPlayerSnapshot(
        Battler player,
        ITrickInventory playerTrickInventory,
        ICombatInventory combatPlayerInventory,
        CombatSessionData sessionData,
        int coreStatCap)
    {
        if (sessionData == null)
        {
            return new PlayerStatusSnapshot
            {
                hp = player.HP,
                heart = CombatInitializer.ClampCoreStat(player.Heart, coreStatCap),
                mind = CombatInitializer.ClampCoreStat(player.Mind, coreStatCap),
                body = CombatInitializer.ClampCoreStat(player.Body, coreStatCap),
                attack = player.Attack,
                defense = player.Defense,
                initiative = player.Initiative,
                focus = player.Focus,
                strength = player.Strength,
                agility = player.Agility,
                maxHeart = CombatInitializer.ClampCoreStat(player.Heart, coreStatCap),
                maxMind = CombatInitializer.ClampCoreStat(player.Mind, coreStatCap),
                maxBody = CombatInitializer.ClampCoreStat(player.Body, coreStatCap),
                maxHp = player.MaxHp,
                currentDices = player.CurrentDices,
                maxDices = player.MaxDices,
                trickInventory = playerTrickInventory != null
                    ? TrickInventorySnapshot.CreatePersistentSnapshot(playerTrickInventory.GetSnapshot())
                    : new TrickInventorySnapshot()
            };
        }

        PlayerStatusSnapshot snapshot = sessionData.PlayerSnapshot;
        snapshot.hp = player.HP;
        snapshot.heart = CombatInitializer.ClampCoreStat(player.Heart, coreStatCap);
        snapshot.mind = CombatInitializer.ClampCoreStat(player.Mind, coreStatCap);
        snapshot.body = CombatInitializer.ClampCoreStat(player.Body, coreStatCap);
        snapshot.attack = player.Attack;
        snapshot.defense = player.Defense;
        snapshot.initiative = player.Initiative;
        snapshot.focus = player.Focus;
        snapshot.strength = player.Strength;
        snapshot.agility = player.Agility;
        snapshot.maxHeart = CombatInitializer.ClampCoreStat(Mathf.Max(snapshot.maxHeart, snapshot.heart), coreStatCap);
        snapshot.maxMind = CombatInitializer.ClampCoreStat(Mathf.Max(snapshot.maxMind, snapshot.mind), coreStatCap);
        snapshot.maxBody = CombatInitializer.ClampCoreStat(Mathf.Max(snapshot.maxBody, snapshot.body), coreStatCap);
        snapshot.maxHp = player.MaxHp;
        snapshot.currentDices = player.CurrentDices;
        snapshot.maxDices = player.MaxDices;
        if (combatPlayerInventory != null)
            snapshot.inventory = combatPlayerInventory.GetSnapshot();
        if (playerTrickInventory != null)
            snapshot.trickInventory = TrickInventorySnapshot.CreatePersistentSnapshot(playerTrickInventory.GetSnapshot());
        return snapshot;
    }
}
