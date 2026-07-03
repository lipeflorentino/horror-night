using UnityEngine;

public class CombatInitializer
{
    public static (Battler player, Battler enemy, Sprite playerIcon, Sprite enemyIcon, EnemyVisuals enemyVisuals) InitializeBattlers(
        CombatSessionData sessionData, 
        int defaultPowerDiceCount, 
        int defaultAccuracyDiceCount, 
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
            player = new Battler("Player", 1, 20, 10, 10, 10, 10, 5, 5, defaultPowerDiceCount, defaultAccuracyDiceCount, true);
            enemy = new Battler("Enemy", 1, 20, 10, 10, 10, 6, 3, 5, defaultPowerDiceCount, defaultAccuracyDiceCount, false);
            if (enemyVisuals != null)
            {
                enemyVisuals.SetEnemyVisual(null);
            }
            return (player, enemy, playerIcon, enemyIcon, enemyVisuals);
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
            Mathf.Max(1, playerSnapshot.maxPowerDices > 0 ? playerSnapshot.maxPowerDices : defaultPowerDiceCount),
            Mathf.Max(1, playerSnapshot.maxAccuracyDices > 0 ? playerSnapshot.maxAccuracyDices : defaultAccuracyDiceCount),
            true,
            Mathf.RoundToInt(playerSnapshot.maxHp > 0 ? playerSnapshot.maxHp : playerSnapshot.hp),
            Mathf.RoundToInt(playerSnapshot.focus),
            Mathf.RoundToInt(playerSnapshot.strength),
            Mathf.RoundToInt(playerSnapshot.agility)
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
                enemySnapshot.currentPowerDices > 0 ? enemySnapshot.currentPowerDices : defaultPowerDiceCount,
                enemySnapshot.currentAccuracyDices > 0 ? enemySnapshot.currentAccuracyDices : defaultAccuracyDiceCount,
                false,
                -1,
                enemySnapshot.focus,
                enemySnapshot.strength,
                enemySnapshot.agility
            );
        }
        else
        {
            Logger.Log("[Combat] Enemy snapshot missing. Using default enemy.");
            enemy = new Battler("Enemy", 1, 100, 10, 10, 10, 10, 5, 5, defaultPowerDiceCount, defaultAccuracyDiceCount, false);
        }

        return (player, enemy, playerIcon, enemyIcon, enemyVisuals);
    }

    public static int ClampCoreStat(float value, int coreStatCap)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value), 0, coreStatCap);
    }
}
