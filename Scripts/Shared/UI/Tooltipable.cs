using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltipable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    [SerializeField] private TooltipUI.TooltipColor tooltipColor = TooltipUI.TooltipColor.Default;

    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }

    public void SetTooltipColor(TooltipUI.TooltipColor color)
    {
        tooltipColor = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUI.Instance.Show(tooltipText, transform.position, tooltipColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.Hide();
    }

    public void HideTooltip()
    {
        TooltipUI.Instance.Hide();
    }
}