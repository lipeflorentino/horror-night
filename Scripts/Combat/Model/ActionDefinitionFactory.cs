public class ActionDefinitionFactory
{
    public ActionDefinition CreateAttack()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.Attack,
            displayName = "Attack",
            baseDiceCost = 1,
            heartCost = 0,
            bodyCost = 1,
            mindCost = 0,
            requiresTarget = true,
            consumesItem = false,
            isDefensive = false
        };
    }

    public ActionDefinition CreateDefend()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.Defend,
            displayName = "Defend",
            baseDiceCost = 1,
            heartCost = 0,
            bodyCost = 1,
            mindCost = 0,
            requiresTarget = false,
            consumesItem = false,
            isDefensive = true
        };
    }

    public ActionDefinition CreateInvestigate()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.Investigate,
            displayName = "Investigate",
            baseDiceCost = 1,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0,
            requiresTarget = false,
            consumesItem = false,
            isDefensive = false
        };
    }

    public ActionDefinition CreateUseItem()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.UseItem,
            displayName = "Use Item",
            baseDiceCost = 0,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0,
            requiresTarget = false,
            consumesItem = true,
            isDefensive = false
        };
    }

    public ActionDefinition CreateUseSkill()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.UseSkill,
            displayName = "Use Skill",
            baseDiceCost = 0,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0,
            requiresTarget = false,
            consumesItem = false,
            isDefensive = false
        };
    }

    public ActionDefinition CreateFlee()
    {
        return new ActionDefinition
        {
            type = PlayerActionType.Flee,
            displayName = "Flee",
            baseDiceCost = 1,
            heartCost = 0,
            bodyCost = 0,
            mindCost = 0,
            requiresTarget = false,
            consumesItem = false,
            isDefensive = true
        };
    }
}
