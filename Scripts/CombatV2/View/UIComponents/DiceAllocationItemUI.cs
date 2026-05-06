using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceAllocationItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text facesText;

    public void Bind(Sprite icon, int faces)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (facesText != null)
            facesText.text = $"d{Mathf.Max(1, faces)}";
    }
}
