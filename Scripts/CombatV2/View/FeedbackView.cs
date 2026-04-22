using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackView : MonoBehaviour
{
    private const float EnemyPopupDuration = 0.55f;
    private const float EnemyPopupRiseDistance = 0.75f;
    private const float EnemyPopupStartScale = 0.7f;
    private const float EnemyPopupBounceScale = 1.2f;

    private const float PlayerFlashDuration = 0.15f;
    private const float PlayerFlashAlpha = 0.45f;

    public TMP_Text TurnOwnerText;

    [Header("Enemy Damage Popup")]
    [SerializeField] private Transform enemyPopupAnchor;
    [SerializeField] private TMP_FontAsset popupFont;
    [SerializeField] private int popupFontSize = 7;
    [SerializeField] private Color popupColor = new(1f, 0.25f, 0.25f, 1f);

    [Header("Player Damage Flash")]
    [SerializeField] private Color playerFlashColor = new(0.9f, 0.1f, 0.1f, PlayerFlashAlpha);

    private Canvas worldPopupCanvas;
    private Canvas screenFlashCanvas;
    private Image playerFlashImage;

    public void ShowDamageFeedback(int damage, bool targetIsPlayer)
    {
        if (damage <= 0)
            return;

        if (targetIsPlayer)
        {
            ShowPlayerDamageFlash();
            return;
        }

        ShowEnemyDamagePopup(damage);
    }

    public void SetEnemyPopupAnchor(Transform anchor)
    {
        enemyPopupAnchor = anchor;
    }

    public void ShowHealFeedback(int healAmount)
    {
        Debug.Log($"Healed: {healAmount}");
    }

    public void ShowDodgeFeedback()
    {
        Debug.Log("Attack Dodged!");
    }

    public void ShowTurnStartFeedback(bool isPlayerTurn)
    {
        string turnOwner = isPlayerTurn ? "Player's Turn" : "Enemy's Turn";

        if (TurnOwnerText != null)
            TurnOwnerText.text = turnOwner;
    }

    private void ShowEnemyDamagePopup(int damage)
    {
        if (enemyPopupAnchor == null)
        {
            Debug.LogWarning("[Feedback] Enemy popup anchor is missing.");
            return;
        }

        EnsureWorldPopupCanvas();

        GameObject popupObject = new("EnemyDamagePopup", typeof(RectTransform), typeof(TextMeshProUGUI));
        popupObject.transform.SetParent(worldPopupCanvas.transform, false);

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.position = enemyPopupAnchor.position;
        popupRect.localScale = Vector3.one * EnemyPopupStartScale;

        TextMeshProUGUI popupText = popupObject.GetComponent<TextMeshProUGUI>();
        popupText.text = $"-{damage}";
        popupText.color = popupColor;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.fontSize = popupFontSize;
        if (popupFont != null)
            popupText.font = popupFont;

        StartCoroutine(AnimateEnemyPopup(popupRect, popupText));
    }

    private void ShowPlayerDamageFlash()
    {
        EnsureScreenFlashCanvas();
        StartCoroutine(AnimatePlayerFlash());
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

        Destroy(popupRect.gameObject);
    }

    private IEnumerator AnimatePlayerFlash()
    {
        playerFlashImage.enabled = true;
        Color color = playerFlashColor;
        color.a = PlayerFlashAlpha;
        playerFlashImage.color = color;

        float elapsed = 0f;
        while (elapsed < PlayerFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PlayerFlashDuration);
            color.a = Mathf.Lerp(PlayerFlashAlpha, 0f, t);
            playerFlashImage.color = color;
            yield return null;
        }

        playerFlashImage.enabled = false;
    }

    private void EnsureWorldPopupCanvas()
    {
        if (worldPopupCanvas != null)
            return;

        GameObject popupCanvasObject = new("EnemyDamagePopupCanvas", typeof(Canvas));
        popupCanvasObject.transform.SetParent(transform, false);

        worldPopupCanvas = popupCanvasObject.GetComponent<Canvas>();
        worldPopupCanvas.renderMode = RenderMode.WorldSpace;
        worldPopupCanvas.worldCamera = Camera.main;
        worldPopupCanvas.transform.localScale = Vector3.one * 0.01f;
    }

    private void EnsureScreenFlashCanvas()
    {
        if (screenFlashCanvas != null)
            return;

        GameObject flashCanvasObject = new("PlayerDamageFlashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        flashCanvasObject.transform.SetParent(transform, false);

        screenFlashCanvas = flashCanvasObject.GetComponent<Canvas>();
        screenFlashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenFlashCanvas.sortingOrder = short.MaxValue;

        GameObject flashImageObject = new("PlayerDamageFlash", typeof(RectTransform), typeof(Image));
        flashImageObject.transform.SetParent(screenFlashCanvas.transform, false);

        RectTransform rect = flashImageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        playerFlashImage = flashImageObject.GetComponent<Image>();
        playerFlashImage.color = new Color(playerFlashColor.r, playerFlashColor.g, playerFlashColor.b, 0f);
        playerFlashImage.raycastTarget = false;
        playerFlashImage.enabled = false;
    }
}
