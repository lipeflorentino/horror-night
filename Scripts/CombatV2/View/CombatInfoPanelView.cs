using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatInfoPanelView : MonoBehaviour
{
    private const string StatsIconResourcePath = "UI/Battler/Stats/{0}Icon";

    [SerializeField] private GameObject panelRoot;

    [Header("Player Profile")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerLvlText;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Transform playerStatsContainer;

    [Header("Enemy Profile")]
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyLvlText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Transform enemyStatsContainer;

    [Header("Stat Row Prefab")]
    [SerializeField] private StatRowUI statRowPrefab;

    // Cache to avoid repeated Resources.Load calls for the same icon.
    private static readonly Dictionary<string, Sprite> _iconCache = new();

    // Ordered stat definitions: resource key (used for icon lookup), display label (Pt-BR), value getter.
    private static readonly (string Key, string Label, Func<Battler, string> GetValue)[] _statDefinitions =
    {
        ("Atk", "Attack", b => b.Attack.ToString()),
        ("Def", "Defense", b => b.Defense.ToString()),
        ("Mind", "Mind", b => b.Mind.ToString()),
        ("Heart", "Heart", b => b.Heart.ToString()),
        ("Body", "Body", b => b.Body.ToString()),
        ("Init", "Initiative", b => b.Initiative.ToString()),
        ("Focus", "Focus", b => b.Focus.ToString()),
        ("Str", "Strength", b => b.Strength.ToString()),
        ("Agi", "Agility", b => b.Agility.ToString()),
        ("PowerDices", "Power dices", b => $"{b.CurrentPowerDices}/{b.MaxPowerDices}"),
        ("AccuracyDices", "Accuracy dices", b => $"{b.CurrentAccuracyDices}/{b.MaxAccuracyDices}"),
    };

    public void Bind(Battler player, Battler enemy)
    {
        if (playerNameText != null)
            playerNameText.text = player.Name;

        if (playerLvlText != null)
            playerLvlText.text = player.Level.ToString();

        if (playerHpText != null)
            playerHpText.text = $"{player.HP}/{player.MaxHp}";

        if (enemyNameText != null)
            enemyNameText.text = enemy.Name;

        if (enemyLvlText != null)
            enemyLvlText.text = enemy.Level.ToString();

        if (enemyHpText != null)
            enemyHpText.text = $"{enemy.HP}/{enemy.MaxHp}";

        BuildStatRows(player, playerStatsContainer);
        BuildStatRows(enemy, enemyStatsContainer);
    }

    private void BuildStatRows(Battler battler, Transform container)
    {
        if (container == null || statRowPrefab == null || battler == null)
        {
            Debug.LogWarning("[CombatInfoPanelView] Missing container, prefab, or battler reference for stat rows.");
            return;
        }

        ClearContainer(container);

        foreach (var (Key, Label, GetValue) in _statDefinitions)
        {
            StatRowUI row = Instantiate(statRowPrefab, container);
            row.Bind(GetStatIcon(Key), Label, GetValue(battler));
        }
    }

    private static void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    private static Sprite GetStatIcon(string statKey)
    {
        if (_iconCache.TryGetValue(statKey, out Sprite cachedSprite))
            return cachedSprite;

        string path = string.Format(StatsIconResourcePath, statKey);
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[CombatInfoPanelView] Icon not found at Resources/{path}.png");

        _iconCache[statKey] = sprite;
        return sprite;
    }

    public void SetVisible()
    {
        if (panelRoot != null)
            panelRoot.SetActive(!panelRoot.activeSelf);
        else
            gameObject.SetActive(!gameObject.activeSelf);
    }
}