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
    public StatRange life;
    public StatRange physical;
    public StatRange mental;

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

        return new EnemyInstance
        {
            source = this,
            life = life.Roll(difficulty),
            physical = physical.Roll(difficulty),
            mental = mental.Roll(difficulty),
            runTier = context != null ? context.tier : 0
        };
    }
}
