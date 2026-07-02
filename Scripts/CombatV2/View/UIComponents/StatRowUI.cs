using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
public class StatRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;
 
    public void Bind(Sprite icon, string label, string value)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
 
        if (labelText != null)
            labelText.text = label;
 
        if (valueText != null)
            valueText.text = value;
    }
}