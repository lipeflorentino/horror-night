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

        int totalAvailableDice = enemy.CurrentPowerDices + enemy.CurrentAccuracyDices;
        int allocatedAccuracyDice = 1;
        int allocatedPowerDice = Mathf.Max(1, totalAvailableDice - 1);
        
        allocatedAccuracyDice = Mathf.Min(allocatedAccuracyDice, enemy.CurrentAccuracyDices);
        allocatedPowerDice = Mathf.Min(allocatedPowerDice, enemy.CurrentPowerDices);
        
        List<DiceStatType> accuracyTypes = BuildStrategyStatTypeList(enemy, allocatedAccuracyDice);
        List<DiceStatType> powerTypes = BuildStrategyStatTypeList(enemy, allocatedPowerDice);

        enemy.CurrentAccuracyDices = Mathf.Max(enemy.CurrentAccuracyDices - allocatedAccuracyDice, 0);
        enemy.CurrentPowerDices = Mathf.Max(enemy.CurrentPowerDices - allocatedPowerDice, 0);

        return new EnemyTurnPlan
        {
            Action = action,
            PowerDiceTypes = powerTypes,
            AccuracyDiceTypes = accuracyTypes
        };
    }

    private static List<DiceStatType> BuildStrategyStatTypeList(Battler enemy, int count)
    {
        List<DiceStatType> types = new();
        int safeCount = Mathf.Max(0, count);
        
        DiceStatType strategicType = ChooseStrategicStatType(enemy);
        
        for (int i = 0; i < safeCount; i++)
            types.Add(strategicType);

        return types;
    }

    private static DiceStatType ChooseStrategicStatType(Battler enemy)
    {
        int mindValue = enemy.Mind;
        int heartValue = enemy.Heart;
        int bodyValue = enemy.Body;
        
        int maxValue = Mathf.Max(mindValue, Mathf.Max(heartValue, bodyValue));

        if (mindValue == maxValue)
            return DiceStatType.Mind;
        if (heartValue == maxValue)
            return DiceStatType.Heart;
        
        return DiceStatType.Body;
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