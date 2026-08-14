using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerkSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image perkIconImage;
    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPrefab;
    private PerkTooltip tooltip;
    private PerkRuntimeInstance runtimeInstance;

    public void Bind(PerkRuntimeInstance instance)
    {
        Logger.Log($"[PerkSlotUI] Binding perk to slot: {instance.Definition.Id ?? "null"}");
        if (perkIconImage != null)
        {
            if (instance.SourceTrick != null && instance.SourceTrick.Definition != null)
                perkIconImage.sprite = instance.SourceTrick.Definition.Icon;
            else if (instance.SourceDrawback != null && instance.SourceDrawback.Definition != null)
                perkIconImage.sprite = instance.SourceDrawback.Definition.Icon;
            else if (instance.SourceState != null && instance.SourceState.Definition != null)
                perkIconImage.sprite = instance.SourceState.Definition.Icon;
        }
        runtimeInstance = instance;
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
        if (tooltip != null || tooltipPrefab == null || runtimeInstance == null)
            return;

        tooltip = Instantiate(tooltipPrefab, transform).GetComponent<PerkTooltip>();
        if (tooltip != null)
            tooltip.Show(runtimeInstance);
    }

    private void HideTooltip()
    {
        if (tooltip != null)
            Destroy(tooltip.gameObject);
    }
}