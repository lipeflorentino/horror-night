using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDatabase : MonoBehaviour
{
    public List<EnemySO> allEnemies = new List<EnemySO>();

    private void Awake()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        EnemySO[] loaded = Resources.LoadAll<EnemySO>("Data/Enemies");
        allEnemies.Clear();
        allEnemies.AddRange(loaded);
    }

    public EnemyInstance RollRandomEnemy(EnemyRunContext context)
    {
        if (allEnemies == null || allEnemies.Count == 0)
            return null;

        List<EnemySO> candidates = FilterCandidates(context);
        if (candidates.Count == 0)
            return null;

        int totalWeight = 0;
        foreach (EnemySO enemy in candidates)
            totalWeight += Mathf.Max(1, GetContextWeight(enemy, context));

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (EnemySO enemy in candidates)
        {
            cumulative += Mathf.Max(1, GetContextWeight(enemy, context));
            if (roll < cumulative)
                return enemy.RollInstance(context);
        }

        return candidates[candidates.Count - 1].RollInstance(context);
    }

    private List<EnemySO> FilterCandidates(EnemyRunContext context)
    {
        List<EnemySO> candidates = new List<EnemySO>();
        foreach (EnemySO enemy in allEnemies)
        {
            if (enemy == null)
                continue;

            if (enemy.MatchesContext(context))
                candidates.Add(enemy);
        }

        return candidates;
    }

    private static int GetContextWeight(EnemySO enemy, EnemyRunContext context)
    {
        int weight = Mathf.Max(1, enemy.weight);
        if (context == null)
            return weight;

        HashSet<string> tags = context.BuildTagSet();
        if (tags.Count == 0 || enemy.tags == null)
            return weight;

        foreach (string enemyTag in enemy.tags.Enumerate())
        {
            if (tags.Contains(enemyTag))
            {
                weight += Mathf.Max(1, Mathf.CeilToInt(enemy.weight * 0.5f));
                break;
            }
        }

        return weight;
    }
}
