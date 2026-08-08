using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatView : MonoBehaviour
{
    public BattlerHUDView PlayerPanel;
    public BattlerHUDView EnemyPanel;
    public ActionPanelView ActionPanel;
    public FeedbackView FeedbackView;
    public DiceRollView DiceRollView;
    public DiceAllocationView DiceAllocationView;
    public CombatEndView CombatEndView;
    public CombatInfoPanelView InfoPanelView;
    public CombatLogView CombatLogView;
    public CastedTricksView CastedTricksView;

    private Sprite playerIconSprite;
    private Sprite enemyIconSprite;
    private CombatManager combatManager;

    public void Init(CombatManager combatManager)
    {
        this.combatManager = combatManager;
        BattlerHUDView[] panels = FindObjectsOfType<BattlerHUDView>();
        
        foreach (BattlerHUDView panel in panels)
        {
            string panelName = panel.gameObject.name.ToLowerInvariant();
            if (PlayerPanel == null && panelName.Contains("player"))
            {
                PlayerPanel = panel;
                continue;
            }

            if (EnemyPanel == null && panelName.Contains("enemy"))
            {
                EnemyPanel = panel;
            }
        }

        if (PlayerPanel == null && panels.Length > 0)
            PlayerPanel = panels[0];

        if (EnemyPanel == null && panels.Length > 1)
            EnemyPanel = panels[1];

        ActionPanel = FindObjectOfType<ActionPanelView>();
        FeedbackView = FindObjectOfType<FeedbackView>();
        DiceRollView = FindObjectOfType<DiceRollView>();
        DiceAllocationView = FindObjectOfType<DiceAllocationView>();
        CombatEndView = FindObjectOfType<CombatEndView>();
        InfoPanelView = FindObjectOfType<CombatInfoPanelView>();
        CombatLogView = FindObjectOfType<CombatLogView>();
        CastedTricksView = FindObjectOfType<CastedTricksView>();
            
        DiceRollView.HidePanel();
        CastedTricksView.Init(combatManager);
        FeedbackView.Init(combatManager.GetPerkService(), combatManager.GetTrickService(), combatManager.Player, combatManager.Enemy);

        playerIconSprite = combatManager.PlayerIcon;
        enemyIconSprite = combatManager.EnemyIcon;
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        ActionPanel.BindInput(inputHandler);
        DiceAllocationView.BindInput(inputHandler);
    }

    public void UpdateView(Battler player, Battler enemy)
    {
        PerkService perkService = combatManager != null ? combatManager.GetPerkService() : null;
        PlayerPanel.Bind(player, perkService);
        EnemyPanel.Bind(enemy, perkService);

        List<CombatInfoStatCalculator.StatRowEntry> playerStatRows = CombatInfoStatCalculator.BuildStatRows(player, enemy, perkService);
        List<CombatInfoStatCalculator.StatRowEntry> enemyStatRows = CombatInfoStatCalculator.BuildStatRows(enemy, player, perkService);
        InfoPanelView.Bind(player, playerIconSprite, playerStatRows, enemy, enemyIconSprite, enemyStatRows);
    }

    public void UpdateTurnOwner(bool isPlayerAttacker)
    {
        FeedbackView.ShowTurnStartFeedback(isPlayerAttacker);
    }

    public void ShowDiceCostFeedback(Dictionary<DiceStatType, int> costs)
    {
        FeedbackView.ShowDiceCostFeedback(costs);
    }

    public void RefreshActiveTricks()
    {
        if (CastedTricksView != null)
            CastedTricksView.Refresh();
    }

    public IEnumerator PlayDiceResolution(
        IReadOnlyList<DiceResult> playerRolls,
        IReadOnlyList<DiceResult> enemyRolls,
        DiceRollType rollType,
        (int lowMax, int mediumMax, int highMin, int maxValue) tierBoundaries)
    {
        if (DiceRollView  == null)
            yield break;

        yield return DiceRollView.PlayDiceResolution(playerRolls, enemyRolls, rollType, tierBoundaries);
    }

    public void HighlightSelectedAction(ActionInstance action)
    {
        ActionPanel.HighlightSelectedAction(action);
    }

    public void ShowResolveFeedback(ActionResolutionResult result, bool attackerIsPlayer)
    {
        FeedbackView.ShowResolveFeedback(result, attackerIsPlayer);
    }

    public void ShowAttackEffect(bool attackerIsPlayer)
    {
        FeedbackView.ShowAttackEffect(attackerIsPlayer);
    }

    public void ShowSkipTurnFeedback(bool isPlayerTurn)
    {
        FeedbackView.ShowSkipTurnFeedback(isPlayerTurn);
    }

    public void SetCombatInputEnabled(bool isEnabled)
    {
        if (ActionPanel != null)
            ActionPanel.SetAllInteractable(isEnabled);
    }

    public void SetInfoPanelVisible()
    {
        if (InfoPanelView != null)
            InfoPanelView.SetVisible(true);
    }

    public void ShowCombatLog(string logText, Sprite icon = null, Color? textColor = null)
    {
        if (CombatLogView != null)
            CombatLogView.ShowTextLog(logText, icon, textColor);
    }

    public void ShowTrickFeedback(TrickRuntimeInstance trick, string feedbackType)
    {
        if (CombatLogView != null)
            CombatLogView.ShowTrickFeedback(trick, feedbackType);
    }
}