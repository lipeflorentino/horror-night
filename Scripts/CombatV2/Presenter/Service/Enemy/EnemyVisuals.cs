using UnityEngine;
public class EnemyVisuals : MonoBehaviour
{
    
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float targetWidth = 3f;
    [SerializeField] private float targetHeight = 4f;
    [Header("Breathing")]
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private float breathingAmount = 0.02f;

    [Header("Floating")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatAmount = 0.03f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float rotationAmount = 1f;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private EnemyTypeTag enemyTypeTag;
    private readonly Sprite enemySprite;

    private float seed;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning("[Combat] No SpriteRenderer found in children of EnemyVisuals.");
            } else
            {
                ResizeToFit(spriteRenderer.sprite);
                baseScale = spriteRenderer.gameObject.transform.localScale;
                basePosition = spriteRenderer.gameObject.transform.localPosition;

                seed = Random.Range(0f, 100f);
            }
        }
    }

    private void Update()
    {
        float time = Time.time + seed;
        AnimateRotation(time);

        if (enemyTypeTag == EnemyTypeTag.Creature) AnimateBreathing(time);
        if (enemyTypeTag == EnemyTypeTag.Flying) AnimateFloating(time);
        if (enemyTypeTag == EnemyTypeTag.Hybrid) AnimateRotation(time);
    }

    public void SetEnemyVisual(EnemyInstance enemySnapshot = null)
    {
        Sprite enemySprite = null;
        Logger.Log("[Combat] Setting enemy visuals.");
        if (enemySnapshot != null)
        {
            enemyTypeTag = enemySnapshot.source.tags.type;
            enemySprite = enemySnapshot.source.image;
        }
        else
        {
            Logger.Log("[Combat] No enemy snapshot provided, using default visuals.");
        }
        
        
        if (spriteRenderer != null)
        {
            if (enemySprite != null)
            {
                spriteRenderer.sprite = enemySprite;
            }
            ResizeToFit(enemySprite);
        }
        else
        {
            Debug.LogWarning("[Combat] Could not find SpriteRenderer to set enemy image.");
        }
    }

    private void ResizeToFit(Sprite sprite)
    {
        Sprite defaultSprite = spriteRenderer.sprite;

        if (sprite == null)
        {
            sprite = defaultSprite;
        }

        Logger.Log("[Combat] Resizing enemy sprite to fit target dimensions.");
        spriteRenderer.gameObject.transform.localScale = Vector3.one;
        Vector2 spriteSize = sprite.bounds.size;

        float scaleX = targetWidth / spriteSize.x;
        float scaleY = targetHeight / spriteSize.y;

        float finalScale = Mathf.Min(scaleX, scaleY);
        spriteRenderer.gameObject.transform.localScale = new Vector3(finalScale, finalScale, 1f);
    }

    private void AnimateBreathing(float time)
    {
        float scaleOffset =
            Mathf.Sin(time * breathingSpeed) * breathingAmount;

        spriteRenderer.gameObject.transform.localScale =
            baseScale + new Vector3(scaleOffset, scaleOffset, 0f);
    }

    private void AnimateFloating(float time)
    {
        float yOffset =
            Mathf.Sin(time * floatSpeed) * floatAmount;

        Vector3 pos = spriteRenderer.gameObject.transform.localPosition;
        pos.y = basePosition.y + yOffset;

        spriteRenderer.gameObject.transform.localPosition = pos;
    }

    private void AnimateRotation(float time)
    {
        float zRotation =
            Mathf.Sin(time * rotationSpeed) * rotationAmount;

        spriteRenderer.gameObject.transform.localRotation =
            Quaternion.Euler(0f, 0f, zRotation);
    }
}