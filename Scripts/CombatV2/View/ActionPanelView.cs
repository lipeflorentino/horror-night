using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button attackButton;
    public Button defendButton;

    public Button addDiceAttackButton;
    public Button addDiceDefenseButton;

    public Button endTurnButton;

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        attackButton.gameObject.SetActive(isPlayerAttacker);
        addDiceAttackButton.gameObject.SetActive(isPlayerAttacker);

        defendButton.gameObject.SetActive(!isPlayerAttacker);
        addDiceDefenseButton.gameObject.SetActive(!isPlayerAttacker);
    }
}
