using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void StartCombat(float difficultyModifier)
    {
        StartCombat(null, difficultyModifier);
    }

    public void StartCombat(EnemyInstance enemy, float difficultyModifier)
    {
        if (enemy == null)
        {
            Debug.Log("Combat started with no enemy selected. Modifier: " + difficultyModifier);
            return;
        }

        Debug.Log($"Combat started vs {enemy.source.enemyName} ({enemy.source.archetype}) | Life: {enemy.life} Physical: {enemy.physical} Mental: {enemy.mental} | Modifier: {difficultyModifier}");
    }
}
