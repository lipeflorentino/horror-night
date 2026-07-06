using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatView : MonoBehaviour
{
    public BattlerPanelView PlayerPanel;
    public BattlerPanelView EnemyPanel;
    public ActionPanelView ActionPanel;
    public FeedbackView FeedbackView;
    public DiceRollView DiceRollView;
    public DiceAllocationView DiceAllocationView;
    public CombatEndView CombatEndView;
    public CombatInfoPanelView InfoPanelView;
    public CombatLogView CombatLogView;
    public ActiveTricksView ActiveTricksView;

    private Sprite playerIconSprite;
    private Sprite enemyIconSprite;

    public void Init(CombatManager combatManager)
    {
        BattlerPanelView[] panels = FindObjectsOfType<BattlerPanelView>();
        
        foreach (BattlerPanelView panel in panels)
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
        ActiveTricksView = FindObjectOfType<ActiveTricksView>();
            
        DiceRollView.HidePanel();
        ActiveTricksView.Init(combatManager);
        FeedbackView.Init(combatManager.GetPerkService());

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
        PlayerPanel.Bind(player);
        EnemyPanel.Bind(enemy);
        InfoPanelView.Bind(player, playerIconSprite, enemy, enemyIconSprite);
    }

    public void UpdateTurnOwner(bool isPlayerAttacker)
    {
        FeedbackView.ShowTurnStartFeedback(isPlayerAttacker);
    }

    public void RefreshActiveTricks()
    {
        if (ActiveTricksView != null)
            ActiveTricksView.Refresh();
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

    public void ShowResolveFeedback(ActionResolutionResult result, bool targetIsPlayer)
    {
        FeedbackView.ShowResolveFeedback(result, targetIsPlayer);
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
}
