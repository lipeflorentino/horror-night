using UnityEngine;

public class EnemyBattler : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    public EnemyInstance enemyData;

    private void Awake()
    {
        if (enemySpriteRenderer == null)
            enemySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Setup(EnemyInstance enemy)
    {
        Debug.Log($"Setup Enemy! enemy: {enemy.source.enemyName}");
        enemyData = enemy;
        if (enemyData == null || enemyData.source == null)
            return;

        gameObject.name = $"EnemyBattler_{enemyData.source.enemyName}";

        if (enemySpriteRenderer != null && enemyData.source.image != null)
            enemySpriteRenderer.sprite = enemyData.source.image;
    }
}
