using UnityEngine;

public class EnemyBattler : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    public EnemyInstance enemyData;

    public int life;
    public int physical;
    public int mental;
    public int attack;
    public int defense;
    public int criticalHitChance;
    public int parryChance;

    private void Awake()
    {
        if (enemySpriteRenderer == null)
            enemySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Setup(EnemyInstance enemy)
    {
        enemyData = enemy;
        if (enemyData == null || enemyData.source == null)
            return;

        gameObject.name = $"EnemyBattler_{enemyData.source.enemyName}";

        if (enemySpriteRenderer != null && enemyData.source.image != null)
            enemySpriteRenderer.sprite = enemyData.source.image;

        life = enemyData.life;
        physical = enemyData.physical;
        mental = enemyData.mental;
        attack = enemyData.combatStats.attack;
        defense = enemyData.combatStats.defense;
        criticalHitChance = enemyData.combatStats.criticalHitChance;
        parryChance = enemyData.combatStats.parryChance;

        Debug.Log($"Setup Enemy! enemy: {enemy.source.enemyName} | life: {life}, physical: {physical}, mental: {mental}, atk: {attack}, def: {defense}");
    }
}
