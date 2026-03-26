using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct StatModifier
{
    [FormerlySerializedAs("life")] public int heart;
    public int physical;
    [FormerlySerializedAs("sanity")] public int mind;
    [FormerlySerializedAs("power")] public int physicalBonus;
}

public struct OccurrenceResult
{
    public bool success;
    public int occurrenceRoll;
    public int playerRoll;
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
