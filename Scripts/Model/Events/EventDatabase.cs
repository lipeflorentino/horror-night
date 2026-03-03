using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event Database")]
public class EventDatabase : ScriptableObject
{
    public List<EventEntrySO> allEvents = new List<EventEntrySO>();

    public EventEntrySO GetRandom()
    {
        if (allEvents == null || allEvents.Count == 0)
            return null;

        return allEvents[Random.Range(0, allEvents.Count)];
    }
}
