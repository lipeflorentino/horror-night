using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
public class CharacterSO : ScriptableObject
{
    public const float DefaultHeart = 20f;
    public const float DefaultBody = 20f;
    public const float DefaultMind = 20f;
    public const float DefaultHp = 100f;
    public const int DefaultAttack = 10;
    public const int DefaultDefense = 5;
    public const int DefaultInitiative = 10;
    public const int DefaultPowerDices = 3;
    public const int DefaultAccuracyDices = 3;
    public const int DefaultXpThreshold = 10;

    [Header("Identity")]
    public string Id;
    public string DisplayName = "Player";
    public Sprite CharacterIcon;
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

    public void ApplyDefaults()
    {
        if (IdentityTricks == null)
            IdentityTricks = new List<TrickSO>();

        if (string.IsNullOrWhiteSpace(Id))
        {
            Id = string.IsNullOrWhiteSpace(DisplayName)
                ? "character"
                : DisplayName.Replace(" ", "_").ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = "Player";

        Xp = Mathf.Max(0, Xp <= 0 ? DefaultXpThreshold : Xp);
        Heart = Mathf.Max(1f, Heart <= 0f ? DefaultHeart : Heart);
        Body = Mathf.Max(1f, Body <= 0f ? DefaultBody : Body);
        Mind = Mathf.Max(1f, Mind <= 0f ? DefaultMind : Mind);
        Hp = Mathf.Max(1f, Hp <= 0f ? DefaultHp : Hp);
        Attack = Mathf.Max(0, Attack <= 0 ? DefaultAttack : Attack);
        Defense = Mathf.Max(0, Defense <= 0 ? DefaultDefense : Defense);
        Initiative = Mathf.Max(0, Initiative <= 0 ? DefaultInitiative : Initiative);
        Focus = Mathf.Max(0, Focus);
        Strength = Mathf.Max(0, Strength);
        Agility = Mathf.Max(0, Agility);
        PowerDices = Mathf.Max(1, PowerDices <= 0 ? DefaultPowerDices : PowerDices);
        AccuracyDices = Mathf.Max(1, AccuracyDices <= 0 ? DefaultAccuracyDices : AccuracyDices);
    }
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
