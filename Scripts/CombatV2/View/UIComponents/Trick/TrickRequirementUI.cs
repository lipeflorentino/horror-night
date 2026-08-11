using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickRequirementUI : MonoBehaviour
{
    [Header("Requirement Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image highlight;
    private Tooltipable tooltipable;

    void Awake()
    {
        EnsureTooltipable();
    }
    
    private void EnsureTooltipable()
    {
        if (tooltipable == null)
        {
            tooltipable = gameObject.GetOrAddComponent<Tooltipable>();
        }
    }

    public void Setup(string requirementKey, int value, int availableValue)
    {
        EnsureTooltipable(); // Garante que a referência existe antes de usá-la

        if (iconImage != null) iconImage.sprite = IconProvider.GetRequirementIcon(requirementKey);
        if (countText != null) countText.text = $"{value}/{availableValue}";
        if (tooltipable != null) tooltipable.SetTooltipText(requirementKey);
        if (highlight != null) highlight.color = value > availableValue ? Colorization.HexToColor(Colorization.BadColorHex) : Colorization.HexToColor(Colorization.GoodColorHex);
    }
}