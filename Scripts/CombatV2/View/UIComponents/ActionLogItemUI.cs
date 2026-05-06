using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionLogItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;

    public void Bind(Sprite sprite, string logText, Color textColor)
    {
        if (icon != null)
        {
            icon.enabled = sprite != null;
            icon.sprite = sprite;
        }

        if (text != null)
        {
            text.text = logText;
            text.color = textColor;
        }
    }
}