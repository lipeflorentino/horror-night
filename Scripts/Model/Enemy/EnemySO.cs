using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyTagSet
{
    public string behavior;
    public string style;
    public string type;

    public IEnumerable<string> Enumerate()
    {
        if (!string.IsNullOrWhiteSpace(behavior))
            yield return behavior;
        if (!string.IsNullOrWhiteSpace(style))
            yield return style;
        if (!string.IsNullOrWhiteSpace(type))
            yield return type;
    }
}

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
    public EnemyTagSet tags = new EnemyTagSet();

    [Header("Stats Range")]
    public StatRange heart;
    public StatRange body;
    public StatRange mind;

    [Header("Combat - Advanced Stats")]
    public StatRange attack;
    public StatRange defense;
    public StatRange initiative;
    public StatRange criticalHitChance;
    public StatRange parryChance;
    public StatRange fleeChance;
    public StatRange instantKillChance;
    public StatRange learnChance;

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

        int rolledHeart = heart.Roll(difficulty);
        int rolledBody = body.Roll(difficulty);
        int rolledMind = mind.Roll(difficulty);

        return new EnemyInstance
        {
            source = this,
            heart = rolledHeart,
            body = rolledBody,
            mind = rolledMind,
            runTier = context != null ? context.tier : 0,
        };
    }
}
