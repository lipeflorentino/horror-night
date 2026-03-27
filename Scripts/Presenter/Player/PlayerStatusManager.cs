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
        [SerializeField] public float heart;
        [SerializeField] public float body;
        [SerializeField] public float mind;
        public TurnManagerStats combatStats;
    }

    [Header("Status HUD (Horizontal Bar + Text)")]
    [SerializeField] private StatHudBinding heartHud;
    [SerializeField] private StatHudBinding bodyHud;
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
    [SerializeField] private float maxHeart = 100f;
    [SerializeField] private float currentHeart = 100f;

    [Header("body")]
    [SerializeField] private float maxBody = 100f;
    [SerializeField] private float currentBody = 100f;

    [Header("Mind")]
    [SerializeField] private float maxMind = 100f;
    [SerializeField] private float currentMind = 100f;

    private void Awake()
    {
        currentHeart = Mathf.Clamp(currentHeart, 0f, maxHeart);
        currentBody = Mathf.Clamp(currentBody, 0f, maxBody);
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

    public void Increasebody(float amount)
    {
        currentBody = Mathf.Clamp(currentBody + amount, 0f, maxBody);
        bodyHud?.SetValue(currentBody, maxBody);
    }

    public void DecreaseBody(float amount)
    {
        currentBody = Mathf.Clamp(currentBody - amount, 0f, maxBody);
        bodyHud?.SetValue(currentBody, maxBody);
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
    public float GetCurrentBody() => currentBody;

    public int GetStatValue(string statName)
    {
        switch (NormalizeStatName(statName))
        {
            case "heart":
                return Mathf.RoundToInt(currentHeart);
            case "body":
                return Mathf.RoundToInt(currentBody);
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
            case "body":
                if (value > 0) Increasebody(value);
                else DecreaseBody(-value);
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
            body = currentBody,
            mind = currentMind,
            combatStats = stats
        };
    }

    public void RestoreSnapshot(PlayerStatusSnapshot snapshot)
    {
        currentHeart = Mathf.Clamp(snapshot.heart, 0f, maxHeart);
        currentBody = Mathf.Clamp(snapshot.body, 0f, maxBody);
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

    public bool CanSpendBody(float amount)
    {
        return amount >= 0f && currentBody >= amount;
    }

    public bool TrySpendBody(float amount)
    {
        if (!CanSpendBody(amount))
            return false;

        DecreaseBody(amount);
        return true;
    }

    private void RefreshAllBars()
    {
        heartHud?.SetValue(currentHeart, maxHeart);
        bodyHud?.SetValue(currentBody, maxBody);
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
            case "body":
            case "strength":
            case "força":
            case "forca":
            case "power":
                return "body";
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
