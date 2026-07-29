using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    public enum TooltipColor
    {
        Default,
        Red,
        Yellow,
        Blue
    }

    private static readonly Color DefaultColor = ParseHex("#C3C099");
    private static readonly Color RedColor = ParseHex("#D65A5A");
    private static readonly Color YellowColor = ParseHex("#D6C15A");
    private static readonly Color BlueColor = ParseHex("#5AA9D6");

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Size")]
    [SerializeField] private float maxWidth = 500f;
    [SerializeField] private Vector2 padding = new(24f, 16f);

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.15f;

    private RectTransform rectTransform;
    private RectTransform panelRectTransform;
    private CanvasGroup canvasGroup;
    private Tween fadeTween;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        rectTransform = GetComponent<RectTransform>();

        if (tooltipPanel != null)
        {
            panelRectTransform = tooltipPanel.GetComponent<RectTransform>();

            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (tooltipText != null)
            tooltipText.enableWordWrapping = true;

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        fadeTween?.Kill();
    }

    public void Show(string text, Vector3 position, TooltipColor color = TooltipColor.Default)
    {
        if (tooltipText != null)
        {
            tooltipText.text = text;
            tooltipText.color = GetColor(color);
        }

        ResizeToText(text);

        if (rectTransform != null)
            rectTransform.position = position + new Vector3(0, 50, 0);

        if (tooltipPanel == null)
            return;

        tooltipPanel.SetActive(true);

        fadeTween?.Kill();
        if (canvasGroup != null)
            fadeTween = canvasGroup.DOFade(1f, fadeDuration);
    }

    public void Hide()
    {
        if (tooltipPanel == null)
            return;

        fadeTween?.Kill();

        if (canvasGroup == null)
        {
            tooltipPanel.SetActive(false);
            return;
        }

        fadeTween = canvasGroup.DOFade(0f, fadeDuration)
            .OnComplete(() => tooltipPanel.SetActive(false));
    }

    private void HideImmediate()
    {
        fadeTween?.Kill();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void ResizeToText(string text)
    {
        if (tooltipText == null)
            return;

        Vector2 preferredSize = tooltipText.GetPreferredValues(text, maxWidth, 0f);
        float width = Mathf.Min(preferredSize.x, maxWidth) + padding.x;
        float height = preferredSize.y + padding.y;

        if (panelRectTransform != null)
            panelRectTransform.sizeDelta = new Vector2(width, height);
        else if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static Color GetColor(TooltipColor color)
    {
        return color switch
        {
            TooltipColor.Red => RedColor,
            TooltipColor.Yellow => YellowColor,
            TooltipColor.Blue => BlueColor,
            _ => DefaultColor
        };
    }

    private static Color ParseHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}