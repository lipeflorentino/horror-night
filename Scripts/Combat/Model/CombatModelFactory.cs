using UnityEngine;

public class CombatModelFactory
{
    private const int DefaultAttack = 10;
    private const int DefaultDefense = 5;
    private const int DefaultInitiative = 10;

    public CombatBattlerModel CreatePlayer(PlayerStatusSnapshot snapshot)
    {
        int heart = Mathf.Max(0, Mathf.RoundToInt(snapshot.heart));
        int body = Mathf.Max(0, Mathf.RoundToInt(snapshot.body));
        int mind = Mathf.Max(0, Mathf.RoundToInt(snapshot.mind));
        int hp = Mathf.Max(0, Mathf.RoundToInt(snapshot.hp));

        int maxHeart = Mathf.Max(0, Mathf.RoundToInt(snapshot.maxHeart));
        int maxBody = Mathf.Max(0, Mathf.RoundToInt(snapshot.maxBody));
        int maxMind = Mathf.Max(0, Mathf.RoundToInt(snapshot.maxMind));
        int maxHp = Mathf.Max(0, Mathf.RoundToInt(snapshot.maxHp));

        return new CombatBattlerModel
        {
            heart = Mathf.Clamp(heart, 0, maxHeart),
            body = Mathf.Clamp(body, 0, maxBody),
            mind = Mathf.Clamp(mind, 0, maxMind),
            hp = Mathf.Clamp(hp, 0, maxHp),
            maxHeart = maxHeart,
            maxBody = maxBody,
            maxMind = maxMind,
            maxHp = maxHp,
            attack = DefaultAttack,
            defense = DefaultDefense,
            initiative = DefaultInitiative
        };
    }

    public CombatBattlerModel CreateEnemy(EnemyInstance enemy)
    {
        if (enemy == null)
            return new CombatBattlerModel();

        int maxHeart = Mathf.Max(0, enemy.heart);
        int maxBody = Mathf.Max(0, enemy.body);
        int maxMind = Mathf.Max(0, enemy.mind);
        int maxHp = enemy.hp > 0 ? enemy.hp : enemy.body;
        maxHp = Mathf.Max(0, maxHp);

        return new CombatBattlerModel
        {
            heart = maxHeart,
            body = maxBody,
            mind = maxMind,
            hp = maxHp,
            maxHeart = maxHeart,
            maxBody = maxBody,
            maxMind = maxMind,
            maxHp = maxHp,
            attack = Mathf.Max(0, enemy.attack),
            defense = Mathf.Max(0, enemy.defense),
            initiative = Mathf.Max(0, enemy.initiative)
        };
    }
}
