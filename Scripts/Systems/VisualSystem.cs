using UnityEngine;
public class VisualSystem : MonoBehaviour
{
   public static VisualSystem Instance;

   void Awake()
   {
       Instance = this;
   }

   public void ApplyTags(string tags)
   {
       if (tags.Contains("mental"))
           EnableFogEffect();

       if (tags.Contains("sombra"))
           DarkenEnvironment();
   }

   void EnableFogEffect() { }
   void DarkenEnvironment() { }
}
