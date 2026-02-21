using UnityEngine;
public class RunManager : MonoBehaviour
{
   public static RunManager Instance;
   public int CurrentTier = 1;
   [SerializeField] private LevelGenerator generator;
   [SerializeField] private Transform levelSpawnPoint;
   private LevelInstance currentLevel;

   void Awake()
   {
       Instance = this;
   }

   public void StartRun()
   {
       GenerateNewLevel();
   }

   public void GenerateNewLevel()
   {
       if (currentLevel != null)
           Destroy(currentLevel.gameObject);

       LevelDefinition def = generator.GetRandomLevel(CurrentTier);
       GameObject obj = Instantiate(generator.BaseLevelPrefab, levelSpawnPoint.position, Quaternion.identity);
       currentLevel = obj.GetComponent<LevelInstance>();
       currentLevel.Initialize(def);
   }

   public void IncreaseTier()
   {
       CurrentTier++;
       GenerateNewLevel();
   }
}
