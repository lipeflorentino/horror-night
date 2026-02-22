using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNode
{
    public int index;
    public LevelNodeSO definition;

    public bool explored;
    public SpawnEntry spawnedContent;

    public LevelNode(int index, LevelNodeSO definition)
    {
        this.index = index;
        this.definition = definition;
        explored = false;
    }

    public void GenerateContent()
    {
        if (definition == null)
            return;

        if (definition.flags.HasFlag(NodeFlags.CanSpawnLoot))
        {
            spawnedContent = definition.GetRandomSpawn();
        }
    }
}
