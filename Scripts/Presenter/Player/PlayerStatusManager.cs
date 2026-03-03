using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusManager : MonoBehaviour
{
    [Header("Status Bars (Image Fill Radial 360)")]
    [SerializeField] private Image lifeBar;
    [SerializeField] private Image strengthBar;
    [SerializeField] private Image sanityBar;

    [Header("Life")]
    [SerializeField] private float maxLife = 100f;
    [SerializeField] private float currentLife = 100f;

    [Header("Strength")]
    [SerializeField] private float maxStrength = 100f;
    [SerializeField] private float currentStrength = 100f;

    [Header("Sanity")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float currentSanity = 100f;

    private void Awake()
    {
        currentLife = Mathf.Clamp(currentLife, 0f, maxLife);
        currentStrength = Mathf.Clamp(currentStrength, 0f, maxStrength);
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        RefreshAllBars();
    }

    public void IncreaseLife(float amount)
    {
        currentLife = Mathf.Clamp(currentLife + amount, 0f, maxLife);
        UpdateBar(lifeBar, currentLife, maxLife);
    }

    public void DecreaseLife(float amount)
    {
        currentLife = Mathf.Clamp(currentLife - amount, 0f, maxLife);
        UpdateBar(lifeBar, currentLife, maxLife);
    }

    public void IncreaseStrength(float amount)
    {
        currentStrength = Mathf.Clamp(currentStrength + amount, 0f, maxStrength);
        UpdateBar(strengthBar, currentStrength, maxStrength);
    }

    public void DecreaseStrength(float amount)
    {
        currentStrength = Mathf.Clamp(currentStrength - amount, 0f, maxStrength);
        UpdateBar(strengthBar, currentStrength, maxStrength);
    }

    public void IncreaseSanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
        UpdateBar(sanityBar, currentSanity, maxSanity);
    }

    public void DecreaseSanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity - amount, 0f, maxSanity);
        UpdateBar(sanityBar, currentSanity, maxSanity);
    }



    public float GetCurrentLife()
    {
        return currentLife;
    }

    public float GetCurrentSanity()
    {
        return currentSanity;
    }

    public int GetStatValue(string statName)
    {
        switch (statName)
        {
            case "life":
                return Mathf.RoundToInt(currentLife);
            case "physical":
            case "power":
                return Mathf.RoundToInt(currentStrength);
            case "sanity":
                return Mathf.RoundToInt(currentSanity);
            default:
                return 0;
        }
    }

    public void ApplyStatDelta(string statName, int value)
    {
        if (value == 0)
            return;

        if (statName == "life")
        {
            if (value > 0)
                IncreaseLife(value);
            else
                DecreaseLife(-value);
            return;
        }

        if (statName == "physical" || statName == "power")
        {
            if (value > 0)
                IncreaseStrength(value);
            else
                DecreaseStrength(-value);
            return;
        }

        if (statName == "sanity")
        {
            if (value > 0)
                IncreaseSanity(value);
            else
                DecreaseSanity(-value);
        }
    }

    public float GetCurrentStrength()
    {
        return currentStrength;
    }

    public bool CanSpendStrength(float amount)
    {
        return amount >= 0f && currentStrength >= amount;
    }

    public bool TrySpendStrength(float amount)
    {
        if (!CanSpendStrength(amount))
        {
            return false;
        }

        DecreaseStrength(amount);
        return true;
    }

    private void RefreshAllBars()
    {
        UpdateBar(lifeBar, currentLife, maxLife);
        UpdateBar(strengthBar, currentStrength, maxStrength);
        UpdateBar(sanityBar, currentSanity, maxSanity);
    }

    private void UpdateBar(Image bar, float currentValue, float maxValue)
    {
        if (bar == null || maxValue <= 0f)
            return;

        bar.fillAmount = currentValue / maxValue;
    }
}
