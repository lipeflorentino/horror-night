using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatLogItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;

    private RectTransform iconRect;
    private RectTransform textRect;
    private float textOffsetMinXWithIcon;
    private bool cached;

    private void Awake()
    {
        CacheRects();
    }

    private void CacheRects()
    {
        if (cached) return;

        if (icon != null) iconRect = icon.rectTransform;
        if (text != null)
        {
            textRect = text.rectTransform;
            textOffsetMinXWithIcon = textRect.offsetMin.x;
        }

        cached = true;
    }

    public void Bind(Sprite sprite, string logText, Color textColor)
    {
        if (!cached) CacheRects();

        if (text != null)
        {
            text.text = logText;
            text.color = textColor;
        }

        bool hasIcon = sprite != null;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(hasIcon);
        }

        if (textRect != null)
        {
            Vector2 offsetMin = textRect.offsetMin;
            offsetMin.x = hasIcon || iconRect == null ? textOffsetMinXWithIcon : iconRect.offsetMin.x;
            textRect.offsetMin = offsetMin;
        }
    }
}