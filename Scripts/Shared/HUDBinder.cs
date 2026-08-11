
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StatHudBinding
{
    public Image icon;
    public Image highlight;
    public Image alert;
    public Image fillImage;
    public TMP_Text valueText;
    private readonly int minThreshold = CombatRules.MinCoreStatValue;

    public void SetValue(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(current).ToString();

        if (alert != null)
        {
            bool isCritical = current <= minThreshold;
            alert.gameObject.SetActive(isCritical);
            
            if (isCritical)
            {
                alert.color = Color.red;
            }
        }  
    }
}

[Serializable]
public class StatBarBinding
{
    public Image icon;
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
    public StatHudBinding mind;
    public StatHudBinding heart;
    public StatHudBinding body;

    public void SetValues(int heartValue, int heartMax, int bodyValue, int bodyMax, int mindValue, int mindMax)
    {
        heart?.SetValue(heartValue, heartMax);
        body?.SetValue(bodyValue, bodyMax);
        mind?.SetValue(mindValue, mindMax);
    }
}