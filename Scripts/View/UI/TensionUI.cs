using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TensionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image tensionFill;

    [Header("Colors")]
    [SerializeField] private Color lowColor = Color.blue; // Azul (low)
    [SerializeField] private Color highColor = new(0.5f, 0f, 1f); // Roxo (high)

    public void RefreshUI(int currentTension, int threshold)
    {
        float normalized = Mathf.Clamp01(currentTension / (float)threshold);

        if (tensionFill != null)
        {
            tensionFill.fillAmount = normalized;
            tensionFill.color = Color.Lerp(lowColor, highColor, normalized);
        }
    }
}