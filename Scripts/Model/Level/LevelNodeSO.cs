using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelNodeSO", menuName = "Game/Level Node Definition")]
public class LevelNodeSO : ScriptableObject
{

    [Header("Base Settings")]
    public NodeType nodeType;
    public NodeFlags flags;

    [Header("Spawn Table")]
    public List<SpawnEntry> spawnTable = new();

    public SpawnEntry GetRandomSpawn()
    {
        if (spawnTable == null || spawnTable.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (var entry in spawnTable)
            totalWeight += entry.weight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in spawnTable)
        {
            cumulative += entry.weight;
            if (roll < cumulative)
                return entry;
        }

        return null;
    }
}
