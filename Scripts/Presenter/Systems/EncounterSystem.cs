using UnityEngine;
public class EncounterSystem : MonoBehaviour
{
   public static EncounterSystem Instance;
   private float riskModifier = 1f;

   void Awake()
   {
       Instance = this;
   }

   public void SetRiskModifier(float value)
   {
       riskModifier = value;
   }

   public void TriggerEncounter()
   {
       CombatManager.Instance.StartCombat(riskModifier);
   }

   public void TriggerForcedEncounter()
   {
       CombatManager.Instance.StartCombat(riskModifier * 1.5f);
   }
}
