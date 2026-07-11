using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatLogItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;

    public void Bind(Sprite sprite, string logText, Color textColor)
    {
        if (text != null)
        {
            text.text = logText;
            text.color = textColor;
        }

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }
    }
}