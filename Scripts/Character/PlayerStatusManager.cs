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

    // Alias methods in case "strenght" is used elsewhere.
    public void IncreaseStrenght(float amount)
    {
        IncreaseStrength(amount);
    }

    public void DecreaseStrenght(float amount)
    {
        DecreaseStrength(amount);
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
