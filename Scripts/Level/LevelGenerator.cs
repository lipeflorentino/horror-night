using UnityEngine;
using System.Collections.Generic;
public class LevelGenerator : MonoBehaviour
{
   public LevelDatabase database;
   public GameObject BaseLevelPrefab;

   public LevelDefinition GetRandomLevel(int currentTier)
   {
       List<LevelDefinition> valid = database.Levels.FindAll(c =>
           currentTier >= c.Tier_Min &&
           currentTier <= c.Tier_Max);

       int totalWeight = 0;
       
       foreach (var c in valid)
           totalWeight += c.Spawn_Weight;

       int roll = Random.Range(0, totalWeight);

       foreach (var c in valid)
       {
           roll -= c.Spawn_Weight;
           if (roll <= 0)
               return c;
       }

       return valid[0];
   }
}
