using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatView : MonoBehaviour
{
    public BattlerPanelView PlayerPanel;
    public BattlerPanelView EnemyPanel;
    public ActionPanelView ActionPanel;
    public FeedbackView FeedbackView;
    public DicePanelView DicePanelView;
    public CombatEndView CombatEndView;
    public CombatInfoPanelView InfoPanelView;

    public void Init()
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
        DicePanelView = FindObjectOfType<DicePanelView>();
        CombatEndView = FindObjectOfType<CombatEndView>();
        InfoPanelView = FindObjectOfType<CombatInfoPanelView>();
        DicePanelView.HidePanel();
        InfoPanelView.Init();
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        ActionPanel.BindInput(inputHandler);
    }

    public void UpdateView(Battler player, Battler enemy)
    {
        PlayerPanel.Bind(player);
        EnemyPanel.Bind(enemy);
        InfoPanelView.Bind(player, enemy);
    }

    public void UpdateTurnOwner(bool isPlayerAttacker)
    {
        FeedbackView.ShowTurnStartFeedback(isPlayerAttacker);
    }

    public IEnumerator PlayDiceResolution(
        IReadOnlyList<DiceResult> playerPowerRolls,
        IReadOnlyList<DiceResult> playerAccuracyRolls,
        IReadOnlyList<DiceResult> enemyPowerRolls,
        IReadOnlyList<DiceResult> enemyAccuracyRolls)
    {
        if (DicePanelView  == null)
            yield break;

        yield return DicePanelView.PlayDiceResolution(playerPowerRolls, playerAccuracyRolls, enemyPowerRolls, enemyAccuracyRolls);
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

    public void SetInfoPanelVisible(bool isVisible)
    {
        if (InfoPanelView != null)
            InfoPanelView.SetVisible(isVisible);
    }
}
