using UnityEngine;

public class CombatView : MonoBehaviour
{
    public BattlerPanelView playerPanel;
    public BattlerPanelView enemyPanel;

    public ActionPanelView actionPanel;

    public void UpdateView(Battler player, Battler enemy)
    {
        playerPanel.Bind(player);
        enemyPanel.Bind(enemy);
    }
}