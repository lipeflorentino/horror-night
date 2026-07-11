using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text durationText;   
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text nameText;
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

        if (durationText != null)
            durationText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;

        if (descriptionText != null)
            descriptionText.text = description;

        if (nameText != null)
            nameText.text = displayName;
    }

    public void PlayEnterAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale(Vector3.zero, enterScale, enterDuration));
    }

    public void PlayExitAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale(transform.localScale, exitScale, exitDuration));
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

    private IEnumerator AnimateScale(Vector3 fromScale, Vector3 toScale, float duration)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            yield break;

        rectTransform.localScale = fromScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            rectTransform.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        rectTransform.localScale = toScale;
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
