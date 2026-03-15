using System.Collections.Generic;

[System.Serializable]
public struct StatModifier { 
    public int life; 
    public int physical; 
    public int sanity; 
    public int power; 
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