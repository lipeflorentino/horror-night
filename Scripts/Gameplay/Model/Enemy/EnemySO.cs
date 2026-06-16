using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyBehaviorTag { Aggressive, Defensive, Cautious, Erratic, Relentless }
public enum EnemyStyleTag { Stealthy, Brutal, Ritualistic, Primal, Mental }
public enum EnemyTypeTag { Creature, Flying, Hybrid, Inanimate }
public enum EnemyArchetype { Normal, Special, Boss }

[Serializable]
public class EnemyTagSet
{
    public EnemyBehaviorTag behavior;
    public EnemyStyleTag style;
    public EnemyTypeTag type;

    public IEnumerable<string> Enumerate()
    {
        yield return behavior.ToString().ToLowerInvariant();
        yield return style.ToString().ToLowerInvariant();
        yield return type.ToString().ToLowerInvariant();
    }
}

[CreateAssetMenu(menuName = "Game/Enemy")]
public class EnemySO : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string enemyName;
    [TextArea] public string description;
    public EnemyArchetype archetype;

    [Header("Spawn")]
    [Min(0)] public int weight = 1;
    public int tierMin;
    public int tierMax = 10;

    [Header("Visual")]
    public Sprite image;

    [Header("Tags")]
    public EnemyTagSet tags = new();

    [Header("Stats Range")]
    public StatRange hp;
    public StatRange heart;
    public StatRange body;
    public StatRange mind;

    [Header("Combat - Advanced Stats")]
    public StatRange attack;
    public StatRange defense;
    public StatRange initiative;
    public StatRange focus = new() { min = 0, max = 0 };
    public StatRange strength = new() { min = 0, max = 0 };
    public StatRange agility = new() { min = 0, max = 0 };

    [Header("Tricks")]
    [SerializeField] private List<string> trickIds = new();
    public IReadOnlyList<string> TrickIds => trickIds;

    [Header("Optional")]
    [TextArea] public string specialRule;

    public bool MatchesContext(EnemyRunContext context)
    {
        if (context == null)
            return true;

        int maxTier = Mathf.Max(tierMin, tierMax);
        if (context.tier < tierMin || context.tier > maxTier)
            return false;

        return true;
    }

    public EnemyInstance RollInstance(EnemyRunContext context)
    {
        float difficulty = context != null ? context.DifficultyModifier : 1f;

        int rolledHp = hp.Roll(difficulty);
        int rolledHeart = heart.Roll(difficulty);
        int rolledBody = body.Roll(difficulty);
        int rolledMind = mind.Roll(difficulty);

        int rolledAttack = attack.Roll(difficulty);
        int rolledDefense = defense.Roll(difficulty);
        int rolledInitiative = initiative.Roll(difficulty);
        int rolledFocus = focus.Roll(difficulty);
        int rolledStrength = strength.Roll(difficulty);
        int rolledAgility = agility.Roll(difficulty);

        return new EnemyInstance
        {
            source = this,
            hp = rolledHp,
            heart = rolledHeart,
            body = rolledBody,
            mind = rolledMind,
            attack = rolledAttack,
            defense = rolledDefense,
            initiative = rolledInitiative,
            focus = rolledFocus,
            strength = rolledStrength,
            agility = rolledAgility,
            runTier = context != null ? context.tier : 0
        };
    }

    /// <summary>
    /// Gera um TrickInventorySnapshot com as tricks deste inimigo.
    /// Se não houver tricks, retorna um snapshot vazio (seguro).
    /// </summary>
    public TrickInventorySnapshot GetTrickInventorySnapshot()
    {
        TrickInventorySnapshot snapshot = new();
        
        if (trickIds == null || trickIds.Count == 0)
            return snapshot;

        foreach (string trickId in trickIds)
        {
            if (!string.IsNullOrWhiteSpace(trickId))
                snapshot.learnedTrickIds.Add(trickId.Trim());
        }

        return snapshot;
    }

    public void SetTrickIds(List<string> ids)
    {
        trickIds = ids ?? new List<string>();
    }
}