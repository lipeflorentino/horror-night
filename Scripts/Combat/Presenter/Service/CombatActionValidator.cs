using System.Collections.Generic;
using System.Linq;

public class CombatActionValidator
{
    private readonly ActionDefinitionFactory actionDefinitionFactory;

    public CombatActionValidator()
    {
        actionDefinitionFactory = new ActionDefinitionFactory();
    }

    public bool ValidateAttackPhase(List<ActionInstance> queued)
    {
        return queued.Any(a => 
            a.definition.type == PlayerActionType.Attack || 
            a.definition.type == PlayerActionType.Investigate);
    }

    public bool ValidateDefensePhase(List<ActionInstance> queued)
    {
        return queued.Any(a => 
            a.definition.type == PlayerActionType.Defend);
    }

    public void EnforceMinimumActionAttackPhase(List<ActionInstance> queued)
    {
        if (!ValidateAttackPhase(queued))
        {
            var defaultAttack = new ActionInstance
            {
                definition = actionDefinitionFactory.CreateAttack(),
                allocatedDice = 0,
                allocatedHeart = 0,
                allocatedBody = 0,
                allocatedMind = 0
            };
            queued.Add(defaultAttack);
        }
    }

    public void EnforceMinimumActionDefensePhase(List<ActionInstance> queued)
    {
        if (!ValidateDefensePhase(queued))
        {
            var defaultDefend = new ActionInstance
            {
                definition = actionDefinitionFactory.CreateDefend(),
                allocatedDice = 0,
                allocatedHeart = 0,
                allocatedBody = 0,
                allocatedMind = 0
            };
            queued.Add(defaultDefend);
        }
    }
}
