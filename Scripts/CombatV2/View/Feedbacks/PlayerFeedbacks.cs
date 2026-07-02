using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFeedbacks : MonoBehaviour
{
    public Canvas screenFlashCanvas;
    public Image playerFlashImage;
    [SerializeField] private GameObject actionLogPanel;
    [SerializeField] private TextMeshProUGUI playerStatusText;
    [SerializeField] private float PlayerStatusDuration = 2f;
    
    [Header("Player Damage Flash")]
    const float PlayerFlashAlpha = 0.45f;
    [SerializeField] private Color playerFlashColor = new(0.9f, 0.1f, 0.1f, PlayerFlashAlpha);
    [SerializeField] private float PlayerFlashDuration = 0.15f;

    void Start()
    {
        if (screenFlashCanvas == null || playerFlashImage == null)
        {
            Debug.LogError("PlayerFeedbacks: Screen flash canvas or image reference is missing.");
            return;
        }

        actionLogPanel.SetActive(false);
    }

    public void ShowPlayerDamageFlash()
    {
        Debug.Log("[Feedback] Player damage flash triggered.");
        StartCoroutine(AnimatePlayerFlash());
    }

    public void ShowStatusText(string text)
    {
        actionLogPanel.SetActive(true);

        if (playerStatusText == null)
        {
            Debug.Log($"[Feedback] {text}");
            return;
        }
        
        StartCoroutine(AnimateActionLog(text));
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
        playerFlashImage.gameObject.SetActive(false);
    }

    private IEnumerator AnimateActionLog(string text)
    {
        playerStatusText.text = text;
        yield return new WaitForSeconds(PlayerStatusDuration);
        actionLogPanel.SetActive(false);
    }

    private IEnumerator AnimatePlayerStatusText(string text)
    {
        playerStatusText.text = text;
        actionLogPanel.SetActive(true);
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

        actionLogPanel.SetActive(false);
    }
}
