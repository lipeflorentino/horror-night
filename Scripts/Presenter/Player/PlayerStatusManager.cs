using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusManager : MonoBehaviour
{
    [System.Serializable]
    public struct PlayerStatusSnapshot
    {
        public float life;
        public float strength;
        public float sanity;
        public TurnManagerStats combatStats;
    }

    [Header("Status Bars (Image Fill Radial 360)")]
    [SerializeField] private Image lifeBar;
    [SerializeField] private Image strengthBar;
    [SerializeField] private Image sanityBar;


    [Header("Combat - Advanced Stats")]
    [SerializeField] private int attack = 10;
    [SerializeField] private int defense = 5;
    [Range(0, 100)] [SerializeField] private int criticalHitChance = 10;
    [Range(0, 100)] [SerializeField] private int parryChance = 25;
    [Range(0, 100)] [SerializeField] private int fleeChance = 35;
    [Range(0, 100)] [SerializeField] private int instantKillChance = 5;
    [Range(0, 100)] [SerializeField] private int learnChance = 55;

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

    public PlayerStatusSnapshot GetSnapshot()
    {
        TurnManagerStats stats = new TurnManagerStats
        {
            attack = attack,
            defense = defense,
            criticalHitChance = criticalHitChance,
            parryChance = parryChance,
            fleeChance = fleeChance,
            instantKillChance = instantKillChance,
            learnChance = learnChance
        };
        stats.Normalize();

        return new PlayerStatusSnapshot
        {
            life = currentLife,
            strength = currentStrength,
            sanity = currentSanity,
            combatStats = stats
        };
    }

    public void RestoreSnapshot(PlayerStatusSnapshot snapshot)
    {
        currentLife = Mathf.Clamp(snapshot.life, 0f, maxLife);
        currentStrength = Mathf.Clamp(snapshot.strength, 0f, maxStrength);
        currentSanity = Mathf.Clamp(snapshot.sanity, 0f, maxSanity);

        TurnManagerStats restoredStats = snapshot.combatStats;
        restoredStats.Normalize();

        attack = restoredStats.attack;
        defense = restoredStats.defense;
        criticalHitChance = restoredStats.criticalHitChance;
        parryChance = restoredStats.parryChance;
        fleeChance = restoredStats.fleeChance;
        instantKillChance = restoredStats.instantKillChance;
        learnChance = restoredStats.learnChance;

        RefreshAllBars();
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
