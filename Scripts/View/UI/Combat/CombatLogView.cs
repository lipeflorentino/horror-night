using System.Collections.Generic;
using TMPro;

public class CombatLogView
{
    private readonly TMP_Text text;
    private readonly Queue<string> entries = new();
    private const int Max = 5;

    public CombatLogView(TMP_Text text)
    {
        this.text = text;
    }

    public void Add(string value)
    {
        entries.Enqueue(value);
        while (entries.Count > Max)
            entries.Dequeue();

        text.text = string.Join("\n", entries);
    }
}