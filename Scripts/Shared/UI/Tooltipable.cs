using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltipable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    [SerializeField] private TooltipUI.TooltipColor tooltipColor = TooltipUI.TooltipColor.Default;
    [SerializeField] private bool isDisabled = false;

    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }

    public void SetTooltipColor(TooltipUI.TooltipColor color, GameObject owner = null)
    {
        tooltipColor = color;
    }

    public void DisableTooltip(bool disabled)
    {
        isDisabled = disabled;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDisabled)
            TooltipUI.Instance.Show(tooltipText, transform.position, tooltipColor, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.Hide(this);
    }

    public void HideTooltip()
    {
        TooltipUI.Instance.Hide(this);
    }
}