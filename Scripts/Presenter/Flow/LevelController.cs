using System;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public LevelSO currentLevel;
    public LevelNode[] nodes;
    public int CurrentIndex { get; private set; }
    public int CurrentAreaIndex { get; private set; }

    public event Action<int> OnNodeChanged;
    public event Action<int> OnAreaChanged;
    public event Action OnLevelCompleted;

    private bool levelCompleted;
    private int startIndex;

    public void Initialize(LevelSO level)
    {
        currentLevel = level;
        levelCompleted = false;

        if (currentLevel == null)
        {
            nodes = Array.Empty<LevelNode>();
            CurrentIndex = 0;
            CurrentAreaIndex = 0;
            return;
        }

        nodes = new LevelNode[currentLevel.TotalNodes];

        for (int i = 0; i < nodes.Length; i++)
        {
            LevelNodeSO nodeDef = currentLevel.GetNodeDefinitionForIndex(i);
            nodes[i] = new LevelNode(i, nodeDef);
        }

        startIndex = 0;
        CurrentIndex = startIndex;
        CurrentAreaIndex = GetAreaIndex(CurrentIndex);
        nodes[CurrentIndex].explored = true;

        OnNodeChanged?.Invoke(CurrentIndex);
        OnAreaChanged?.Invoke(CurrentAreaIndex);
    }

    public bool TryMove(int direction)
    {
        if (nodes == null || nodes.Length == 0)
            return false;

        int targetIndex = CurrentIndex + direction;

        if (targetIndex < 0 || targetIndex >= nodes.Length)
            return false;

        int previousAreaIndex = CurrentAreaIndex;

        CurrentIndex = targetIndex;
        CurrentAreaIndex = GetAreaIndex(CurrentIndex);

        if (!nodes[CurrentIndex].explored)
            nodes[CurrentIndex].explored = true;

        OnNodeChanged?.Invoke(CurrentIndex);

        if (CurrentAreaIndex != previousAreaIndex)
            OnAreaChanged?.Invoke(CurrentAreaIndex);

        if (currentLevel.IsFinalNodeIndex(CurrentIndex))
            MarkLevelCompleted();

        return true;
    }

    public Vector3 GetWorldPositionFromIndex(int index)
    {
        int localIndex = currentLevel == null ? index : index % currentLevel.nodesPerArea;
        int offsetFromStart = localIndex - startIndex;
        float x = offsetFromStart * currentLevel.tileSpacing;
        return new Vector3(x, 0f, 0f);
    }

    public LevelNode GetCurrentNode()
    {
        if (nodes == null || nodes.Length == 0)
            return null;

        return nodes[CurrentIndex];
    }

    public bool IsCurrentNodeAreaPortal()
    {
        if (currentLevel == null || nodes == null || nodes.Length == 0)
            return false;

        return currentLevel.IsAreaEndIndex(CurrentIndex) && !currentLevel.IsFinalNodeIndex(CurrentIndex);
    }


    public bool[] CaptureExploredSnapshot()
    {
        if (nodes == null)
            return Array.Empty<bool>();

        bool[] explored = new bool[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            explored[i] = nodes[i] != null && nodes[i].explored;

        return explored;
    }

    public void RestoreProgress(LevelSO level, int currentIndex, bool[] exploredSnapshot)
    {
        Initialize(level);

        if (nodes == null || nodes.Length == 0)
            return;

        if (exploredSnapshot != null)
        {
            int count = Mathf.Min(exploredSnapshot.Length, nodes.Length);
            for (int i = 0; i < count; i++)
                nodes[i].explored = exploredSnapshot[i];
        }

        CurrentIndex = Mathf.Clamp(currentIndex, 0, nodes.Length - 1);
        nodes[CurrentIndex].explored = true;
        CurrentAreaIndex = GetAreaIndex(CurrentIndex);

        OnNodeChanged?.Invoke(CurrentIndex);
        OnAreaChanged?.Invoke(CurrentAreaIndex);
    }

    public void MarkLevelCompleted()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;
        OnLevelCompleted?.Invoke();
    }

    private int GetAreaIndex(int nodeIndex)
    {
        return nodeIndex / Mathf.Max(1, currentLevel.nodesPerArea);
    }
}
