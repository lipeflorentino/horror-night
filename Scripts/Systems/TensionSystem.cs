using UnityEngine;
public class TensionSystem : MonoBehaviour
{
   public static TensionSystem Instance;
   private float baseModifier = 1f;
   public int CurrentTension;

   void Awake()
   {
       Instance = this;
   }

   public void SetBaseModifier(float value)
   {
       baseModifier = value;
       CurrentTension = 0;
   }

   public void OnPlayerMove()
   {
       CurrentTension += Mathf.RoundToInt(1 * baseModifier);

       if (CurrentTension >= GetEncounterThreshold())
       {
           EncounterSystem.Instance.TriggerEncounter();
           CurrentTension = 0;
       }
   }

   int GetEncounterThreshold()
   {
       return 5 + RunManager.Instance.CurrentTier;
   }
}
