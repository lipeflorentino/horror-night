using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNode
{
    public int index;
    public LevelNodeSO definition;

    public bool explored, looted, activityResolved;

    public LevelNode(int index, LevelNodeSO definition)
    {
        this.index = index;
        this.definition = definition;
        explored = false;
        looted = false;
        activityResolved = false;
    }
}
