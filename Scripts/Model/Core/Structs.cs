using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct StatModifier
{
    [SerializeField] public int mind;
    [SerializeField] public int heart;
    [SerializeField] public int body;
}

public struct OccurrenceResult
{
    public bool success;
    public bool requiresRoll;
    public int selectedOption;
    public int occurrenceRoll;
    public int playerRoll;
    public int rollRange;
    public int delta;
    public string optionText;
    public string primaryStat;
    public string successText;
    public string failText;
}

[System.Serializable]
public class RunStateSnapshot
{
    public string sceneName;
    public LevelSO level;
    public int levelIndex;
    public bool[] exploredNodes;
    public PlayerStatusManager.PlayerStatusSnapshot playerStatus;
    public List<ItemSO> inventoryItems;
    public int currentTension;
}

[System.Serializable]
public struct StatRange
{
    public int min;
    public int max;

    public int Roll(float modifier = 1f)
    {
        int clampedMin = Mathf.Max(0, min);
        int clampedMax = Mathf.Max(clampedMin, max);
        int value = Random.Range(clampedMin, clampedMax + 1);
        return Mathf.Max(0, Mathf.RoundToInt(value * Mathf.Max(0.1f, modifier)));
    }
}
