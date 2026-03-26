using UnityEngine;

public class PlayerBattler : MonoBehaviour
{
    public int life;
    public int physical;
    public int mental;

    public int attack;
    public int defense;
    public int criticalHitChance;
    public int parryChance;
    public int fleeChance;
    public int instantKillChance;
    public int learnChance;

    public void Setup(int baseLife, int basePhysical, int baseMental, TurnManagerStats combatStats)
    {
        life = baseLife;
        physical = basePhysical;
        mental = baseMental;

        attack = combatStats.attack;
        defense = combatStats.defense;
        criticalHitChance = combatStats.criticalHitChance;
        parryChance = combatStats.parryChance;
        fleeChance = combatStats.fleeChance;
        instantKillChance = combatStats.instantKillChance;
        learnChance = combatStats.learnChance;

        Debug.Log($"Setup Player! life: {life}, physical: {physical}, mental: {mental}, atk: {attack}, def: {defense}, crit: {criticalHitChance}");
    }
}
