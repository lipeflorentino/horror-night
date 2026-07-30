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
    [SerializeField] private float enterDuration = 0.2f;
    [SerializeField] private float exitDuration = 0.15f;
    [SerializeField] private Vector3 enterScale = new(1f, 1f, 1f);
    [SerializeField] private Vector3 exitScale = new(0.9f, 0.9f, 0.9f);

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

    public void PlayEnterAnimation()
    {
        transform.DOKill(); 
        transform.localScale = Vector3.zero;
        transform.DOScale(enterScale, enterDuration).SetEase(Ease.OutBack);
    }

    public void PlayExitAnimation()
    {
        transform.DOKill();
        transform.DOScale(exitScale, exitDuration).SetEase(Ease.InBack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnMouseEnter()
    {
        ShowTooltip();
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (tooltip == null)
            return;

        tooltip.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltip != null)
            tooltip.SetActive(false);
    }
}
