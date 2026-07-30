using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Popup Settings")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text durationText;

    [Header("Tooltip Settings")]
    [SerializeField] private Image tooltipIcon;
    [SerializeField] private TMP_Text tooltipDurationText;   
    [SerializeField] private TMP_Text tooltipDescriptionText;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private GameObject tooltip;

    [Header("Animation Settings")]
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private float exitDuration = 0.25f;
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Header("Pulse / Punch Settings")]
    [Tooltip("Força do pulso ao aparecer/desaparecer (ex: 0.3 = infla 30% antes de estabilizar)")]
    [SerializeField] private Vector3 punchScale = new Vector3(0.35f, 0.35f, 0.35f);
    [SerializeField] private int punchVibrato = 5;
    [SerializeField] private float punchElasticity = 0.5f;

    [Header("Blink Settings")]
    [Tooltip("Habilita o efeito de piscar/flash durante o aparecimento")]
    [SerializeField] private bool enableBlinkOnEnter = true;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private CanvasGroup canvasGroup; // Recomendado para piscar o elemento inteiro

    private void Awake()
    {
        // Garante que exista um CanvasGroup no objeto para controlar o Alpha/Piscar
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null && enableBlinkOnEnter)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Configura o ícone com os dados necessários para exibir o efeito de status.
    /// </summary>
    public void Setup(Sprite iconSprite, int remainingTurns, string description, string displayName)
    {
        if (icon != null)
            icon.sprite = iconSprite;

        if (tooltipIcon != null)
            tooltipIcon.sprite = iconSprite;

        if (durationText != null)
        {
            durationText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;
        }

        if (tooltipDurationText != null)
        {
            tooltipDurationText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;
        }

        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = description;

        if (tooltipNameText != null)
            tooltipNameText.text = displayName;
    }

    public void RefreshDuration(int remainingTurns)
    {
        if (durationText != null)
        {
            durationText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;
        }

        if (tooltipDurationText != null)
        {
            tooltipDurationText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;
        }
    }

    /// <summary>
    /// Entrada: Cresce rapidamente com pulso elástico (DOPunchScale) e efeito de piscar (Flash).
    /// </summary>
    public void PlayEnterAnimation()
    {
        // Cancela animações anteriores no Transform e CanvasGroup
        transform.DOKill(); 
        if (canvasGroup != null) canvasGroup.DOKill();

        // Estado Inicial
        transform.localScale = Vector3.zero;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        // Criamos uma sequência para coordenar o aparecimento, pulso e piscar
        Sequence enterSeq = DOTween.Sequence();

        // 1. Aparição com Scale até o tamanho alvo
        enterSeq.Join(transform.DOScale(targetScale, enterDuration).SetEase(Ease.OutBack));

        // 2. Fade in do CanvasGroup
        if (canvasGroup != null)
            enterSeq.Join(canvasGroup.DOFade(1f, enterDuration * 0.5f));

        // 3. Efeito Pulsante (Impacto que infla e oscila até o tamanho normal)
        enterSeq.Append(transform.DOPunchScale(punchScale, enterDuration, punchVibrato, punchElasticity));

        // 4. Efeito de Piscar (Flash) no CanvasGroup
        if (enableBlinkOnEnter && canvasGroup != null)
        {
            // Apaga e acende 'blinkCount' vezes rapidamente
            Sequence blinkSeq = DOTween.Sequence();
            blinkSeq.Append(canvasGroup.DOFade(0.2f, 0.06f));
            blinkSeq.Append(canvasGroup.DOFade(1f, 0.06f));
            blinkSeq.SetLoops(blinkCount, LoopType.Restart);

            enterSeq.Join(blinkSeq);
        }
    }

    /// <summary>
    /// Saída: Pulsa brevemente para fora antes de encolher totalmente a zero.
    /// </summary>
    public void PlayExitAnimation()
    {
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();

        Sequence exitSeq = DOTween.Sequence();

        // 1. Um pequeno pulso rápido de aviso antes de sumir
        exitSeq.Append(transform.DOPunchScale(punchScale * 0.5f, exitDuration * 0.5f, 3, 0.5f));

        // 2. Encolhe até zero e esvanece o alpha
        exitSeq.Append(transform.DOScale(Vector3.zero, exitDuration).SetEase(Ease.InBack));

        if (canvasGroup != null)
            exitSeq.Join(canvasGroup.DOFade(0f, exitDuration));
    }

    private void OnDestroy()
    {
        // Prevenção de memory leak ao destruir o GameObject durante uma animação
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
    }

    public void OnPointerEnter(PointerEventData eventData) => ShowTooltip();
    public void OnPointerExit(PointerEventData eventData) => HideTooltip();
    private void OnMouseEnter() => ShowTooltip();
    private void OnMouseExit() => HideTooltip();

    private void ShowTooltip()
    {
        if (tooltip == null) return;
        tooltip.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltip != null) return;
        tooltip.SetActive(false);
    }
}