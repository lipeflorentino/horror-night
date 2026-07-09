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
    private Color flashColor = new(0.9f, 0.1f, 0.1f, EnemyFlashAlpha);
    
    [Header("Enemy Damage Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private Canvas worldPopupCanvas;
    [SerializeField] private GameObject enemyVisual;
    [SerializeField] private Color damageColor = new(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color statusColor = new(0.1f, 0.1f, 1f, 1f);

    void Start()
    {
        popupObject.SetActive(false);
        if (worldPopupCanvas == null)
        {
            Debug.LogError("EnemyFeedbacks: World popup canvas reference is missing.");
            return;
        }
        if (popupObject == null)
        {
            Debug.LogError("EnemyFeedbacks: Popup object reference is missing.");
            return;
        }
    }

    public void ShowDamagePopup(int damage)
    {
        ShowPopupText($"-{damage}", damageColor);
        StartCoroutine(AnimateEnemyFlash());
    }

    public void ShowStatusPopup(string text)
    {
        ShowPopupText(text, statusColor);
    }

    private void ShowPopupText(string text, Color color)
    {
        popupObject.SetActive(true);

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        TextMeshProUGUI popupText = popupObject.GetComponent<TextMeshProUGUI>();

        if (popupRect == null || popupText == null)
        {
            Debug.LogError("EnemyFeedbacks: Popup object is missing RectTransform or TextMeshProUGUI component.");
            return;
        }

        popupText.text = text;
        popupText.color = color;

        StartCoroutine(AnimateEnemyPopup(popupRect, popupText));
    }

    private IEnumerator AnimateEnemyPopup(RectTransform popupRect, TextMeshProUGUI popupText)
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
        SpriteRenderer spriteRenderer = enemyVisual.GetComponent<SpriteRenderer>();
        
        Color InitialColor = spriteRenderer.color;
        Color color = flashColor;
        spriteRenderer.color = color;

        float elapsed = 0f;
        while (elapsed < EnemyFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / EnemyFlashDuration);
            color.a = Mathf.Lerp(EnemyFlashAlpha, 1f, t);
            spriteRenderer.color = color;
            yield return null;
        }
        spriteRenderer.color = InitialColor;
    }
}
