using UnityEngine;

public class PlayerBattler : MonoBehaviour
{
    public int heart;
    public int body;
    public int mind;

    public int attack;
    public int defense;
    public int initiative;
    public int criticalHitChance;
    public int parryChance;
    public int fleeChance;
    public int instantKillChance;
    public int learnChance;

    public void Setup(int baseHeart, int baseBody, int baseMind)
    {
        heart = baseHeart;
        body = baseBody;
        mind = baseMind;

        Debug.Log($"Setup Player! heart: {heart}, body: {body}, mind: {mind}");
    }
}
