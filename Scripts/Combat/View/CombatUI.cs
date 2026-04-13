using System;

public class CombatUI
{
    public Action<string, bool> Feedback;
    public Action<int> Hud;
    public Action<string, CombatLogStyle> Log;
    public Action<string> TurnText;
    public Action<string> ActionQueued;
    public Action InventorySelectionRequested;
    public Action SkillSelectionRequested;

    public void ShowFeedback(string text, bool popup)
    {
        Feedback?.Invoke(text, popup);
    }

    public void UpdateHud(int availableDice)
    {
        Hud?.Invoke(availableDice);
    }

    public void AddLog(string text, CombatLogStyle style)
    {
        Log?.Invoke(text, style);
    }
    
    public void SetTurnText(string text)
    {
        TurnText?.Invoke(text);
    }

    public void NotifyActionQueued(string text)
    {
        ActionQueued?.Invoke(text);
    }

    public void RequestInventorySelection()
    {
        InventorySelectionRequested?.Invoke();
    }

    public void RequestSkillSelection()
    {
        SkillSelectionRequested?.Invoke();
    }
}
