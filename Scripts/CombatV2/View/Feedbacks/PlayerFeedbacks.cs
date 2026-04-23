using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFeedbacks : MonoBehaviour
{
    public Canvas screenFlashCanvas;
    public Image playerFlashImage;
    [SerializeField] private TextMeshProUGUI playerStatusText;
    private const float PlayerFlashDuration = 0.15f;
    private const float PlayerFlashAlpha = 0.45f;
    private const float PlayerStatusDuration = 0.6f;

    [Header("Player Damage Flash")]
    [SerializeField] private Color playerFlashColor = new(0.9f, 0.1f, 0.1f, PlayerFlashAlpha);

    void Start()
    {
        if (screenFlashCanvas == null || playerFlashImage == null)
        {
            Debug.LogError("PlayerFeedbacks: Screen flash canvas or image reference is missing.");
            return;
        }

        playerStatusText.gameObject.SetActive(false);
    }

    public void ShowPlayerDamageFlash()
    {
        Debug.Log("[Feedback] Player damage flash triggered.");
        StartCoroutine(AnimatePlayerFlash());
    }

    public void ShowStatusText(string text)
    {
        playerStatusText.gameObject.SetActive(true);
        if (playerStatusText == null)
        {
            Debug.Log($"[Feedback] {text}");
            return;
        }

        StopCoroutine(nameof(AnimatePlayerStatusText));
        StartCoroutine(AnimatePlayerStatusText(text));
    }

    private IEnumerator AnimatePlayerFlash()
    {
        playerFlashImage.gameObject.SetActive(true);
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

    private IEnumerator AnimatePlayerStatusText(string text)
    {
        playerStatusText.text = text;
        playerStatusText.gameObject.SetActive(true);
        Color color = playerStatusText.color;
        color.a = 1f;
        playerStatusText.color = color;

        float elapsed = 0f;
        while (elapsed < PlayerStatusDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PlayerStatusDuration);
            color.a = Mathf.Lerp(1f, 0f, t);
            playerStatusText.color = color;
            yield return null;
        }

        playerStatusText.gameObject.SetActive(false);
    }
}
