using UnityEngine;
public class LevelInstance : MonoBehaviour
{
   public LevelDefinition Definition;

   public void Initialize(LevelDefinition def)
   {
       Definition = def;
       ApplyDefinition();
   }

   void ApplyDefinition()
   {
       TensionSystem.Instance.SetBaseModifier(Definition.Base_Tension_Modifier);
       PresenceSystem.Instance.SetBaseModifier(Definition.Presence_Growth_Modifier);
       EncounterSystem.Instance.SetRiskModifier(Definition.Encounter_Risk_Modifier);
       VisualSystem.Instance.ApplyTags(Definition.Tags);
   }
}
