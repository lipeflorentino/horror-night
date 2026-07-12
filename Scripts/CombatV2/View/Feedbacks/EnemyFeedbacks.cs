using System.Collections;
using TMPro;
using UnityEngine;

public class EnemyFeedbacks : MonoBehaviour
{
    private const float EnemyPopupDuration = 0.55f;
    private const float EnemyPopupRiseDistance = 0.75f;
    private const float EnemyPopupStartScale = 0.7f;
    private const float EnemyPopupBounceScale = 1.2f;
    private const float EnemyFlashDuration = 0.15f;
    private const float EnemyFlashAlpha = 0.9f;
    private const float EnemyStatusDuration = 2f;
    private Color flashColor = new(0.9f, 0.1f, 0.1f, EnemyFlashAlpha);
    
    [Header("Enemy Damage Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private Canvas worldPopupCanvas;
    [SerializeField] private GameObject enemyVisual;
    [SerializeField] private Color damageColor = new(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color statusColor = new(0.1f, 0.1f, 1f, 1f);

    private RectTransform popupRect;
    private TextMeshProUGUI popupText;
    private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private GameObject actionLogPanel;
    [SerializeField] private TMP_Text enemyStatusText;

    void Start()
    {
        if (worldPopupCanvas == null)
        {
            Debug.LogError("[EnemyFeedbacks] World popup canvas reference is missing.");
            return;
        }

        if (popupObject == null)
        {
            Debug.LogError("[EnemyFeedbacks] Popup object reference is missing.");
            return;
        }

        popupRect = popupObject.GetComponent<RectTransform>();
        popupText = popupObject.GetComponent<TextMeshProUGUI>();

        if (popupRect == null || popupText == null)
            Debug.LogError("[EnemyFeedbacks] Popup object is missing RectTransform or TextMeshProUGUI component.");

        if (enemyVisual != null)
            enemySpriteRenderer = enemyVisual.GetComponent<SpriteRenderer>();

        if (enemySpriteRenderer == null)
            Debug.LogError("[EnemyFeedbacks] Enemy visual reference or SpriteRenderer is missing.");

        popupObject.SetActive(false);
        actionLogPanel.SetActive(false);
    }

    public void ShowDamagePopup(int damage)
    {
        ShowPopupText($"-{damage}", damageColor);
        StartCoroutine(AnimateEnemyFlash());
    }

    public void ShowStatusPopup(string text)
    {
        actionLogPanel.SetActive(true);
        StartCoroutine(AnimateActionLog(text));
    }

    private void ShowPopupText(string text, Color color)
    {
        if (popupObject == null || popupRect == null || popupText == null)
        {
            Debug.LogError("[EnemyFeedbacks] Cannot show popup, references are missing.");
            return;
        }

        popupObject.SetActive(true);
        popupText.text = text;
        popupText.color = color;

        StartCoroutine(AnimateEnemyPopup());
    }

    private IEnumerator AnimateEnemyPopup()
    {
        Vector3 startPosition = popupRect.position;
        Vector3 endPosition = startPosition + Vector3.up * EnemyPopupRiseDistance;

        float elapsed = 0f;
        while (elapsed < EnemyPopupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / EnemyPopupDuration);

            popupRect.position = Vector3.Lerp(startPosition, endPosition, t);

            float scaleT = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(EnemyPopupStartScale, EnemyPopupBounceScale, scaleT);
            popupRect.localScale = Vector3.one * scale;

            Color color = popupText.color;
            color.a = 1f - t;
            popupText.color = color;

            yield return null;
        }

        popupObject.SetActive(false);
        popupRect.position = startPosition;
        popupRect.localScale = Vector3.one * EnemyPopupStartScale;
    }

    private IEnumerator AnimateEnemyFlash()
    {
        if (enemySpriteRenderer == null)
        {
            Debug.LogError("[EnemyFeedbacks] Cannot animate flash, SpriteRenderer reference is missing.");
            yield break;
        }

        Color initialColor = enemySpriteRenderer.color;
        enemySpriteRenderer.color = flashColor;

        yield return FadeUtility.FadeSpriteAlpha(enemySpriteRenderer, EnemyFlashAlpha, 1f, EnemyFlashDuration);

        enemySpriteRenderer.color = initialColor;
    }

    

    private IEnumerator AnimateActionLog(string text)
    {
        enemyStatusText.text = text;
        yield return new WaitForSeconds(EnemyStatusDuration);
        actionLogPanel.SetActive(false);
    }
}