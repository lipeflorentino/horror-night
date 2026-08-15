using System.Text.RegularExpressions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyFeedbacks : MonoBehaviour
{
    private const float EnemyPopupDuration = 0.55f;
    private const float EnemyPopupRiseDistance = 0.75f;
    private const float EnemyPopupStartScale = 0.7f;
    private const float EnemyPopupBounceScale = 1.2f;
    private const float EnemyFlashDuration = 0.15f;
    private const float EnemyFlashAlpha = 0.9f;
    private Color flashColor = new(0.9f, 0.1f, 0.1f, EnemyFlashAlpha);
    [SerializeField] private float EnemyStatusDuration = 2f;
    
    [Header("Enemy Damage Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private Canvas worldPopupCanvas;
    [SerializeField] private GameObject enemyVisual;
    [SerializeField] private Color damageColor = new(1f, 0.1f, 0.1f, 1f);

    private RectTransform popupRect;
    private TextMeshProUGUI popupText;
    private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private GameObject actionLogPanel;
    [SerializeField] private Image actionIcon;
    [SerializeField] private TMP_Text enemyStatusText;

    [Header("Feedback Text Colors")]
    [SerializeField] private Color attackFeedbackColor = new(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color defenseFeedbackColor = new(0.1f, 0.1f, 1f, 1f);

    private Sequence popupSequence;
    private Tween flashTween;
    private Tween statusLogTween;

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

        if (actionLogPanel == null)
        {
            Debug.LogError("[EnemyFeedbacks] Action log panel reference is missing.");
            return;
        }

        if (actionIcon == null)
            actionIcon = actionLogPanel.GetComponentInChildren<Image>(true);

        if (actionIcon != null)
        {
            actionIcon.enabled = false;
            actionIcon.gameObject.SetActive(false);
        }

        popupObject.SetActive(false);
        actionLogPanel.SetActive(false);
    }

    public void ShowDamagePopup(int damage)
    {
        ShowPopupText($"-{damage}", damageColor);
        AnimateEnemyFlash();
    }

    public void ShowStatusPopup(string text, bool isAttackFeedback)
    {
        if (actionLogPanel == null)
        {
            Debug.LogError("[EnemyFeedbacks] Action log panel reference is missing.");
            return;
        }

        actionLogPanel.SetActive(true);
        UpdateActionIcon(isAttackFeedback);
        AnimateActionLog(text, isAttackFeedback);
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

        AnimateEnemyPopup();
    }

    private void AnimateEnemyPopup()
    {
        popupSequence?.Kill();

        Vector3 startPosition = popupRect.position;
        Vector3 endPosition = startPosition + Vector3.up * EnemyPopupRiseDistance;

        popupRect.position = startPosition;
        popupRect.localScale = Vector3.one * EnemyPopupStartScale;

        Color startColor = popupText.color;
        startColor.a = 1f;
        popupText.color = startColor;

        popupSequence = DOTween.Sequence();
        popupSequence.Join(popupRect.DOMove(endPosition, EnemyPopupDuration).SetEase(Ease.Linear));
        popupSequence.Join(popupRect.DOScale(EnemyPopupBounceScale, EnemyPopupDuration * 0.5f).SetEase(Ease.OutSine).SetLoops(2, LoopType.Yoyo));
        popupSequence.Join(popupText.DOFade(0f, EnemyPopupDuration).SetEase(Ease.Linear));
        popupSequence.OnComplete(() =>
        {
            popupObject.SetActive(false);
            popupRect.position = startPosition;
            popupRect.localScale = Vector3.one * EnemyPopupStartScale;
        });
    }

    private void AnimateEnemyFlash()
    {
        if (enemySpriteRenderer == null)
        {
            Debug.LogError("[EnemyFeedbacks] Cannot animate flash, SpriteRenderer reference is missing.");
            return;
        }

        flashTween?.Kill();

        Color initialColor = enemySpriteRenderer.color;
        enemySpriteRenderer.color = flashColor;

        flashTween = enemySpriteRenderer
            .DOFade(1f, EnemyFlashDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => enemySpriteRenderer.color = initialColor);
    }

    private void UpdateActionIcon(bool isAttackFeedback)
    {
        if (actionLogPanel == null)
            return;

        if (actionIcon == null)
            actionIcon = actionLogPanel.GetComponentInChildren<Image>(true);

        if (actionIcon == null)
            return;

        string actionName = isAttackFeedback ? "attack" : "defense";
        Texture2D actionTexture = Resources.Load<Texture2D>($"UI/Actions/{actionName}");

        if (actionTexture == null)
        {
            actionIcon.enabled = false;
            actionIcon.gameObject.SetActive(false);
            return;
        }

        actionIcon.sprite = Sprite.Create(actionTexture, new Rect(0f, 0f, actionTexture.width, actionTexture.height), new Vector2(0.5f, 0.5f));
        actionIcon.enabled = true;
        actionIcon.gameObject.SetActive(true);
    }

    private void AnimateActionLog(string text, bool isAttackFeedback)
    {
        Color textColor = isAttackFeedback ? attackFeedbackColor : defenseFeedbackColor;
        enemyStatusText.color = textColor;
        
        if (Regex.IsMatch(text, @"[+-]\d+"))
        {
            int bonusIndex = text.IndexOfAny(new char[] { '+', '-' });
            
            string baseText = text[..bonusIndex];
            enemyStatusText.text = $"{baseText} damage";
        }
        else
        {
            enemyStatusText.text = text;
        }

        statusLogTween?.Kill();
        statusLogTween = DOVirtual.DelayedCall(EnemyStatusDuration, () => actionLogPanel.SetActive(false));
    }
}