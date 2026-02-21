using UnityEngine;
public class CombatManager : MonoBehaviour
{
   public static CombatManager Instance;

   void Awake()
   {
       Instance = this;
   }

   public void StartCombat(float difficultyModifier)
   {
       Debug.Log("Combat started with modifier: " + difficultyModifier);

       // Aqui você carrega inimigos baseados em:
       // - Tier
       // - Tags da clareira
       // - Difficulty modifier
   }
}
