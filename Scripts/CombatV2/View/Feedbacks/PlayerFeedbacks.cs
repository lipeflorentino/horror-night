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
            Debug.LogError("[PlayerFeedbacks] Screen flash canvas or image reference is missing.");
            return;
        }

        if (actionLogPanel == null)
        {
            Debug.LogError("[PlayerFeedbacks] Action log panel reference is missing.");
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
        if (actionLogPanel == null)
        {
            Debug.LogError("[PlayerFeedbacks] Action log panel reference is missing.");
            return;
        }

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

        yield return FadeUtility.FadeImageAlpha(playerFlashImage, PlayerFlashAlpha, 0f, PlayerFlashDuration);

        playerFlashImage.enabled = false;
        playerFlashImage.gameObject.SetActive(false);
    }

    private IEnumerator AnimateActionLog(string text)
    {
        playerStatusText.text = text;
        yield return new WaitForSeconds(PlayerStatusDuration);
        actionLogPanel.SetActive(false);
    }
}