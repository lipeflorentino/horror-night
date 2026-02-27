using UnityEngine;
using System;

public class LevelController : MonoBehaviour
{
    public LevelSO currentLevel;
    public LevelNode[] nodes;
    public int CurrentIndex { get; private set; }
    public event Action<int> OnNodeChanged;
    public event Action OnLevelCompleted;
    private bool levelCompleted;
    private int startIndex;

    public void Initialize(LevelSO level)
    {
        currentLevel = level;

        nodes = new LevelNode[level.size];

        for (int i = 0; i < level.size; i++)
        {
            LevelNodeSO nodeDef;

            if (i == 0)
                nodeDef = level.leftPortalNode;
            else if (i == level.size - 1)
                nodeDef = level.rightPortalNode;
            else
                nodeDef = level.defaultNode;

            nodes[i] = new LevelNode(i, nodeDef);
        }

        startIndex = level.size / 2;
        CurrentIndex = startIndex;
        OnNodeChanged?.Invoke(CurrentIndex);
    }

    public bool TryMove(int direction)
    {
        int targetIndex = CurrentIndex + direction;

        if (targetIndex < 0 || targetIndex >= nodes.Length)
            return false;

        CurrentIndex = targetIndex;

        if (!nodes[CurrentIndex].explored)
        {
            nodes[CurrentIndex].explored = true;
            nodes[CurrentIndex].looted = true;
        }

        OnNodeChanged?.Invoke(CurrentIndex);

        return true;
    }

    public Vector3 GetWorldPositionFromIndex(int index)
    {
        int offsetFromCenter = index - startIndex;
        float x = offsetFromCenter * currentLevel.tileSpacing;
        return new Vector3(x, 0f, 0f);
    }

    public LevelNode GetCurrentNode()
    {
        return nodes[CurrentIndex];
    }

    public void MarkLevelCompleted()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;
        OnLevelCompleted?.Invoke();
    }

    public void ResetCompletion()
    {
        levelCompleted = false;
    }
}