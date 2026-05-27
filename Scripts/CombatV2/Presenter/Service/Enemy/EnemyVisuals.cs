using UnityEngine;
public class EnemyVisuals : MonoBehaviour
{
    [SerializeField] private GameObject enemyBattler;
    public void SetEnemyVisual(EnemyInstance enemySnapshot = null)
    {
        Sprite enemySprite = enemySnapshot != null && enemySnapshot.source != null
            ? enemySnapshot.source.image
            : null;

        enemyBattler = GameObject.Find("EnemyBattler");
        if (enemyBattler == null)
        {
            Debug.LogWarning("[Combat] EnemyBattler GameObject not found.");
            return;
        }

        Transform visualTransform = enemyBattler.transform.Find("EnemyVisual");
        if (visualTransform == null)
        {
            Debug.LogWarning("[Combat] EnemyVisual transform not found.");
            return;
        }

        if (visualTransform.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.sprite = enemySprite;
        }
        else
        {
            Debug.LogWarning("[Combat] Could not find SpriteRenderer to set enemy image.");
        }
    }
}