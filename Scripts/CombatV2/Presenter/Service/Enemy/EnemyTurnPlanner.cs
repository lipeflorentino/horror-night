using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnPlan
{
    public ActionInstance Action;
    public List<DiceStatType> PowerDiceTypes;
    public List<DiceStatType> AccuracyDiceTypes;
}

public class EnemyTurnPlanner
{
    private readonly EnemyActionSelector enemyActionSelector;

    public EnemyTurnPlanner(EnemyActionSelector selector)
    {
        enemyActionSelector = selector;
    }

    public EnemyTurnPlan BuildPlan(Battler enemy, EnemyInstance enemySnapshot, ActionDefinition attackDef, ActionDefinition defenseDef)
    {
        ActionInstance action = enemyActionSelector.Select(attackDef, defenseDef);
        int totalEnemyPowerDice = Mathf.Max(1, enemy.CurrentPowerDices);
        int totalEnemyAccuracyDice = Mathf.Max(1, enemy.CurrentAccuracyDices);
        int enemyAllocatedPowerDice = Mathf.Clamp(Random.Range(1, totalEnemyPowerDice + 1), 1, totalEnemyPowerDice);
        int enemyAllocatedAccuracyDice = Mathf.Clamp(Random.Range(1, totalEnemyAccuracyDice + 1), 1, totalEnemyAccuracyDice);
        int allocatedPowerDice = Mathf.Clamp(Random.Range(0, enemyAllocatedPowerDice + 1), 0, enemyAllocatedPowerDice);
        int allocatedAccuracyDice = Mathf.Clamp(Random.Range(0, enemyAllocatedAccuracyDice + 1), 0, enemyAllocatedAccuracyDice);

        if (allocatedPowerDice == 0 && allocatedAccuracyDice == 0)
            allocatedAccuracyDice = 1;

        List<DiceStatType> powerTypes = BuildStatTypeList(enemySnapshot, allocatedPowerDice, true);
        List<DiceStatType> accuracyTypes = BuildStatTypeList(enemySnapshot, allocatedAccuracyDice, false);

        enemy.CurrentPowerDices = Mathf.Max(enemy.CurrentPowerDices - enemyAllocatedPowerDice, 0);
        enemy.CurrentAccuracyDices = Mathf.Max(enemy.CurrentAccuracyDices - enemyAllocatedAccuracyDice, 0);

        return new EnemyTurnPlan
        {
            Action = action,
            PowerDiceTypes = powerTypes,
            AccuracyDiceTypes = accuracyTypes
        };
    }

    private static List<DiceStatType> BuildStatTypeList(EnemyInstance snapshot, int count, bool forPower)
    {
        List<DiceStatType> types = new();
        int safeCount = Mathf.Max(0, count);
        DiceStatType chosenType = ChoosePlaceholderType(snapshot, forPower);
        for (int i = 0; i < safeCount; i++)
            types.Add(chosenType);

        return types;
    }

    private static DiceStatType ChoosePlaceholderType(EnemyInstance snapshot, bool forPower)
    {
        DiceStatType defaultPlaceholderType = forPower ? DiceStatType.Body : DiceStatType.Mind;
        
        if (snapshot == null || snapshot.source == null || snapshot.source.tags == null) return defaultPlaceholderType;

        EnemyTagSet tags = snapshot.source.tags;

        return tags.style switch
        {
            EnemyStyleTag.Stealthy => forPower ? DiceStatType.Mind : DiceStatType.Mind,
            EnemyStyleTag.Brutal => DiceStatType.Body,
            EnemyStyleTag.Mental => DiceStatType.Mind,
            EnemyStyleTag.Ritualistic => DiceStatType.Heart,
            EnemyStyleTag.Primal => DiceStatType.Heart,
            _ => DiceStatType.Body
        };
    }
}