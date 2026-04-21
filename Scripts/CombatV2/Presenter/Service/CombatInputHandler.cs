using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private CombatManager Combat;
    private int AttackDiceAllocated = 0;
    private int DefenseDiceAllocated = 0;
    private ActionType? SelectedAction = null;
    private ActionType AllowedAction = ActionType.Attack;

    public void Init(CombatManager cm)
    {
        Combat = cm;
    }

    public void UpdateCombatView()
    {
        Combat.View.UpdateView(Combat.Player, Combat.Enemy);
    }

    public void SetAllowedAction(ActionType allowedAction)
    {
        AllowedAction = allowedAction;
        SelectedAction = allowedAction;

        if (allowedAction == ActionType.Attack)
        {
            DefenseDiceAllocated = 0;
        }
        else
        {
            AttackDiceAllocated = 0;
        }

        Debug.Log($"[Input] Turn role updated. Allowed action: {AllowedAction}");
    }

    public void OnAttack()
    {
        if (AllowedAction != ActionType.Attack)
        {
            Debug.Log("[Input] Attack is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Attack;
        Debug.Log("[Input] Selected ATTACK");
    }

    public void OnDefend()
    {
        if (AllowedAction != ActionType.Defense)
        {
            Debug.Log("[Input] Defense is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Defense;
        Debug.Log("[Input] Selected DEFENSE");
    }

    public void OnAddDiceToAttack()
    {
        if (AllowedAction != ActionType.Attack) return;
        if (Combat.Player.CurrentDices <= 0) return;

        AttackDiceAllocated++;
        Combat.Player.CurrentDices--;
        Combat.View.UpdateAddDiceAttackCount(AttackDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Added dice to ATTACK: {AttackDiceAllocated}");
    }

    public void OnAddDiceToDefense()
    {
        if (AllowedAction != ActionType.Defense) return;
        if (Combat.Player.CurrentDices <= 0) return;

        DefenseDiceAllocated++;
        Combat.Player.CurrentDices--;
        Combat.View.UpdateAddDiceDefenseCount(DefenseDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Added dice to DEFENSE: {DefenseDiceAllocated}");
    }

    public void OnEndTurn()
    {
        if (SelectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        Combat.ReceivePlayerInput(
            SelectedAction.Value,
            AttackDiceAllocated,
            DefenseDiceAllocated
        );

        AttackDiceAllocated = 0;
        DefenseDiceAllocated = 0;
        SelectedAction = AllowedAction;
    }
}
