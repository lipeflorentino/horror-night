using DG.Tweening;
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
    [Header("Resource Cost Popup")]
    [SerializeField] private ResourceCostPopupUI resourceCostPopupPrefab;
    [SerializeField] private CombatHudBinding hudBinding;

    private Tween flashTween;
    private Tween statusLogTween;

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
        AnimatePlayerFlash();
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

        AnimateActionLog(text, isAttackFeedback);
    }

    public void ShowDamagePopup(int damage)
    {
        if (damage <= 0)
            return;

        RectTransform anchor = GetHpAnchor();

        if (screenFlashCanvas == null || anchor == null || resourceCostPopupPrefab == null)
        {
            Logger.Log("[PlayerFeedbacks] Screen flash canvas or image reference is missing.");
            return;
        }

        var popup = Instantiate(resourceCostPopupPrefab, screenFlashCanvas.transform);
        popup.transform.position = anchor.position;
        Color color = ColorUtility.TryParseHtmlString(Colorization.BadColorHex, out Color c) ? c : Color.white;
        popup.Show($"-{damage}", color);
    }

    public void ShowResourceCostPopup(DiceStatType statType, int amount)
    {
        RectTransform anchor = GetStatIconAnchor(statType);
        if (resourceCostPopupPrefab == null || anchor == null || screenFlashCanvas == null)
        {
            Logger.Log("[PlayerFeedbacks] Cannot show resource cost popup, references missing.");
            return;
        }

        var popup = Instantiate(resourceCostPopupPrefab, screenFlashCanvas.transform);
        popup.transform.position = anchor.position;
        Color color = ColorUtility.TryParseHtmlString(Colorization.BadColorHex, out Color c) ? c : Color.white;
        popup.Show($"-{amount}", color);
        Image hightlightImage = GetStatHightlightImage(statType);
        AnimateHighlight(hightlightImage);
    }

    private RectTransform GetHpAnchor() => hudBinding?.hp?.valueText?.rectTransform;

    private RectTransform GetStatIconAnchor(DiceStatType statType) => statType switch
    {
        DiceStatType.Mind => hudBinding?.mind?.icon?.rectTransform,
        DiceStatType.Heart => hudBinding?.heart?.icon?.rectTransform,
        DiceStatType.Body => hudBinding?.body?.icon?.rectTransform,
        _ => null
    };

    private Image GetStatHightlightImage(DiceStatType statType) => statType switch
    {
        DiceStatType.Mind => hudBinding?.mind?.highlight,
        DiceStatType.Heart => hudBinding?.heart?.highlight,
        DiceStatType.Body => hudBinding?.body?.highlight,
        _ => null
    };

    private void AnimateHighlight(Image highlightImage)
    {
        if (highlightImage == null)
            return;
            
        highlightImage.DOKill();

        Color originalColor = highlightImage.color;
        Color targetColor = new(1f, 0f, 0f, 0.5f);
        Sequence highlightSeq = DOTween.Sequence();
        
        highlightSeq.Append(highlightImage.DOColor(targetColor, 0.2f))
                    .SetLoops(2, LoopType.Yoyo) 
                    .OnComplete(() => highlightImage.color = originalColor);
    }

    private void AnimatePlayerFlash()
    {
        if (playerFlashImage == null)
            return;

        flashTween?.Kill();

        playerFlashImage.gameObject.SetActive(true);
        playerFlashImage.enabled = true;

        Color color = playerFlashImage.color;
        color.a = PlayerFlashAlpha;
        playerFlashImage.color = color;

        flashTween = playerFlashImage
            .DOFade(0f, PlayerFlashDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                playerFlashImage.enabled = false;
                playerFlashImage.gameObject.SetActive(false);
            });
    }

    private void AnimateActionLog(string text, bool isAttackFeedback)
    {
        Color textColor = isAttackFeedback ? attackFeedbackColor : defenseFeedbackColor;

        playerStatusText.color = textColor;
        playerStatusText.text = text;

        statusLogTween?.Kill();
        statusLogTween = DOVirtual.DelayedCall(PlayerStatusDuration, () => actionLogPanel.SetActive(false));
    }
}