using System;
using System.Diagnostics;

public class CombatUI
{
    public Action<string, bool> Feedback;
    public Action<int> Hud;
    public Action<string, CombatLogStyle> Log;

    public void ShowFeedback(string text, bool popup)
    {
        Feedback?.Invoke(text, popup);
    }

    public void UpdateHud(int availableDice)
    {
        Debug.Print($"availableDice: {availableDice}");
        Hud?.Invoke(availableDice);
    }

    public void AddLog(string text, CombatLogStyle style)
    {
        Log?.Invoke(text, style);
    }
}
