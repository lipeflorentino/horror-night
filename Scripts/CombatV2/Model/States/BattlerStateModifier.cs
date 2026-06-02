using System;

[Serializable]
public abstract class BattlerStateModifier
{
    public BattlerStateRole Role = BattlerStateRole.OwnerAsActor;
    public ActionType ActionType;
    public bool FilterByActionType;

    public bool MatchesAction(ActionType actionType)
    {
        return !FilterByActionType || ActionType == actionType;
    }
}
