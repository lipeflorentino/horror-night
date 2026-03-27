using UnityEngine;

public class PlayerBattler : MonoBehaviour
{
    public int heart;
    public int body;
    public int mind;

    public int attack;
    public int defense;
    public int criticalHitChance;
    public int parryChance;
    public int fleeChance;
    public int instantKillChance;
    public int learnChance;

    public void Setup(int baseHeart, int baseBody, int baseMind, TurnManagerStats combatStats)
    {
        heart = baseHeart;
        body = baseBody;
        mind = baseMind;

        attack = combatStats.attack;
        defense = combatStats.defense;
        criticalHitChance = combatStats.criticalHitChance;
        parryChance = combatStats.parryChance;
        fleeChance = combatStats.fleeChance;
        instantKillChance = combatStats.instantKillChance;
        learnChance = combatStats.learnChance;

        Debug.Log($"Setup Player! heart: {heart}, body: {body}, mind: {mind}, atk: {attack}, def: {defense}, crit: {criticalHitChance}");
    }
}
