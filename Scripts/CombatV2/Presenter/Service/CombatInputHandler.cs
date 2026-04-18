using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private CombatManager _combat;
    private int _attackDiceAllocated = 0;
    private int _defenseDiceAllocated = 0;
    private ActionType? _selectedAction = null;
    private ActionType _allowedAction = ActionType.Attack;

    public void Init(CombatManager combat)
    {
        _combat = combat;
    }

    public void SetAllowedAction(ActionType allowedAction)
    {
        _allowedAction = allowedAction;
        _selectedAction = allowedAction;

        if (allowedAction == ActionType.Attack)
        {
            _defenseDiceAllocated = 0;
        }
        else
        {
            _attackDiceAllocated = 0;
        }

        Debug.Log($"[Input] Turn role updated. Allowed action: {_allowedAction}");
    }

    public void OnAttack()
    {
        if (_allowedAction != ActionType.Attack)
        {
            Debug.Log("[Input] Attack is disabled for this turn role");
            return;
        }

        _selectedAction = ActionType.Attack;
        Debug.Log("[Input] Selected ATTACK");
    }

    public void OnDefend()
    {
        if (_allowedAction != ActionType.Defense)
        {
            Debug.Log("[Input] Defense is disabled for this turn role");
            return;
        }

        _selectedAction = ActionType.Defense;
        Debug.Log("[Input] Selected DEFENSE");
    }

    public void OnAddDiceToAttack()
    {
        if (_allowedAction != ActionType.Attack) return;
        if (_combat.Player.CurrentDices <= 0) return;

        _attackDiceAllocated++;
        _combat.Player.CurrentDices--;

        Debug.Log($"[Input] Added dice to ATTACK: {_attackDiceAllocated}");
    }

    public void OnAddDiceToDefense()
    {
        if (_allowedAction != ActionType.Defense) return;
        if (_combat.Player.CurrentDices <= 0) return;

        _defenseDiceAllocated++;
        _combat.Player.CurrentDices--;

        Debug.Log($"[Input] Added dice to DEFENSE: {_defenseDiceAllocated}");
    }

    public void OnEndTurn()
    {
        if (_selectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        _combat.ReceivePlayerInput(
            _selectedAction.Value,
            _attackDiceAllocated,
            _defenseDiceAllocated
        );

        _attackDiceAllocated = 0;
        _defenseDiceAllocated = 0;
        _selectedAction = _allowedAction;
    }
}
