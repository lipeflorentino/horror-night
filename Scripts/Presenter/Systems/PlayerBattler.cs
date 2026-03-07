using UnityEngine;

public class PlayerBattler : MonoBehaviour
{
    public int life;
    public int physical;
    public int mental;

    public void Setup(int baseLife, int basePhysical, int baseMental)
    {
        life = baseLife;
        physical = basePhysical;
        mental = baseMental;
    }
}
