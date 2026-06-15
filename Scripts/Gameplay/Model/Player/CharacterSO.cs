using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
public class CharacterSO : ScriptableObject
{
    [Header("Identity")]
    public string Id;
    public string DisplayName = "Player";
    [TextArea(2, 4)] public string Description;
    [Header("Progression Defaults")]
    [Min(0)] public int Xp = 0;

    [Header("Core Stats Defaults")]
    [Min(1f)] public float Heart = 20f;
    [Min(1f)] public float Body = 20f;
    [Min(1f)] public float Mind = 20f;
    [Min(1f)] public float Hp = 100f;

    [Header("Combat Stats")]
    [Min(0)] public int Attack = 10;
    [Min(0)] public int Defense = 5;
    [Min(0)] public int Initiative = 10;
    [Min(0)] public int Focus = 0;
    [Min(0)] public int Strength = 0;
    [Min(0)] public int Agility = 0;
    [Min(1)] public int PowerDices = 3;
    [Min(1)] public int AccuracyDices = 3;

    [Header("Tricks")]
    [Tooltip("Tricks permanentes da identidade/classe do personagem. Entram no combate ativas como Identity Tricks.")]
    public List<TrickSO> IdentityTricks = new();
    public TrickInventorySnapshot CreateInitialTrickSnapshot()
    {
        TrickInventorySnapshot snapshot = new();
        AddTrickIds(IdentityTricks, snapshot.identityTrickIds);
        return snapshot;
    }

    private static void AddTrickIds(List<TrickSO> source, List<string> target)
    {
        if (source == null || target == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            string id = source[i]?.Id;
            if (!string.IsNullOrWhiteSpace(id) && !target.Contains(id))
                target.Add(id);
        }
    }
}
