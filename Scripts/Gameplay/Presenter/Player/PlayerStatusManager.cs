using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerStatusManager : MonoBehaviour
{
    private const float CoreStatCap = 20f;

    [Header("Character")]
    [SerializeField] private CharacterSO character;

    [Header("Archetype")]
    [SerializeField] private PlayerArchetype initialArchetype = PlayerArchetype.Standard;
    [SerializeField] private PlayerArchetype currentArchetype = PlayerArchetype.Standard;
    [SerializeField] private ArchetypePoints archetypePoints;

    [Header("Status HUD (Horizontal Bar + Text)")]
    [SerializeField] private StatHudBinding heartHud;
    [SerializeField] private StatHudBinding bodyHud;
    [SerializeField] private StatHudBinding mindHud;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private TrickInventorySnapshot trickInventorySnapshot = new();

    [Header("Combat - Advanced Stats")]
    [SerializeField] private int attack = 10;
    [SerializeField] private int defense = 5;
    [SerializeField] private int initiative = 10;
    [SerializeField] private int focus = 0;
    [SerializeField] private int strength = 0;
    [SerializeField] private int agility = 0;
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXp = 0;
    [SerializeField] private int maxXp = 10;
    [SerializeField] private int currentPowerDices = 3;
    [SerializeField] private int currentAccuracyDices = 3;
    [SerializeField] private int maxPowerDices = 3;
    [SerializeField] private int maxAccuracyDices = 3;

    [Header("Heart")]
    [SerializeField] private float maxHeart = 100f;
    [SerializeField] private float currentHeart = 100f;

    [Header("body")]
    [SerializeField] private float maxBody = 100f;
    [SerializeField] private float currentBody = 100f;

    [Header("Mind")]
    [SerializeField] private float maxMind = 100f;
    [SerializeField] private float currentMind = 100f;

    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp = 100f;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
            
        ApplyCharacterDefaults();
        currentArchetype = initialArchetype;
        maxHeart = ClampCoreStatMax(maxHeart);
        maxBody = ClampCoreStatMax(maxBody);
        maxMind = ClampCoreStatMax(maxMind);
        currentHeart = ClampCoreStat(currentHeart, maxHeart);
        currentBody = ClampCoreStat(currentBody, maxBody);
        currentMind = ClampCoreStat(currentMind, maxMind);
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        RefreshAllBars();
    }

    private void Start()
    {
        CombatResultSnapshot result = CombatResultStore.Consume();

        if (result == null)
            return;

        RestoreSnapshot(result.PlayerSnapshot);
        AddXp(result.XpGained);
        if (result.ItensGained == null)
            return;

        foreach (var kvp in result.ItensGained)
        {
            if (kvp.Key == null || kvp.Value <= 0)
                continue;

            AddInventoryItem(kvp.Key.itemName, kvp.Value);
        }
    }

    public CharacterSO GetCharacter() => character;

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
        currentHeart = ClampCoreStat(currentHeart + amount, maxHeart);
        heartHud?.SetValue(currentHeart, maxHeart);
    }

    public void DecreaseHeart(float amount)
    {
        currentHeart = ClampCoreStat(currentHeart - amount, maxHeart);
        heartHud?.SetValue(currentHeart, maxHeart);
    }

    public void IncreaseBody(float amount)
    {
        currentBody = ClampCoreStat(currentBody + amount, maxBody);
        bodyHud?.SetValue(currentBody, maxBody);
    }

    public void DecreaseBody(float amount)
    {
        currentBody = ClampCoreStat(currentBody - amount, maxBody);
        bodyHud?.SetValue(currentBody, maxBody);
    }

    public void IncreaseMind(float amount)
    {
        currentMind = ClampCoreStat(currentMind + amount, maxMind);
        mindHud?.SetValue(currentMind, maxMind);
    }

    public void DecreaseMind(float amount)
    {
        currentMind = ClampCoreStat(currentMind - amount, maxMind);
        mindHud?.SetValue(currentMind, maxMind);
    }

    public float GetCurrentHeart() => currentHeart;
    public float GetCurrentMind() => currentMind;
    public float GetCurrentBody() => currentBody;
    public float GetMaxHeart() => maxHeart;
    public float GetMaxBody() => maxBody;
    public float GetMaxMind() => maxMind;

    public float GetHeartRatio() => maxHeart <= 0f ? 0f : currentHeart / maxHeart;
    public float GetBodyRatio() => maxBody <= 0f ? 0f : currentBody / maxBody;
    public float GetMindRatio() => maxMind <= 0f ? 0f : currentMind / maxMind;
    public int GetLevel() => Mathf.Max(1, level);

    public int GetStatValue(string statName)
    {
        return NormalizeStatName(statName) switch
        {
            "heart" => Mathf.RoundToInt(currentHeart),
            "body" => Mathf.RoundToInt(currentBody),
            "mind" => Mathf.RoundToInt(currentMind),
            "initiative" => initiative,
            "focus" => focus,
            "strength" => strength,
            "agility" => agility,
            "attack" => attack,
            "defense" => defense,
            _ => 0,
        };
    }

    public int GetAttack() => attack;
    public int GetDefense() => defense;
    public int GetInitiative() => initiative;
    public int GetFocus() => focus;
    public int GetStrength() => strength;
    public int GetAgility() => agility;

    public void ApplyStatDelta(string statName, int value)
    {
        Logger.Log($"[PlayerStatusManager] Aplicando stat delta: {statName} {value}");
        if (value == 0)
            return;

        switch (NormalizeStatName(statName))
        {
            case "heart":
                if (value > 0) IncreaseHeart(value);
                else DecreaseHeart(-value);
                return;
            case "body":
                if (value > 0) IncreaseBody(value);
                else DecreaseBody(-value);
                return;
            case "mind":
                if (value > 0) IncreaseMind(value);
                else DecreaseMind(-value);
                return;
            case "initiative":
                initiative = Mathf.Max(0, initiative + value);
                return;
            case "focus":
                focus = Mathf.Max(0, focus + value);
                return;
            case "strength":
                strength = Mathf.Max(0, strength + value);
                return;
            case "agility":
                agility = Mathf.Max(0, agility + value);
                return;
            case "attack":
                attack = Mathf.Max(0, attack + value);
                return;
            case "defense":
                defense = Mathf.Max(0, defense + value);
                return;
            default:
                Logger.Log($"[PlayerStatusManager] Stat desconhecida: {statName}");
                return;
        }
    }

    public PlayerStatusSnapshot GetSnapshot()
    {
        return new PlayerStatusSnapshot
        {
            characterId = character != null ? character.Id : string.Empty,
            characterName = character != null ? character.DisplayName : string.Empty,
            heart = ClampCoreStat(currentHeart, maxHeart),
            body = ClampCoreStat(currentBody, maxBody),
            mind = ClampCoreStat(currentMind, maxMind),
            attack = attack,
            defense = defense,
            initiative = initiative,
            focus = focus,
            strength = strength,
            agility = agility,
            level = GetLevel(),
            currentXp = Mathf.Max(0, currentXp),
            maxXp = Mathf.Max(1, maxXp),
            hp = currentHp,
            maxHeart = ClampCoreStatMax(maxHeart),
            maxBody = ClampCoreStatMax(maxBody),
            maxMind = ClampCoreStatMax(maxMind),
            maxHp = maxHp,
            powerDices = currentPowerDices,
            accuracyDices = currentAccuracyDices,
            maxPowerDices = maxPowerDices,
            maxAccuracyDices = maxAccuracyDices,
            currentArchetype = currentArchetype,
            archetypePoints = archetypePoints,
            inventory = playerInventory != null ? playerInventory.GetSnapshot() : new PlayerInventorySnapshot(),
            trickInventory = TrickInventorySnapshot.CreatePersistentSnapshot(trickInventorySnapshot)
        };
    }

    public void RestoreSnapshot(PlayerStatusSnapshot snapshot)
    {
        maxHeart = ClampCoreStatMax(snapshot.maxHeart > 0f ? snapshot.maxHeart : maxHeart);
        maxBody = ClampCoreStatMax(snapshot.maxBody > 0f ? snapshot.maxBody : maxBody);
        maxMind = ClampCoreStatMax(snapshot.maxMind > 0f ? snapshot.maxMind : maxMind);
        maxHp = Mathf.Max(1f, snapshot.maxHp > 0f ? snapshot.maxHp : maxHp);

        currentHeart = ClampCoreStat(snapshot.heart, maxHeart);
        currentBody = ClampCoreStat(snapshot.body, maxBody);
        currentMind = ClampCoreStat(snapshot.mind, maxMind);
        if (snapshot.attack > 0f)
            attack = Mathf.Max(0, Mathf.RoundToInt(snapshot.attack));
        if (snapshot.defense > 0f)
            defense = Mathf.Max(0, Mathf.RoundToInt(snapshot.defense));
        if (snapshot.initiative > 0f)
            initiative = Mathf.Max(0, Mathf.RoundToInt(snapshot.initiative));
        focus = Mathf.Max(0, Mathf.RoundToInt(snapshot.focus));
        strength = Mathf.Max(0, Mathf.RoundToInt(snapshot.strength));
        agility = Mathf.Max(0, Mathf.RoundToInt(snapshot.agility));
        if (snapshot.level > 0)
            level = Mathf.Max(1, snapshot.level);
        currentXp = Mathf.Max(0, snapshot.currentXp);
        maxXp = Mathf.Max(1, snapshot.maxXp > 0 ? snapshot.maxXp : CalculateMaxXpForLevel(level));
        currentHp = Mathf.Clamp(snapshot.hp, 0f, maxHp);
        currentPowerDices = Mathf.Max(0, snapshot.powerDices);
        currentAccuracyDices = Mathf.Max(0, snapshot.accuracyDices);
        maxPowerDices = Mathf.Max(1, snapshot.maxPowerDices > 0 ? snapshot.maxPowerDices : maxPowerDices);
        maxAccuracyDices = Mathf.Max(1, snapshot.maxAccuracyDices > 0 ? snapshot.maxAccuracyDices : maxAccuracyDices);
        currentArchetype = snapshot.currentArchetype;
        archetypePoints = snapshot.archetypePoints;
        
        if (playerInventory != null)
            playerInventory.RestoreSnapshot(snapshot.inventory);
            
        trickInventorySnapshot = TrickInventorySnapshot.CreatePersistentSnapshot(snapshot.trickInventory);
        Logger.Log($"[PlayerStatusManager] Snapshot restaurado.");
        RefreshAllBars();
    }


    private void ApplyCharacterDefaults()
    {
        if (character == null)
            return;

        initialArchetype = PlayerArchetype.Standard;
        archetypePoints = new ArchetypePoints();
        level = 1;
        currentXp = Mathf.Max(0, character.Xp);
        maxXp = Mathf.Max(0, character.Xp);
        maxHeart = Mathf.Max(1f, character.Heart);
        currentHeart = maxHeart;
        maxBody = Mathf.Max(1f, character.Body);
        currentBody = maxBody;
        maxMind = Mathf.Max(1f, character.Mind);
        currentMind = maxMind;
        maxHp = Mathf.Max(1f, CalculateMaxXpForLevel(level));
        currentHp = maxHp;
        attack = Mathf.Max(0, character.Attack);
        defense = Mathf.Max(0, character.Defense);
        initiative = Mathf.Max(0, character.Initiative);
        focus = Mathf.Max(0, character.Focus);
        strength = Mathf.Max(0, character.Strength);
        agility = Mathf.Max(0, character.Agility);
        maxPowerDices = Mathf.Max(1, character.PowerDices);
        maxAccuracyDices = Mathf.Max(1, character.AccuracyDices);
        currentPowerDices = maxPowerDices;
        currentAccuracyDices = maxAccuracyDices;
        trickInventorySnapshot = TrickInventorySnapshot.CreatePersistentSnapshot(character.CreateInitialTrickSnapshot());
    }

    private void RefreshAllBars()
    {
        heartHud?.SetValue(currentHeart, maxHeart);
        bodyHud?.SetValue(currentBody, maxBody);
        mindHud?.SetValue(currentMind, maxMind);
    }

    private static float ClampCoreStat(float value, float maxValue)
    {
        return Mathf.Clamp(value, 0f, Mathf.Min(CoreStatCap, Mathf.Max(1f, maxValue)));
    }

    private static float ClampCoreStatMax(float maxValue)
    {
        return Mathf.Clamp(maxValue, 1f, CoreStatCap);
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
            case "physical":
            case "fisico":
            case "físico":
            case "corpo":
                return "body";
            case "strength":
            case "força":
            case "forca":
            case "power":
                return "strength";
            case "mind":
            case "mental":
            case "sanity":
            case "sanidade":
            case "mente":
                return "mind";
            case "initiative":
            case "iniciativa":
            case "speed":
                return "initiative";
            case "focus":
            case "foco":
                return "focus";
            case "agility":
            case "agilidade":
                return "agility";
            default:
                return normalized;
        }
    }

    private static int CalculateMaxXpForLevel(int currentLevel)
    {
        int normalizedLevel = Mathf.Max(1, currentLevel);
        return normalizedLevel * 10;
    }

    public void AddXp(int xpAmount)
    {
        if (xpAmount <= 0)
            return;

        maxXp = Mathf.Max(1, maxXp);
        currentXp += xpAmount;

        while (currentXp >= maxXp)
        {
            currentXp -= maxXp;
            LevelUp();
        }
    }

    public void LevelUp()
    {
        level = Mathf.Max(1, level + 1);
        maxXp = CalculateMaxXpForLevel(level);

        float hpIncrease = level * 5f;
        maxHp = Mathf.Max(1f, maxHp + hpIncrease);
        currentHp = Mathf.Clamp(currentHp + hpIncrease, 0f, maxHp);
    }

    public bool AddInventoryItem(string itemName, int quantity)
    {
        if (quantity <= 0 || playerInventory == null)
            return false;

        bool added = playerInventory.AddInventoryItem(itemName, quantity);
        if (!added)
        {
            Debug.LogWarning($"[PlayerStatusManager] Item '{itemName}' não encontrado para recompensa de inventário.");
            return false;
        }

        return true;
    }
}
