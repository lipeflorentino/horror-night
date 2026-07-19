using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
public class StatRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;
 
    public void Bind(Sprite icon, string label, string value, string deltaText = "", bool showDelta = false, bool positiveDelta = true)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
 
        if (labelText != null)
            labelText.text = label;
 
        if (valueText != null)
        {
            if (showDelta && !string.IsNullOrEmpty(deltaText))
            {
                string color = positiveDelta ? "#2ECC71" : "#E74C3C";
                valueText.text = $"{value} <color={color}>({deltaText})</color>";
            }
            else
            {
                valueText.text = value;
            }
        }
    }
}