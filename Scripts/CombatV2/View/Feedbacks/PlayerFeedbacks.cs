using System.Collections;
using System.Text.RegularExpressions;
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

    [Header("Feedback Text Colors")]
    [SerializeField] private Color attackFeedbackColor = new(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color defenseFeedbackColor = new(0.1f, 0.1f, 1f, 1f);
    void Start()
    {
        if (screenFlashCanvas == null || playerFlashImage == null)
        {
            Logger.Log("[PlayerFeedbacks] Screen flash canvas or image reference is missing.");
            return;
        }

        if (actionLogPanel == null)
        {
            Logger.Log("[PlayerFeedbacks] Action log panel reference is missing.");
            return;
        }

        actionLogPanel.SetActive(false);
    }

    public void ShowPlayerDamageFlash()
    {
        Logger.Log("[Feedback] Player damage flash triggered.");
        StartCoroutine(AnimatePlayerFlash());
    }

    public void ShowStatusText(string text, bool isAttackFeedback)
    {
        if (actionLogPanel == null)
        {
            Logger.Log("[PlayerFeedbacks] Action log panel reference is missing.");
            return;
        }

        actionLogPanel.SetActive(true);

        if (playerStatusText == null)
        {
            Logger.Log($"[Feedback] {text}");
            return;
        }

        StartCoroutine(AnimateActionLog(text, isAttackFeedback));
    }

    private IEnumerator AnimatePlayerFlash()
    {
        playerFlashImage.gameObject.SetActive(true);
        playerFlashImage.enabled = true;

        yield return FadeUtility.FadeImageAlpha(playerFlashImage, PlayerFlashAlpha, 0f, PlayerFlashDuration);

        playerFlashImage.enabled = false;
        playerFlashImage.gameObject.SetActive(false);
    }

    private IEnumerator AnimateActionLog(string text, bool isAttackFeedback)
    {
        Logger.Log($"[Feedback] {text}");

        Color textColor = isAttackFeedback ? attackFeedbackColor : defenseFeedbackColor;

        playerStatusText.color = textColor;
        playerStatusText.text = text;
        
        yield return new WaitForSeconds(PlayerStatusDuration);
        actionLogPanel.SetActive(false);
    }
}