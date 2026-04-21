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
        DicePanelView.HidePanel();
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        ActionPanel.BindInput(inputHandler);
    }

    public void UpdateView(Battler player, Battler enemy)
    {
        PlayerPanel.Bind(player);
        EnemyPanel.Bind(enemy);
    }

    public void UpdateAddDiceAttackCount(int count)
    {
        ActionPanel.UpdateAddDiceAttackCount(count);
    }

    public void UpdateAddDiceDefenseCount(int count)
    {
        ActionPanel.UpdateAddDiceDefenseCount(count);
    }

    public void UpdateTurnOwner(bool isPlayerAttacker)
    {
        FeedbackView.ShowTurnStartFeedback(isPlayerAttacker);
    }

    public IEnumerator PlayDiceResolution(IReadOnlyList<DiceResult> playerRolls, IReadOnlyList<DiceResult> enemyRolls)
    {
        if (DicePanelView  == null)
            yield break;

        yield return DicePanelView.PlayDiceResolution(playerRolls, enemyRolls);
    }
}