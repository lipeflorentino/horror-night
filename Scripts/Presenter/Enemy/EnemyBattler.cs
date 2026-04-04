using UnityEngine;

public class EnemyBattler : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    public EnemyInstance enemyData;

    public int heart;
    public int body;
    public int mind;
    public int attack;
    public int defense;
    public int initiative;
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

        heart = enemyData.heart;
        body = enemyData.body;
        mind = enemyData.mind;

        Debug.Log($"Setup Enemy! enemy: {enemy.source.enemyName} | heart: {heart}, body: {body}, mind: {mind}");
    }
}
