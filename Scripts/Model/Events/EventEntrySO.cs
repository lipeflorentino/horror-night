using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event Entry")]
public class EventEntrySO : ScriptableObject
{
    public int id;
    public string title;
    [TextArea] public string description;
    [TextArea] public string successText;
    [TextArea] public string failText;
    public string successStat;
    public int successValue;
    public string failStat;
    public int failValue;
    public int rollRange;
}
