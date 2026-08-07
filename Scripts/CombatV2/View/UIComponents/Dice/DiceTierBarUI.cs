using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceTierBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform lowRect;
    [SerializeField] private RectTransform mediumRect;
    [SerializeField] private RectTransform highRect;

    [SerializeField] private TMP_Text lowLabel;
    [SerializeField] private TMP_Text mediumLabel;
    [SerializeField] private TMP_Text highLabel;
    [SerializeField] private GameObject rollIndicator;
    private readonly float barWidth = 240;

    private void Awake()
    {
        SetVisible(false);
        SetIndicatorVisible(false);
    }

    public void SetBoundaries(int lowMax, int mediumMax, int highMin, int maxValue)
    {
        SetVisible(lowMax > 0 || mediumMax > 0 || highMin > 0 || maxValue > 0);

        float safeMaxValue = Mathf.Max(1f, maxValue);

        float lowPct    = Mathf.Max(0f, lowMax) / safeMaxValue;
        float mediumPct = Mathf.Max(0f, mediumMax - lowMax) / safeMaxValue;
        float highPct = highMin > 0 ? Mathf.Max(0f, safeMaxValue - (highMin - 1)) / safeMaxValue : 0f;

        float minPixelWidth = 4f; 
        float lowPixels    = Mathf.Max(minPixelWidth, lowPct * barWidth);
        float mediumPixels = Mathf.Max(minPixelWidth, mediumPct * barWidth);
        float highPixels   = Mathf.Max(minPixelWidth, highPct * barWidth);

        float totalPixels = lowPixels + mediumPixels + highPixels;

        float lowEnd    = lowPixels / totalPixels;
        float mediumEnd = (lowPixels + mediumPixels) / totalPixels;

        SetAnchorX(lowRect,    0f,        lowEnd);
        SetAnchorX(mediumRect, lowEnd,    mediumEnd);
        SetAnchorX(highRect,   mediumEnd, 1f);

        if (lowLabel != null) 
            lowLabel.text = lowMax > 0 ? $"L (1-{lowMax})" : "L (-)";
            
        if (mediumLabel != null) 
            mediumLabel.text = mediumMax > lowMax ? $"M ({lowMax + 1}-{mediumMax})" : "M (-)";
            
        if (highLabel != null) 
            highLabel.text = highMin > 0 && highMin <= maxValue ? $"H ({highMin}-{maxValue})" : "H (-)";
    }

    private static void SetAnchorX(RectTransform rect, float xMin, float xMax)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(xMin, rect.anchorMin.y);
        rect.anchorMax = new Vector2(xMax, rect.anchorMax.y);
        rect.offsetMin = new Vector2(0f,   rect.offsetMin.y); 
        rect.offsetMax = new Vector2(0f,   rect.offsetMax.y);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible) ResetHighlight();
    }

    public void SetIndicatorVisible(bool visible)
    {
        if (rollIndicator != null) rollIndicator.SetActive(visible);
    }

    public void SetRollIndicatorPosition(float rollValue, int maxValue)
    {
        if (rollIndicator == null) return;
        SetIndicatorVisible(true);

        float safeMaxValue = Mathf.Max(1f, maxValue);
        float rollPct = Mathf.Clamp01(rollValue / safeMaxValue);

        RectTransform indicatorRect = rollIndicator.GetComponent<RectTransform>();

        indicatorRect.anchorMin = new Vector2(rollPct, indicatorRect.anchorMin.y);
        indicatorRect.anchorMax = new Vector2(rollPct, indicatorRect.anchorMax.y);
        indicatorRect.anchoredPosition = new Vector2(0f, indicatorRect.anchoredPosition.y);
    }

    public void SetHighlightTier(DiceTier tier)
    {
        SetTierAlpha(lowRect, lowLabel, tier == DiceTier.Low);
        SetTierAlpha(mediumRect, mediumLabel, tier == DiceTier.Medium);
        SetTierAlpha(highRect, highLabel, tier == DiceTier.High);
    }

    public void ResetHighlight()
    {
        SetTierAlpha(lowRect, lowLabel, true);
        SetTierAlpha(mediumRect, mediumLabel, true);
        SetTierAlpha(highRect, highLabel, true);
    }

    // Método auxiliar para aplicar a transparência
    private void SetTierAlpha(RectTransform rect, TMP_Text label, bool isHighlighted)
    {
        float targetAlpha = isHighlighted ? 1f : 0.15f;
        if (rect != null && rect.TryGetComponent<Image>(out var image))
        {
            Color color = image.color;
            color.a = targetAlpha;
            image.color = color;
        }

        if (label != null)
        {
            Color color = label.color;
            color.a = targetAlpha;
            label.color = color;
        }
    }
}