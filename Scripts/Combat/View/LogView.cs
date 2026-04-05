using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LogView : MonoBehaviour
{
    [SerializeField] private TMP_Text logText;

    private const int MaxEntries = 5;
    private readonly Queue<string> entries = new();

    public void Add(string text, CombatLogStyle style)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string formattedEntry = Format(text, style);

        entries.Enqueue(formattedEntry);

        while (entries.Count > MaxEntries)
            entries.Dequeue();

        RefreshText();
    }

    private string Format(string text, CombatLogStyle style)
    {
        return style switch
        {
            CombatLogStyle.Positive => WrapColor(text, "#8BFF8B"),
            CombatLogStyle.Negative => WrapColor(text, "#FF8B8B"),
            CombatLogStyle.Info => WrapColor(text, "#8BCBFF"),
            _ => text,
        };
    }

    private static string WrapColor(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    private void RefreshText()
    {
        if (logText == null)
            return;

        StringBuilder builder = new();

        foreach (string entry in entries)
        {
            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append(entry);
        }

        logText.text = builder.ToString();
    }
}
