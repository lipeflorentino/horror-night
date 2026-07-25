using UnityEngine;

public class CombatInitializer
{
    public static (Battler player, Battler enemy, Sprite playerIcon, Sprite enemyIcon) InitializeBattlers(
        CombatSessionData sessionData, 
        int defaultDiceCount, 
        int coreStatCap)
    {
        Battler player;
        Battler enemy;
        Sprite playerIcon = null;
        Sprite enemyIcon = null;
        EnemyVisuals enemyVisuals = Object.FindObjectOfType<EnemyVisuals>();

        if (sessionData == null)
        {
            Logger.Log("[Combat] No CombatSessionData found. Using default battlers.");
            player = new Battler("Player", 1, 30, 12, 12, 12, 10, 5, 5, defaultDiceCount, true);
            enemy = new Battler("Enemy", 1, 20, 12, 12, 12, 6, 3, 3, defaultDiceCount, false);
            if (enemyVisuals != null)
            {
                enemyVisuals.SetEnemyVisual(null);
            }
            return (player, enemy, playerIcon, enemyIcon);
        }

        PlayerStatusSnapshot playerSnapshot = sessionData.PlayerSnapshot;
        EnemyInstance enemySnapshot = sessionData.EnemyInstance;
        if (enemyVisuals != null)
        {
            enemyVisuals.SetEnemyVisual(enemySnapshot);
        }

        playerIcon = playerSnapshot.characterIcon;
        if (enemySnapshot != null && enemySnapshot.source != null)
        {
            enemyIcon = enemySnapshot.source.image;
        }

        player = new Battler(
            string.IsNullOrWhiteSpace(playerSnapshot.characterName) ? "Player" : playerSnapshot.characterName,
            Mathf.Max(1, playerSnapshot.level),
            Mathf.RoundToInt(playerSnapshot.hp),
            ClampCoreStat(playerSnapshot.heart, coreStatCap),
            ClampCoreStat(playerSnapshot.mind, coreStatCap),
            ClampCoreStat(playerSnapshot.body, coreStatCap),
            Mathf.RoundToInt(playerSnapshot.attack),
            Mathf.RoundToInt(playerSnapshot.defense),
            Mathf.RoundToInt(playerSnapshot.initiative),
            Mathf.Max(1, playerSnapshot.maxDices > 0 ? playerSnapshot.maxDices : defaultDiceCount),
            true,
            Mathf.RoundToInt(playerSnapshot.maxHp > 0 ? playerSnapshot.maxHp : playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.focus),
            Mathf.RoundToInt(playerSnapshot.strength),
            Mathf.RoundToInt(playerSnapshot.agility),
            ClampCoreStat(playerSnapshot.heart, coreStatCap),
            ClampCoreStat(playerSnapshot.body, coreStatCap),
            ClampCoreStat(playerSnapshot.mind, coreStatCap)
        );

        if (enemySnapshot != null)
        {
            string enemyName = enemySnapshot.source != null ? enemySnapshot.source.enemyName : "Enemy";
            enemy = new Battler(
                enemyName,
                enemySnapshot.runTier,
                enemySnapshot.hp,
                enemySnapshot.heart,
                enemySnapshot.mind,
                enemySnapshot.body,
                enemySnapshot.attack,
                enemySnapshot.defense,
                enemySnapshot.initiative,
                enemySnapshot.currentDices > 0 ? enemySnapshot.currentDices : defaultDiceCount,
                false,
                -1,
                enemySnapshot.focus,
                enemySnapshot.strength,
                enemySnapshot.agility,
                enemySnapshot.heart,
                enemySnapshot.body,
                enemySnapshot.mind
            );
        }
        else
        {
            Logger.Log("[Combat] Enemy snapshot missing. Using default enemy.");
            enemy = new Battler("Enemy", 1, 20, 12, 12, 12, 6, 3, 3, defaultDiceCount, false);
        }

        return (player, enemy, playerIcon, enemyIcon);
    }

    public static int ClampCoreStat(float value, int coreStatCap)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value), 0, coreStatCap);
    }
}
