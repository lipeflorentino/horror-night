using UnityEngine;
public class EnemyVisuals : MonoBehaviour
{
    
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning("[Combat] No SpriteRenderer found in children of EnemyVisuals.");
            }
        }
    }
    public void SetEnemyVisual(EnemyInstance enemySnapshot = null)
    {
        Sprite enemySprite = enemySnapshot != null && enemySnapshot.source != null
            ? enemySnapshot.source.image
            : null;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = enemySprite;
        }
        else
        {
            Debug.LogWarning("[Combat] Could not find SpriteRenderer to set enemy image.");
        }
    }
}