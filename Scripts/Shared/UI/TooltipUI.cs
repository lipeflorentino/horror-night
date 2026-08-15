using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }
    public enum TooltipColor { Default, Red, Yellow, Blue }
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
    private object currentOwner;

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

    public void Show(string text, Vector3 position, TooltipColor color = TooltipColor.Default, object owner = null)
    {
        currentOwner = owner;
        ResetToDefaults();
        
        if (tooltipText != null)
        {
            tooltipText.text = text;
            tooltipText.color = GetColor(color);
        }
        
        ResizeToText(text);

        if (rectTransform != null)
        {
            Vector3 targetPosition = position + new Vector3(0, 50, 0);
            rectTransform.position = GetScreenClampedPosition(targetPosition);
        }

        if (tooltipPanel == null)
            return;

        tooltipPanel.SetActive(true);

        fadeTween?.Kill();
        if (canvasGroup != null)
            fadeTween = canvasGroup.DOFade(1f, fadeDuration);
    }

    public void Hide(object owner = null)
    {
        if (owner != null && currentOwner != owner)
            return;

        currentOwner = null;

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

    private void ResetToDefaults()
    {
        // Garante a cor padrão usando o método que você já criou
        if (tooltipText != null)
        {
            tooltipText.color = GetColor(TooltipColor.Default);
            
            // Futuras expansões de estado padrão entram aqui. Exemplos:
            // tooltipText.fontSize = 18f;
            // tooltipText.alignment = TextAlignmentOptions.TopLeft;
            // tooltipIcon.gameObject.SetActive(false);
        }
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

    private Vector3 GetScreenClampedPosition(Vector3 desiredPosition)
    {
        // Usa o tamanho do painel (calculado no ResizeToText)
        Vector2 size = panelRectTransform != null ? panelRectTransform.sizeDelta : rectTransform.sizeDelta;
        Vector2 pivot = rectTransform.pivot;

        // Calcula os limites virtuais do tooltip na tela
        float minX = desiredPosition.x - (size.x * pivot.x);
        float maxX = desiredPosition.x + (size.x * (1 - pivot.x));
        float minY = desiredPosition.y - (size.y * pivot.y);
        float maxY = desiredPosition.y + (size.y * (1 - pivot.y));

        // Ajusta a posição no eixo X se estiver saindo pelas laterais
        if (minX < 0) 
            desiredPosition.x += -minX;
        else if (maxX > Screen.width) 
            desiredPosition.x -= (maxX - Screen.width);

        // Ajusta a posição no eixo Y se estiver saindo por cima ou por baixo
        if (minY < 0) 
            desiredPosition.y += -minY;
        else if (maxY > Screen.height) 
            desiredPosition.y -= (maxY - Screen.height);

        return desiredPosition;
    }

    public object Owner => currentOwner;
}