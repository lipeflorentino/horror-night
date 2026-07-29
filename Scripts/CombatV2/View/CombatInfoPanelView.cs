using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CombatInfoPanelView : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Player Profile")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerLvlText;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Transform playerStatsContainer;
    [SerializeField] private Image playerIcon;

    [Header("Enemy Profile")]
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyLvlText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Transform enemyStatsContainer;
    [SerializeField] private Image enemyIcon;

    [Header("Stat Row Prefab")]
    [SerializeField] private StatRowUI statRowPrefab;

    // Cache to avoid repeated Resources.Load calls for the same icon.
    private static readonly Dictionary<string, Sprite> _iconCache = new();

    // Row pools per container, reused across Bind() calls to avoid Instantiate/Destroy churn.
    private readonly List<StatRowUI> _playerRowPool = new();
    private readonly List<StatRowUI> _enemyRowPool = new();

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    // Stat rows arrive pre-computed (Presenter-side, via CombatInfoStatCalculator) so this View
    // only renders — it no longer touches PerkService or decides how values are calculated.
    public void Bind(
        Battler player, Sprite playerSprite, IReadOnlyList<CombatInfoStatCalculator.StatRowEntry> playerStatRows,
        Battler enemy, Sprite enemySprite, IReadOnlyList<CombatInfoStatCalculator.StatRowEntry> enemyStatRows)
    {
        if (player == null || enemy == null)
        {
            Debug.LogWarning("[CombatInfoPanelView] Bind called with null player or enemy Battler.");
            return;
        }

        if (playerNameText != null)
            playerNameText.text = player.Name;

        if (playerLvlText != null)
            playerLvlText.text = player.Level.ToString();

        if (playerHpText != null)
            playerHpText.text = $"{player.HP}/{player.MaxHp}";

        if (playerIcon != null)
            playerIcon.sprite = playerSprite;

        if (enemyNameText != null)
            enemyNameText.text = enemy.Name;

        if (enemyLvlText != null)
            enemyLvlText.text = enemy.Level.ToString();

        if (enemyHpText != null)
            enemyHpText.text = $"{enemy.HP}/{enemy.MaxHp}";

        if (enemyIcon != null)
            enemyIcon.sprite = enemySprite;

        BuildStatRows(playerStatRows, playerStatsContainer, _playerRowPool);
        BuildStatRows(enemyStatRows, enemyStatsContainer, _enemyRowPool);
    }

    // Reuses pooled StatRowUI instances instead of Instantiate/Destroy on every Bind().
    private void BuildStatRows(IReadOnlyList<CombatInfoStatCalculator.StatRowEntry> rows, Transform container, List<StatRowUI> pool)
    {
        if (container == null || statRowPrefab == null || rows == null)
        {
            Debug.LogWarning("[CombatInfoPanelView] Missing container, prefab, or stat rows data.");
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            StatRowUI row = GetOrCreatePooledRow(pool, container, i);
            CombatInfoStatCalculator.StatRowEntry entry = rows[i];
            row.Bind(IconProvider.GetStatIcon(entry.Key), entry.Label, entry.Value, entry.DeltaText, entry.ShowDelta, entry.PositiveDelta);
            row.gameObject.SetActive(true);
        }

        for (int i = rows.Count; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private StatRowUI GetOrCreatePooledRow(List<StatRowUI> pool, Transform container, int index)
    {
        if (index < pool.Count)
            return pool[index];

        StatRowUI row = Instantiate(statRowPrefab, container);
        pool.Add(row);
        return row;
    }

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    private void Close()
    {
        SetVisible(false);
    }
}