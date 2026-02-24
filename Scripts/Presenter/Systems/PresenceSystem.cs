using UnityEngine;
public class PresenceSystem : MonoBehaviour
{
   public static PresenceSystem Instance;
   private float baseModifier = 1f;
   private float presenceValue;

   void Awake()
   {
       Instance = this;
   }

   public void SetBaseModifier(float value)
   {
       baseModifier = value;
       presenceValue = 0;
   }

   public void Tick()
   {
       presenceValue += Time.deltaTime * baseModifier;

       if (presenceValue > 20f)
       {
           EncounterSystem.Instance.TriggerForcedEncounter();
           presenceValue = 0;
       }
   }
}
