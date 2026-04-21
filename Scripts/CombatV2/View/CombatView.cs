using UnityEngine;

public class CombatView : MonoBehaviour
{
    public BattlerPanelView PlayerPanel;
    public BattlerPanelView EnemyPanel;
    public ActionPanelView ActionPanel;
    public FeedbackView FeedbackView;

    public void Init()
    {
        PlayerPanel = FindObjectOfType<BattlerPanelView>();
        EnemyPanel = FindObjectOfType<BattlerPanelView>();
        ActionPanel = FindObjectOfType<ActionPanelView>();
        FeedbackView = FindObjectOfType<FeedbackView>();
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
}