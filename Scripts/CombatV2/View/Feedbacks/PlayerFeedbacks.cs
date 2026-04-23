using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFeedbacks : MonoBehaviour
{
    public Canvas screenFlashCanvas;
    public Image playerFlashImage;
    private const float PlayerFlashDuration = 0.15f;
    private const float PlayerFlashAlpha = 0.45f;

    [Header("Player Damage Flash")]
    [SerializeField] private Color playerFlashColor = new(0.9f, 0.1f, 0.1f, PlayerFlashAlpha);

    void Start()
    {
        if (screenFlashCanvas == null || playerFlashImage == null)
        {
            Debug.LogError("PlayerFeedbacks: Screen flash canvas or image reference is missing.");
            return;
        }
    }

    public void ShowPlayerDamageFlash()
    {
        Debug.Log("[Feedback] Player damage flash triggered.");
        StartCoroutine(AnimatePlayerFlash());
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
}