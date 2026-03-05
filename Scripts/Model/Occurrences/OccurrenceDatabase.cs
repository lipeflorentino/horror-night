using System.Collections.Generic;
using UnityEngine;
public class OccurrenceDatabase : MonoBehaviour
{
    public List<OccurrenceSO> allOccurrences = new();

    private void Awake()
    {
        LoadAll();
    }

    void LoadAll()
    {
        OccurrenceSO[] loaded = Resources.LoadAll<OccurrenceSO>("Data/Occurrences");

        allOccurrences.Clear();
        allOccurrences.AddRange(loaded);
    }

    public OccurrenceSO GetRandom()
    {
        if (allOccurrences == null || allOccurrences.Count == 0)
            return null;

        return allOccurrences[Random.Range(0, allOccurrences.Count)];
    }
}
