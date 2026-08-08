using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickRequirementUI : MonoBehaviour
{
    [Header("Requirement Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    private Tooltipable tooltipable;

    void Awake()
    {
        tooltipable = gameObject.GetOrAddComponent<Tooltipable>();
    }

    public void Setup(string requirementKey, int value)
    {
        if (iconImage != null) iconImage.sprite = IconProvider.GetRequirementIcon(requirementKey);
        if (countText != null) countText.text = $"{value}";
        if (tooltipable != null) tooltipable.SetTooltipText(requirementKey);
    }
}