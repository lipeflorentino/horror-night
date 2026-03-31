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
    public struct ArchetypePoints
    {
        [SerializeField] public int nt;
        [SerializeField] public int nf;
        [SerializeField] public int sj;
        [SerializeField] public int sp;

        public int Get(PlayerArchetype archetype)
        {
            return archetype switch
            {
                PlayerArchetype.NF => nf,
                PlayerArchetype.SJ => sj,
                PlayerArchetype.SP => sp,
                _ => nt
            };
        }

        public void Add(PlayerArchetype archetype, int amount)
        {
            switch (archetype)
            {
                case PlayerArchetype.NF:
                    nf += amount;
                    break;
                case PlayerArchetype.SJ:
                    sj += amount;
                    break;
                case PlayerArchetype.SP:
                    sp += amount;
                    break;
                default:
                    nt += amount;
                    break;
            }
        }
    }

    [System.Serializable]
    public struct PlayerStatusSnapshot
    {
        [SerializeField] public float heart;
        [SerializeField] public float body;
        [SerializeField] public float mind;
        [SerializeField] public PlayerArchetype currentArchetype;
        [SerializeField] public ArchetypePoints archetypePoints;
        public TurnManagerStats combatStats;
    }

    [Header("Archetype")]
    [SerializeField] private PlayerArchetype initialArchetype = PlayerArchetype.NT;
    [SerializeField] private PlayerArchetype currentArchetype = PlayerArchetype.NT;
    [SerializeField] private ArchetypePoints archetypePoints;

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
        currentArchetype = initialArchetype;

        currentHeart = Mathf.Clamp(currentHeart, 0f, maxHeart);
        currentBody = Mathf.Clamp(currentBody, 0f, maxBody);
        currentMind = Mathf.Clamp(currentMind, 0f, maxMind);

        RefreshAllBars();
    }

    public PlayerArchetype GetCurrentArchetype() => currentArchetype;

    public void SetCurrentArchetype(PlayerArchetype archetype)
    {
        currentArchetype = archetype;
    }

    public int GetArchetypePoints(PlayerArchetype archetype)
    {
        return archetypePoints.Get(archetype);
    }

    public void AddArchetypePoint(PlayerArchetype archetype, int amount = 1)
    {
        if (amount <= 0)
            return;

        archetypePoints.Add(archetype, amount);
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

    public float GetHeartRatio() => maxHeart <= 0f ? 0f : currentHeart / maxHeart;
    public float GetBodyRatio() => maxBody <= 0f ? 0f : currentBody / maxBody;
    public float GetMindRatio() => maxMind <= 0f ? 0f : currentMind / maxMind;

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
            currentArchetype = currentArchetype,
            archetypePoints = archetypePoints,
            combatStats = stats
        };
    }

    public void RestoreSnapshot(PlayerStatusSnapshot snapshot)
    {
        currentHeart = Mathf.Clamp(snapshot.heart, 0f, maxHeart);
        currentBody = Mathf.Clamp(snapshot.body, 0f, maxBody);
        currentMind = Mathf.Clamp(snapshot.mind, 0f, maxMind);
        currentArchetype = snapshot.currentArchetype;
        archetypePoints = snapshot.archetypePoints;

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
            case "coracao":
            case "coração":
                return "heart";
            case "body":
            case "strength":
            case "força":
            case "forca":
            case "power":
            case "corpo":
                return "body";
            case "mind":
            case "mental":
            case "sanity":
            case "sanidade":
            case "mente":
                return "mind";
            default:
                return normalized;
        }
    }
}
