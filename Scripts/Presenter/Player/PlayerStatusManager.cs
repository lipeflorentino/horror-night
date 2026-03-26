using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerStatusManager : MonoBehaviour
{
    [System.Serializable]
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

    [System.Serializable]
    public struct PlayerStatusSnapshot
    {
        [FormerlySerializedAs("life")] public float heart;
        [FormerlySerializedAs("strength")] public float physical;
        [FormerlySerializedAs("sanity")] public float mind;
        public TurnManagerStats combatStats;
    }

    [Header("Status HUD (Horizontal Bar + Text)")]
    [SerializeField] private StatHudBinding heartHud;
    [SerializeField] private StatHudBinding physicalHud;
    [SerializeField] private StatHudBinding mindHud;

    [Header("Combat - Advanced Stats")]
    [SerializeField] private int attack = 10;
    [SerializeField] private int defense = 5;
    [Range(0, 100)] [SerializeField] private int criticalHitChance = 10;
    [Range(0, 100)] [SerializeField] private int parryChance = 25;
    [Range(0, 100)] [SerializeField] private int fleeChance = 35;
    [Range(0, 100)] [SerializeField] private int instantKillChance = 5;
    [Range(0, 100)] [SerializeField] private int learnChance = 55;

    [Header("Heart")]
    [FormerlySerializedAs("maxLife")] [SerializeField] private float maxHeart = 100f;
    [FormerlySerializedAs("currentLife")] [SerializeField] private float currentHeart = 100f;

    [Header("Physical")]
    [FormerlySerializedAs("maxStrength")] [SerializeField] private float maxPhysical = 100f;
    [FormerlySerializedAs("currentStrength")] [SerializeField] private float currentPhysical = 100f;

    [Header("Mind")]
    [FormerlySerializedAs("maxSanity")] [SerializeField] private float maxMind = 100f;
    [FormerlySerializedAs("currentSanity")] [SerializeField] private float currentMind = 100f;

    private void Awake()
    {
        currentHeart = Mathf.Clamp(currentHeart, 0f, maxHeart);
        currentPhysical = Mathf.Clamp(currentPhysical, 0f, maxPhysical);
        currentMind = Mathf.Clamp(currentMind, 0f, maxMind);

        RefreshAllBars();
    }

    public void IncreaseHeart(float amount)
    {
        currentHeart = Mathf.Clamp(currentHeart + amount, 0f, maxHeart);
        heartHud?.SetValue(currentHeart, maxHeart);
    }

    public void DecreaseHeart(float amount)
    {
        currentHeart = Mathf.Clamp(currentHeart - amount, 0f, maxHeart);
        heartHud?.SetValue(currentHeart, maxHeart);
    }

    public void IncreasePhysical(float amount)
    {
        currentPhysical = Mathf.Clamp(currentPhysical + amount, 0f, maxPhysical);
        physicalHud?.SetValue(currentPhysical, maxPhysical);
    }

    public void DecreasePhysical(float amount)
    {
        currentPhysical = Mathf.Clamp(currentPhysical - amount, 0f, maxPhysical);
        physicalHud?.SetValue(currentPhysical, maxPhysical);
    }

    public void IncreaseMind(float amount)
    {
        currentMind = Mathf.Clamp(currentMind + amount, 0f, maxMind);
        mindHud?.SetValue(currentMind, maxMind);
    }

    public void DecreaseMind(float amount)
    {
        currentMind = Mathf.Clamp(currentMind - amount, 0f, maxMind);
        mindHud?.SetValue(currentMind, maxMind);
    }

    public float GetCurrentHeart() => currentHeart;
    public float GetCurrentMind() => currentMind;
    public float GetCurrentPhysical() => currentPhysical;

    // Backward-compatible helpers.
    public float GetCurrentLife() => GetCurrentHeart();
    public float GetCurrentSanity() => GetCurrentMind();
    public float GetCurrentStrength() => GetCurrentPhysical();

    public int GetStatValue(string statName)
    {
        switch (NormalizeStatName(statName))
        {
            case "heart":
                return Mathf.RoundToInt(currentHeart);
            case "physical":
                return Mathf.RoundToInt(currentPhysical);
            case "mind":
                return Mathf.RoundToInt(currentMind);
            default:
                return 0;
        }
    }

    public void ApplyStatDelta(string statName, int value)
    {
        if (value == 0)
            return;

        switch (NormalizeStatName(statName))
        {
            case "heart":
                if (value > 0) IncreaseHeart(value);
                else DecreaseHeart(-value);
                return;
            case "physical":
                if (value > 0) IncreasePhysical(value);
                else DecreasePhysical(-value);
                return;
            case "mind":
                if (value > 0) IncreaseMind(value);
                else DecreaseMind(-value);
                return;
        }
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
            heart = currentHeart,
            physical = currentPhysical,
            mind = currentMind,
            combatStats = stats
        };
    }

    public void RestoreSnapshot(PlayerStatusSnapshot snapshot)
    {
        currentHeart = Mathf.Clamp(snapshot.heart, 0f, maxHeart);
        currentPhysical = Mathf.Clamp(snapshot.physical, 0f, maxPhysical);
        currentMind = Mathf.Clamp(snapshot.mind, 0f, maxMind);

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

    public bool CanSpendPhysical(float amount)
    {
        return amount >= 0f && currentPhysical >= amount;
    }

    public bool TrySpendPhysical(float amount)
    {
        if (!CanSpendPhysical(amount))
            return false;

        DecreasePhysical(amount);
        return true;
    }

    // Backward-compatible helpers.
    public void IncreaseLife(float amount) => IncreaseHeart(amount);
    public void DecreaseLife(float amount) => DecreaseHeart(amount);
    public void IncreaseStrength(float amount) => IncreasePhysical(amount);
    public void DecreaseStrength(float amount) => DecreasePhysical(amount);
    public void IncreaseSanity(float amount) => IncreaseMind(amount);
    public void DecreaseSanity(float amount) => DecreaseMind(amount);
    public bool CanSpendStrength(float amount) => CanSpendPhysical(amount);
    public bool TrySpendStrength(float amount) => TrySpendPhysical(amount);

    private void RefreshAllBars()
    {
        heartHud?.SetValue(currentHeart, maxHeart);
        physicalHud?.SetValue(currentPhysical, maxPhysical);
        mindHud?.SetValue(currentMind, maxMind);
    }

    private static string NormalizeStatName(string statName)
    {
        string normalized = string.IsNullOrWhiteSpace(statName)
            ? string.Empty
            : statName.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "heart":
            case "life":
            case "vida":
                return "heart";
            case "physical":
            case "strength":
            case "força":
            case "forca":
            case "power":
                return "physical";
            case "mind":
            case "mental":
            case "sanity":
            case "sanidade":
                return "mind";
            default:
                return normalized;
        }
    }
}
