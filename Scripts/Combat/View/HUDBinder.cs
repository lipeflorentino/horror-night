
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StatHudBinding
{
    public Image fillImage;
    public TMP_Text valueText;

    public void SetValue(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(current).ToString();
    }
}

[Serializable]
public class StatBarBinding
{
    public Image fillImage;
    public TMP_Text valueText;

    public void SetValue(int current, int max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max <= 0 ? 0f : Mathf.Clamp01(current / (float)max);

        if (valueText != null)
            valueText.text = current.ToString();
    }
}

[Serializable]
public class CombatHudBinding
{
    public StatBarBinding heart;
    public StatBarBinding body;
    public StatBarBinding mind;

    public void SetValues(int heartValue, int heartMax, int bodyValue, int bodyMax, int mindValue, int mindMax)
    {
        heart?.SetValue(heartValue, heartMax);
        body?.SetValue(bodyValue, bodyMax);
        mind?.SetValue(mindValue, mindMax);
    }
}